using UnityEngine;
using UnityEngine.InputSystem;

namespace FindersCheesers
{
    /// <summary>
    /// A component that throws rats from the RatInventory to a destination using a physics arc.
    /// Uses pointer input for point-and-click targeting and visualizes the arc with a LineRenderer.
    /// </summary>
    [AddComponentMenu("Finders Cheesers/Rat Thrower")]
    public class RatThrower : MonoBehaviour
    {
        [Header("Input References")]
        [Tooltip("Reference to the Pointer input action for getting mouse position")]
        [SerializeField]
        private InputActionReference pointerActionReference;

        [Tooltip("Reference to the Throw input action (click to throw)")]
        [SerializeField]
        private InputActionReference throwActionReference;

        [Tooltip("Use PlayerInputSingleton to get PlayerInput")]
        [SerializeField]
        private bool usePlayerInputSingleton = false;

        [Header("Camera Settings")]
        [Tooltip("Camera to use for raycasting to world position")]
        [SerializeField]
        private Camera mainCamera;

        [Header("Rat Inventory")]
        [Tooltip("Reference to the RatInventory component")]
        [SerializeField]
        private RatInventory ratInventory;

        [Header("Arc Visualization")]
        [Tooltip("LineRenderer for visualizing the throw arc")]
        [SerializeField]
        private LineRenderer arcLineRenderer;

        [Tooltip("Number of segments in the arc visualization")]
        [SerializeField]
        private int arcSegments = 30;

        [Tooltip("Color of the arc line")]
        [SerializeField]
        private Color arcColor = Color.yellow;

        [Header("Throw Settings")]
        [Tooltip("Launch speed for the rat")]
        [SerializeField]
        private float launchSpeed = 15f;

        [Tooltip("Launch angle in degrees")]
        [SerializeField]
        private float launchAngle = 45f;

        [Tooltip("Duration of the throw animation")]
        [SerializeField]
        private float throwDuration = 1f;

        [Tooltip("Height offset for the launch position")]
        [SerializeField]
        private float launchHeightOffset = 1f;

        [Header("Debug")]
        [Tooltip("Show debug information in the console")]
        [SerializeField]
        private bool debugMode = false;

        [Tooltip("Visualize throw target in Scene view")]
        [SerializeField]
        private bool visualizeTarget = true;

        // Component references
        private PlayerInput playerInput;
        private InputAction pointerAction;
        private InputAction throwAction;

        // Current state
        private Vector2 pointerPosition;
        private Vector3? targetPosition;
        private bool isThrowing;
        private Rat currentThrownRat;
        private Vector3 throwStartPosition;
        private Vector3 throwEndPosition;
        private float throwTimer;
        private Vector3[] arcPoints;

        /// <summary>
        /// Event fired when a rat is thrown.
        /// </summary>
        public event System.Action<Rat, Vector3> OnRatThrown;

        /// <summary>
        /// Event fired when a rat lands.
        /// </summary>
        public event System.Action<Rat, Vector3> OnRatLanded;

        private void Awake()
        {
            // Initialize arc points array
            arcPoints = new Vector3[arcSegments + 1];

            // Get or create LineRenderer
            if (arcLineRenderer == null)
            {
                arcLineRenderer = GetComponent<LineRenderer>();
                if (arcLineRenderer == null)
                {
                    arcLineRenderer = gameObject.AddComponent<LineRenderer>();
                }
            }

            // Configure LineRenderer
            arcLineRenderer.positionCount = arcSegments + 1;
            arcLineRenderer.startWidth = 0.1f;
            arcLineRenderer.endWidth = 0.05f;
            arcLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            arcLineRenderer.startColor = arcColor;
            arcLineRenderer.endColor = arcColor;
            arcLineRenderer.enabled = false;
        }

