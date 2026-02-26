using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Splines;

namespace FindersCheesers.Splines
{
    /// <summary>
    /// Allows a GameObject to detect and ride along splines.
    /// Uses InputSystem for interaction controls.
    /// Can override RatPackController movement when riding.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class SplineRider : MonoBehaviour
    {
        [Header("Input Settings")]
        [SerializeField]
        [Tooltip("Input action for attaching to a spline")]
        private InputActionReference interactActionReference;

        [SerializeField]
        [Tooltip("Input action for detaching from the spline")]
        private InputActionReference detachActionReference;

        [Header("Detection Settings")]
        [SerializeField]
        [Tooltip("Detection radius for finding nearby SplineBegin points")]
        private float detectionRadius = 3f;

        [SerializeField]
        [Tooltip("Layer mask for detecting SplineBegin objects")]
        private LayerMask splineBeginLayerMask = -1;

        [SerializeField]
        [Tooltip("Should show a prompt when a spline is nearby?")]
        private bool showPrompt = true;

        [Header("Riding Settings")]
        [SerializeField]
        [Tooltip("Should the rider follow the spline's rotation?")]
        private bool followSplineRotation = true;

        [SerializeField]
        [Tooltip("Should the rider's velocity be preserved when detaching?")]
        private bool preserveVelocityOnDetach = true;

        [SerializeField]
        [Tooltip("Multiplier for preserved velocity on detach")]
        private float detachVelocityMultiplier = 1f;

        [Header("Visual Settings")]
        [SerializeField]
        [Tooltip("Color to change to when a spline is nearby")]
        private Color nearbyColor = Color.green;

        [SerializeField]
        [Tooltip("Color to change to while riding")]
        private Color ridingColor = Color.cyan;

        [SerializeField]
        [Tooltip("Should the rider change color when nearby/riding?")]
        private bool changeColor = true;

        // State variables
        private InputAction interactAction;
        private InputAction detachAction;
        private PlayerInput playerInput;

        private bool isRiding = false;
        private SplineBegin currentSplineBegin;
        private float currentSplinePosition = 0f; // Normalized position (0-1)
        private float currentRideSpeed = 5f;
        private int currentTravelDirection = 1;

        private Collider riderCollider;
        private Rigidbody riderRigidbody;
        private Renderer riderRenderer;
        private Color originalColor;
        private bool hasOriginalColor;
        
        // Controller override
        private RatPackController ratPackController;

        private SplineBegin nearbySplineBegin;
        private bool canAttach = false;

        // Events
        public event System.Action OnSplineAttached;
        public event System.Action OnSplineDetached;
        public event System.Action<float> OnSplineProgressChanged; // float is normalized progress (0-1)

        // Public properties
        public bool IsRiding => isRiding;
        public SplineBegin CurrentSplineBegin => currentSplineBegin;
        public float CurrentProgress => currentSplinePosition;

        private void Awake()
        {
            riderCollider = GetComponent<Collider>();
            riderRigidbody = GetComponent<Rigidbody>();
            riderRenderer = GetComponent<Renderer>();
            ratPackController = GetComponent<RatPackController>();

            // Store original color
            if (riderRenderer != null && riderRenderer.material.HasProperty("_Color"))
            {
                originalColor = riderRenderer.material.color;
                hasOriginalColor = true;
            }
        }

        private void Start()
        {
            // Get PlayerInput reference following project patterns
            playerInput = PlayerInputSingleton.Instance?.PlayerInput;

            if (playerInput == null)
            {
                // Try to find PlayerInput on the same GameObject or parent
                playerInput = GetComponent<PlayerInput>();
                if (playerInput == null)
                {
                    playerInput = GetComponentInParent<PlayerInput>();
                }
            }

            if (playerInput != null)
            {
                // Get actions using IDs from InputActionReferences
                if (interactActionReference != null)
                {
                    interactAction = playerInput.actions.FindAction(interactActionReference.action.id);
                }

                if (detachActionReference != null)
                {
                    detachAction = playerInput.actions.FindAction(detachActionReference.action.id);
                }
            }
            else
            {
                Debug.LogWarning($"[{nameof(SplineRider)}] PlayerInput component not found. Input detection will not work.");
            }
        }

        private void Update()
        {
            if (isRiding)
            {
                UpdateRiding();
                HandleDetachInput();
            }
            else
            {
                DetectNearbySplines();
                HandleAttachInput();
            }
        }

        private void FixedUpdate()
        {
            if (isRiding)
            {
                UpdateRidingPhysics();
            }
        }

        #region Spline Detection

        private void DetectNearbySplines()
        {
            nearbySplineBegin = null;
            canAttach = false;

            Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, detectionRadius, splineBeginLayerMask);

            foreach (Collider col in nearbyColliders)
            {
                SplineBegin splineBegin = col.GetComponent<SplineBegin>();
                if (splineBegin != null && splineBegin.SplineContainer != null)
                {
                    nearbySplineBegin = splineBegin;
                    canAttach = true;
                    break;
                }
            }

            UpdateVisualState();
        }