        private void Start()
        {
            // Get PlayerInput - either from singleton or from the same GameObject
            if (usePlayerInputSingleton)
            {
                if (PlayerInputSingleton.IsInitialized())
                {
                    playerInput = PlayerInputSingleton.Instance.PlayerInput;
                    
                    if (debugMode)
                    {
                        Debug.Log("[RatThrower] Using PlayerInputSingleton for input.");
                    }
                }
                else
                {
                    Debug.LogError("[RatThrower] PlayerInputSingleton is not initialized!");
                }
            }
            else
            {
                playerInput = GetComponent<PlayerInput>();
                
                if (playerInput == null)
                {
                    Debug.LogError("[RatThrower] PlayerInput component not found on this GameObject!");
                }
            }

            // Get the pointer action using the ID from InputActionReference
            if (playerInput != null && pointerActionReference != null)
            {
                pointerAction = playerInput.actions.FindAction(pointerActionReference.action.id);
                
                if (pointerAction == null)
                {
                    Debug.LogError("[RatThrower] Pointer action not found in PlayerInput actions!");
                }
            }
            else if (pointerActionReference == null)
            {
                Debug.LogError("[RatThrower] Pointer Action Reference is not assigned!");
            }

            // Get the throw action using the ID from InputActionReference
            if (playerInput != null && throwActionReference != null)
            {
                throwAction = playerInput.actions.FindAction(throwActionReference.action.id);
                
                if (throwAction == null)
                {
                    Debug.LogError("[RatThrower] Throw action not found in PlayerInput actions!");
                }
            }
            else if (throwActionReference == null)
            {
                Debug.LogError("[RatThrower] Throw Action Reference is not assigned!");
            }

            // Get RatInventory component
            if (ratInventory == null)
            {
                ratInventory = GetComponent<RatInventory>();
                
                if (ratInventory == null)
                {
                    ratInventory = GetComponentInParent<RatInventory>();
                }

                if (ratInventory == null)
                {
                    Debug.LogError("[RatThrower] RatInventory component not found!");
                }
            }

            // Get camera if not assigned
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }
        }

        private void Update()
        {
            // Read pointer position
            if (pointerAction != null)
            {
                pointerPosition = pointerAction.ReadValue<Vector2>();
                UpdateTargetPosition();
            }

            // Check for throw input
            if (throwAction != null && !isThrowing)
            {
                if (throwAction.WasPressedThisFrame())
                {
                    TryThrowRat();
                }
            }

            // Update arc visualization
            UpdateArcVisualization();

            // Update throw animation
            if (isThrowing)
            {
                UpdateThrowAnimation();
            }
        }

        /// <summary>
        /// Updates the target position based on pointer input.
        /// </summary>
        private void UpdateTargetPosition()
        {
            if (mainCamera == null)
            {
                return;
            }

            // Raycast from camera to ground plane
            Ray ray = mainCamera.ScreenPointToRay(pointerPosition);
            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
            
            if (groundPlane.Raycast(ray, out float distance))
            {
                targetPosition = ray.GetPoint(distance);
            }
            else
            {
                targetPosition = null;
            }
        }

        /// <summary>
        /// Updates the arc visualization.
        /// </summary>
        private void UpdateArcVisualization()
        {
            if (arcLineRenderer == null)
            {
                return;
            }

            // Only show arc if we have a target and rats available
            if (targetPosition.HasValue && ratInventory != null && ratInventory.Count > 0 && !isThrowing)
            {
                Vector3 start = GetLaunchPosition();
                Vector3 end = targetPosition.Value;
                
                // Calculate arc points
                CalculateArcPoints(start, end, arcPoints);
                
                // Update LineRenderer positions
                for (int i = 0; i < arcPoints.Length; i++)
                {
                    arcLineRenderer.SetPosition(i, arcPoints[i]);
                }
                
                arcLineRenderer.enabled = true;
            }
            else
            {
                arcLineRenderer.enabled = false;
            }
        }

        /// <summary>
        /// Gets the launch position for the rat.
        /// </summary>
        private Vector3 GetLaunchPosition()
        {
            return transform.position + Vector3.up * launchHeightOffset;
        }

        /// <summary>
        /// Calculates arc points for visualization.
        /// </summary>
        private void CalculateArcPoints(Vector3 start, Vector3 end, Vector3[] points)
        {
            float distance = Vector3.Distance(new Vector3(start.x, 0, start.z), new Vector3(end.x, 0, end.z));
            float heightDifference = end.y - start.y;
            
            // Calculate initial velocity components
            float angleRad = launchAngle * Mathf.Deg2Rad;
            float vx = launchSpeed * Mathf.Cos(angleRad);
            float vy = launchSpeed * Mathf.Sin(angleRad);
            
            // Calculate flight time
            float flightTime = distance / vx;
            
            // Generate arc points
            for (int i = 0; i < points.Length; i++)
            {
                float t = (float)i / (points.Length - 1);
                float time = t * flightTime;
                
                // Calculate position at time t using projectile motion equations
                float x = start.x + (end.x - start.x) * t;
                float z = start.z + (end.z - start.z) * t;
                float y = start.y + vy * time - 0.5f * 9.81f * time * time;
                
                points[i] = new Vector3(x, y, z);
            }
        }