        #endregion

        #region Input Handling

        private void HandleAttachInput()
        {
            if (canAttach && interactAction != null && interactAction.WasPressedThisFrame())
            {
                AttachToSpline(nearbySplineBegin);
            }
        }

        private void HandleDetachInput()
        {
            if (detachAction != null && detachAction.WasPressedThisFrame())
            {
                DetachFromSpline();
            }
        }

        #endregion

        #region Spline Riding

        /// <summary>
        /// Attaches the rider to a spline.
        /// </summary>
        /// <param name="splineBegin">The SplineBegin component to attach to</param>
        public void AttachToSpline(SplineBegin splineBegin)
        {
            if (splineBegin == null || splineBegin.SplineContainer == null)
            {
                Debug.LogWarning($"[{nameof(SplineRider)}] Cannot attach to invalid spline.");
                return;
            }

            currentSplineBegin = splineBegin;
            currentRideSpeed = splineBegin.RideSpeed;
            currentTravelDirection = splineBegin.TravelDirection;

            // Set starting position based on travel direction
            currentSplinePosition = currentTravelDirection > 0 ? 0f : 1f;

            // Move to the start of the spline
            Vector3 startPosition;
            Vector3 startTangent;
            splineBegin.GetStartPosition(out startPosition, out startTangent);

            transform.position = startPosition;

            if (followSplineRotation)
            {
                transform.rotation = Quaternion.LookRotation(startTangent * currentTravelDirection);
            }

            // Override RatPackController movement if present
            if (ratPackController != null)
            {
                ratPackController.SetMovementOverride(true);
            }

            // Disable physics while riding
            if (riderRigidbody != null)
            {
                riderRigidbody.isKinematic = true;
            }

            isRiding = true;
            canAttach = false;

            UpdateVisualState();
            OnSplineAttached?.Invoke();
        }

        private void UpdateRiding()
        {
            if (currentSplineBegin == null || currentSplineBegin.SplineContainer == null)
            {
                DetachFromSpline();
                return;
            }

            // Calculate movement along spline
            float splineLength = currentSplineBegin.SplineContainer.CalculateLength(currentSplineBegin.SplineIndex);
            float deltaPosition = (currentRideSpeed * Time.deltaTime) / splineLength;

            // Update position based on direction
            currentSplinePosition += deltaPosition * currentTravelDirection;

            // Clamp position
            if (currentTravelDirection > 0)
            {
                currentSplinePosition = Mathf.Clamp01(currentSplinePosition);
            }
            else
            {
                currentSplinePosition = Mathf.Clamp01(currentSplinePosition);
            }

            // Update position on spline
            Vector3 splinePosition = currentSplineBegin.SplineContainer.EvaluatePosition(
                currentSplineBegin.SplineIndex,
                currentSplinePosition
            );

            transform.position = splinePosition;

            // Update rotation if enabled
            if (followSplineRotation)
            {
                Vector3 splineTangent = currentSplineBegin.SplineContainer.EvaluateTangent(
                    currentSplineBegin.SplineIndex,
                    currentSplinePosition
                );

                if (splineTangent != Vector3.zero)
                {
                    transform.rotation = Quaternion.LookRotation(splineTangent * currentTravelDirection);
                }
            }

            // Check if reached end
            bool reachedEnd = currentTravelDirection > 0 ? currentSplinePosition >= 1f : currentSplinePosition <= 0f;

            if (reachedEnd)
            {
                if (currentSplineBegin.DetachAtEnd)
                {
                    DetachFromSpline();
                }
            }

            // Fire progress event
            OnSplineProgressChanged?.Invoke(currentSplinePosition);
        }