        /// <summary>
        /// Attempts to throw a rat to the target position.
        /// </summary>
        private void TryThrowRat()
        {
            if (ratInventory == null || ratInventory.Count == 0)
            {
                if (debugMode)
                {
                    Debug.LogWarning("[RatThrower] No rats available to throw!");
                }
                return;
            }

            if (!targetPosition.HasValue)
            {
                if (debugMode)
                {
                    Debug.LogWarning("[RatThrower] No target position set!");
                }
                return;
            }

            // Get the first rat from inventory
            Rat rat = ratInventory.GetRat(0);
            if (rat == null)
            {
                if (debugMode)
                {
                    Debug.LogWarning("[RatThrower] Failed to get rat from inventory!");
                }
                return;
            }

            // Remove rat from inventory
            ratInventory.RemoveRat(rat);

            // Start throw animation
            StartThrowAnimation(rat, targetPosition.Value);
        }

        /// <summary>
        /// Starts the throw animation for a rat.
        /// </summary>
        private void StartThrowAnimation(Rat rat, Vector3 destination)
        {
            isThrowing = true;
            currentThrownRat = rat;
            throwStartPosition = GetLaunchPosition();
            throwEndPosition = destination;
            throwTimer = 0f;

            // Fire event
            OnRatThrown?.Invoke(rat, destination);

            if (debugMode)
            {
                Debug.Log($"[RatThrower] Throwing rat to {destination}");
            }
        }

        /// <summary>
        /// Updates the throw animation.
        /// </summary>
        private void UpdateThrowAnimation()
        {
            if (currentThrownRat == null)
            {
                isThrowing = false;
                return;
            }

            throwTimer += Time.deltaTime;
            float t = Mathf.Clamp01(throwTimer / throwDuration);

            // Calculate arc position
            Vector3 position = CalculateArcPosition(throwStartPosition, throwEndPosition, t);
            currentThrownRat.transform.position = position;

            // Check if throw is complete
            if (t >= 1f)
            {
                FinishThrowAnimation();
            }
        }

        /// <summary>
        /// Calculates the position along the arc at time t (0-1).
        /// </summary>
        private Vector3 CalculateArcPosition(Vector3 start, Vector3 end, float t)
        {
            // Linear interpolation for X and Z
            Vector3 flatPosition = Vector3.Lerp(
                new Vector3(start.x, 0, start.z),
                new Vector3(end.x, 0, end.z),
                t
            );

            // Parabolic arc for Y
            float height = Mathf.Lerp(start.y, end.y, t);
            float arcHeight = Mathf.Sin(t * Mathf.PI) * launchSpeed * 0.2f;

            return new Vector3(flatPosition.x, height + arcHeight, flatPosition.z);
        }

        /// <summary>
        /// Finishes the throw animation.
        /// </summary>
        private void FinishThrowAnimation()
        {
            if (currentThrownRat != null)
            {
                // Fire event
                OnRatLanded?.Invoke(currentThrownRat, throwEndPosition);

                if (debugMode)
                {
                    Debug.Log($"[RatThrower] Rat landed at {throwEndPosition}");
                }
            }

            isThrowing = false;
            currentThrownRat = null;
        }

        /// <summary>
        /// Gets the current target position.
        /// </summary>
        public Vector3? GetTargetPosition()
        {
            return targetPosition;
        }

        /// <summary>
        /// Gets whether a rat is currently being thrown.
        /// </summary>
        public bool IsThrowing()
        {
            return isThrowing;
        }

        /// <summary>
        /// Gets the currently thrown rat.
        /// </summary>
        public Rat GetCurrentThrownRat()
        {
            return currentThrownRat;
        }

        /// <summary>
        /// Sets the launch speed.
        /// </summary>
        public void SetLaunchSpeed(float speed)
        {
            launchSpeed = Mathf.Max(1f, speed);
        }

        /// <summary>
        /// Sets the launch angle.
        /// </summary>
        public void SetLaunchAngle(float angle)
        {
            launchAngle = Mathf.Clamp(angle, 10f, 80f);
        }

        /// <summary>
        /// Sets the throw duration.
        /// </summary>
        public void SetThrowDuration(float duration)
        {
            throwDuration = Mathf.Max(0.1f, duration);
        }

        private void OnDrawGizmos()
        {
            if (!visualizeTarget || !targetPosition.HasValue)
            {
                return;
            }

            // Draw target position
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(targetPosition.Value, 0.3f);
            Gizmos.DrawLine(transform.position + Vector3.up * launchHeightOffset, targetPosition.Value);

            // Draw arc if in play mode
            if (Application.isPlaying && arcPoints != null && arcPoints.Length > 0)
            {
                Gizmos.color = Color.yellow;
                for (int i = 0; i < arcPoints.Length - 1; i++)
                {
                    Gizmos.DrawLine(arcPoints[i], arcPoints[i + 1]);
                }
            }
        }

        private void Reset()
        {
            launchSpeed = 15f;
            launchAngle = 45f;
            throwDuration = 1f;
            launchHeightOffset = 1f;
            arcSegments = 30;
            arcColor = Color.yellow;
        }
    }
}