        private void UpdateRidingPhysics()
        {
            // Physics is handled in Update() for spline movement
            // This method is available for future physics-based interactions
        }

        /// <summary>
        /// Detaches the rider from the current spline.
        /// </summary>
        public void DetachFromSpline()
        {
            if (!isRiding)
                return;

            // Restore RatPackController movement if present
            if (ratPackController != null)
            {
                ratPackController.SetMovementOverride(false);
            }

            // Preserve velocity if enabled
            if (riderRigidbody != null && preserveVelocityOnDetach)
            {
                riderRigidbody.isKinematic = false;

                // Calculate velocity based on spline direction
                Vector3 splineTangent = currentSplineBegin.SplineContainer.EvaluateTangent(
                    currentSplineBegin.SplineIndex,
                    currentSplinePosition
                );

                Vector3 velocity = splineTangent * currentTravelDirection * currentRideSpeed * detachVelocityMultiplier;
                riderRigidbody.linearVelocity = velocity;
            }
            else if (riderRigidbody != null)
            {
                riderRigidbody.isKinematic = false;
            }

            isRiding = false;
            currentSplineBegin = null;

            UpdateVisualState();
            OnSplineDetached?.Invoke();
        }

        #endregion

        #region Visual State

        private void UpdateVisualState()
        {
            if (!changeColor || riderRenderer == null || !hasOriginalColor)
                return;

            if (isRiding)
            {
                riderRenderer.material.color = ridingColor;
            }
            else if (canAttach && nearbySplineBegin != null)
            {
                riderRenderer.material.color = nearbyColor;
            }
            else
            {
                riderRenderer.material.color = originalColor;
            }
        }

        #endregion

        #region Gizmos

        private void OnDrawGizmosSelected()
        {
            // Draw detection radius
            Gizmos.color = isRiding ? ridingColor : (canAttach ? nearbyColor : Color.gray);
            Gizmos.DrawWireSphere(transform.position, detectionRadius);

            // Draw line to nearby spline
            if (nearbySplineBegin != null && !isRiding)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, nearbySplineBegin.transform.position);
            }

            // Draw current progress on spline
            if (isRiding && currentSplineBegin != null && currentSplineBegin.SplineContainer != null)
            {
                Gizmos.color = Color.cyan;
                Vector3 splinePos = currentSplineBegin.SplineContainer.EvaluatePosition(
                    currentSplineBegin.SplineIndex,
                    currentSplinePosition
                );
                Gizmos.DrawWireSphere(splinePos, 0.5f);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets the ride speed while on a spline.
        /// </summary>
        public void SetRideSpeed(float speed)
        {
            currentRideSpeed = Mathf.Max(0f, speed);
        }

        /// <summary>
        /// Gets the current ride speed.
        /// </summary>
        public float GetRideSpeed()
        {
            return currentRideSpeed;
        }

        /// <summary>
        /// Forces detachment from the current spline.
        /// </summary>
        public void ForceDetach()
        {
            DetachFromSpline();
        }

        #endregion
    }
}
