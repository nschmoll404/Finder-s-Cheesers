using UnityEngine;
using UnityEngine.InputSystem;

namespace FindersCheesers
{
    /// <summary>
    /// A specialized controller for King Rat that requires support from other rats to move.
    /// The King Rat can only move if at least 2 rats are supporting him.
    /// His position and tilt change based on the number and distribution of supporting rats.
    /// </summary>
    [AddComponentMenu("Finders Cheesers/King Rat Controller")]
    public class KingRatController : MonoBehaviour
    {
        [Header("Input References")]
        [Tooltip("Reference to the Move input action")]
        [SerializeField]
        private InputActionReference moveActionReference;

        [Tooltip("Use PlayerInputSingleton to get PlayerInput")]
        [SerializeField]
        private bool usePlayerInputSingleton = false;

        [Header("Movement Settings")]
        [Tooltip("Maximum movement speed when fully supported")]
        [SerializeField]
        private float maxSpeed = 3f;

        [Tooltip("Acceleration rate")]
        [SerializeField]
        private float acceleration = 5f;

        [Tooltip("Deceleration rate")]
        [SerializeField]
        private float deceleration = 5f;

        [Tooltip("Rotation speed for turning towards movement direction")]
        [SerializeField]
        private float rotationSpeed = 8f;

        [Header("Support Settings")]
        [Tooltip("Minimum number of rats required to move")]
        [SerializeField]
        private int minRatsToMove = 2;

        [Tooltip("Reference to the RatInventory component")]
        [SerializeField]
        private RatInventory ratInventory;

        [Tooltip("Height of King Rat when fully supported (at max rats)")]
        [SerializeField]
        private float maxHeight = 2f;

        [Tooltip("Height of King Rat when on the ground (no support)")]
        [SerializeField]
        private float minHeight = 0.2f;

        [Tooltip("How quickly King Rat adjusts his height")]
        [SerializeField]
        private float heightAdjustmentSpeed = 3f;

        [Tooltip("Maximum tilt angle when support is imbalanced")]
        [SerializeField]
        private float maxTiltAngle = 30f;

        [Tooltip("How quickly King Rat adjusts his tilt")]
        [SerializeField]
        private float tiltAdjustmentSpeed = 5f;

        [Header("Physics Settings")]
        [Tooltip("Should the Rigidbody use gravity?")]
        [SerializeField]
        private bool useGravity = false;

        [Tooltip("Drag applied to the Rigidbody when not moving")]
        [SerializeField]
        private float drag = 5f;

        [Header("Debug")]
        [Tooltip("Show debug information in the console")]
        [SerializeField]
        private bool debugMode = false;

        [Tooltip("Visualize support positions in Scene view")]
        [SerializeField]
        private bool visualizeSupport = true;

        // Component references
        private Rigidbody rb;
        private PlayerInput playerInput;
        private InputAction moveAction;

        // Current state
        private Vector2 moveInput;
        private Vector3 currentVelocity;
        private bool isMoving;
        private float currentHeight;
        private Quaternion currentTilt;
        private Quaternion currentYawRotation;
        private bool canMove;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            
            if (rb == null)
            {
                Debug.LogError("[KingRatController] Rigidbody component not found on this GameObject!");
            }
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
                        Debug.Log("[KingRatController] Using PlayerInputSingleton for input.");
                    }
                }
                else
                {
                    Debug.LogError("[KingRatController] PlayerInputSingleton is not initialized!");
                }
            }
            else
            {
                playerInput = GetComponent<PlayerInput>();
                
                if (playerInput == null)
                {
                    Debug.LogError("[KingRatController] PlayerInput component not found on this GameObject!");
                }
            }

            // Get the move action using the ID from InputActionReference
            if (playerInput != null && moveActionReference != null)
            {
                moveAction = playerInput.actions.FindAction(moveActionReference.action.id);
                
                if (moveAction == null)
                {
                    Debug.LogError("[KingRatController] Move action not found in PlayerInput actions!");
                }
            }
            else if (moveActionReference == null)
            {
                Debug.LogError("[KingRatController] Move Action Reference is not assigned!");
            }

            // Get RatInventory component
            if (ratInventory == null)
            {
                ratInventory = GetComponent<RatInventory>();
                
                if (ratInventory == null)
                {
                    Debug.LogError("[KingRatController] RatInventory component not found on this GameObject!");
                }
            }

            // Initialize height and tilt
            currentHeight = minHeight;
            currentTilt = Quaternion.identity;
            currentYawRotation = Quaternion.identity;

            // Configure Rigidbody
            if (rb != null)
            {
                rb.useGravity = useGravity;
                rb.linearDamping = drag;
                rb.angularDamping = 10f;
                rb.constraints = RigidbodyConstraints.FreezeRotation;
            }
        }

        private void Update()
        {
            // Read input values using polling approach
            if (moveAction != null)
            {
                moveInput = moveAction.ReadValue<Vector2>();
                isMoving = moveInput.sqrMagnitude > 0.01f;
            }

            // Update can move state based on supporting rats
            canMove = (ratInventory != null) && (ratInventory.Count >= minRatsToMove);

            // Update height based on support count
            UpdateHeight();

            // Update tilt based on support imbalance
            UpdateTilt();
        }

        private void FixedUpdate()
        {
            if (rb == null)
            {
                return;
            }

            HandleMovement();
            HandleRotation();
            ApplyPositionAndRotation();
        }

        /// <summary>
        /// Updates the King Rat's height based on the number of supporting rats.
        /// </summary>
        private void UpdateHeight()
        {
            // Calculate target height based on support count
            float targetHeight;
            
            if (ratInventory == null || ratInventory.Count == 0)
            {
                targetHeight = minHeight;
            }
            else
            {
                // Interpolate height based on support count
                float supportRatio = (float)ratInventory.Count / ratInventory.MaxCapacity;
                targetHeight = Mathf.Lerp(minHeight, maxHeight, supportRatio);
            }

            // Smoothly adjust height
            currentHeight = Mathf.Lerp(
                currentHeight,
                targetHeight,
                heightAdjustmentSpeed * Time.deltaTime
            );

            if (debugMode)
            {
                int ratCount = (ratInventory != null) ? ratInventory.Count : 0;
                Debug.Log($"[KingRatController] Height: {currentHeight:F2} (Rats: {ratCount})");
            }
        }

        /// <summary>
        /// Updates the King Rat's tilt based on support imbalance.
        /// </summary>
        private void UpdateTilt()
        {
            Quaternion targetTilt = Quaternion.identity;

            if (ratInventory != null && ratInventory.Count > 0)
            {
                // Calculate tilt direction based on support center offset
                Vector3 offset = ratInventory.SupportCenter - transform.position;
                offset.y = 0f; // Ignore height difference

                if (offset.magnitude > 0.01f)
                {
                    // Calculate tilt angle based on imbalance
                    float supportImbalance = ratInventory.CalculateSupportImbalance();
                    float tiltAngle = Mathf.Lerp(0f, maxTiltAngle, supportImbalance);

                    // Calculate tilt axis (perpendicular to offset, in the XZ plane)
                    // This axis should be perpendicular to both the offset direction and up
                    Vector3 tiltAxis = Vector3.Cross(offset.normalized, Vector3.up);

                    // Create tilt rotation around the tilt axis
                    targetTilt = Quaternion.AngleAxis(tiltAngle, tiltAxis);
                }
            }

            // Smoothly adjust tilt
            currentTilt = Quaternion.Slerp(
                currentTilt,
                targetTilt,
                tiltAdjustmentSpeed * Time.deltaTime
            );
        }

        /// <summary>
        /// Applies the calculated height and tilt to the King Rat's position and rotation.
        /// </summary>
        private void ApplyPositionAndRotation()
        {
            if (rb == null)
            {
                return;
            }

            // Preserve current X and Z position from physics, only adjust Y for height
            Vector3 position = rb.position;
            position.y = currentHeight;
            rb.MovePosition(position);

            // Apply the combined rotation: yaw (facing direction) * tilt (support imbalance)
            rb.MoveRotation(currentYawRotation * currentTilt);
        }

        /// <summary>
        /// Handles movement physics using velocity-based approach.
        /// Only allows movement if there are enough supporting rats.
        /// </summary>
        private void HandleMovement()
        {
            Vector3 targetVelocity = Vector3.zero;

            if (canMove && isMoving)
            {
                // Calculate target velocity in world space (XZ plane for top-down)
                targetVelocity = new Vector3(moveInput.x, 0f, moveInput.y) * maxSpeed;
                
                // Smoothly interpolate current velocity towards target velocity
                currentVelocity = Vector3.Lerp(
                    currentVelocity,
                    targetVelocity,
                    acceleration * Time.fixedDeltaTime
                );
            }
            else
            {
                // Decelerate to zero when not moving or not supported
                currentVelocity = Vector3.Lerp(
                    currentVelocity,
                    Vector3.zero,
                    deceleration * Time.fixedDeltaTime
                );
            }

            // Apply velocity to Rigidbody
            rb.linearVelocity = new Vector3(currentVelocity.x, rb.linearVelocity.y, currentVelocity.z);

            if (debugMode && isMoving)
            {
                Debug.Log($"[KingRatController] Moving: {canMove}, Velocity: {currentVelocity}");
            }
        }

        /// <summary>
        /// Handles rotation to face the movement direction.
        /// </summary>
        private void HandleRotation()
        {
            if (!canMove || !isMoving)
            {
                return;
            }

            // Calculate target rotation based on movement direction
            Vector3 movementDirection = new Vector3(moveInput.x, 0f, moveInput.y);
            
            if (movementDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(movementDirection, Vector3.up);
                
                // Smoothly rotate towards target rotation (yaw only)
                currentYawRotation = Quaternion.Slerp(
                    currentYawRotation,
                    targetRotation,
                    rotationSpeed * Time.fixedDeltaTime
                );
            }
        }

        /// <summary>
        /// Gets the current number of supporting rats.
        /// </summary>
        /// <returns>The number of supporting rats.</returns>
        public int GetSupportingRatCount()
        {
            return (ratInventory != null) ? ratInventory.Count : 0;
        }

        /// <summary>
        /// Gets the list of supporting rats.
        /// </summary>
        /// <returns>A copy of the supporting rats list.</returns>
        public System.Collections.Generic.List<Rat> GetSupportingRats()
        {
            return (ratInventory != null) ? ratInventory.GetAllRats() : new System.Collections.Generic.List<Rat>();
        }

        /// <summary>
        /// Gets whether the King Rat can move.
        /// </summary>
        /// <returns>True if the King Rat can move, false otherwise.</returns>
        public bool CanMove()
        {
            return canMove;
        }

        /// <summary>
        /// Gets the current velocity of the controller.
        /// </summary>
        /// <returns>The current velocity vector.</returns>
        public Vector3 GetVelocity()
        {
            if (rb != null)
            {
                return rb.linearVelocity;
            }
            return Vector3.zero;
        }

        /// <summary>
        /// Gets the current height of the King Rat.
        /// </summary>
        /// <returns>The current height above ground.</returns>
        public float GetCurrentHeight()
        {
            return currentHeight;
        }

        /// <summary>
        /// Gets the current tilt of the King Rat.
        /// </summary>
        /// <returns>The current tilt rotation.</returns>
        public Quaternion GetCurrentTilt()
        {
            return currentTilt;
        }

        private void OnDrawGizmos()
        {
            if (!visualizeSupport)
            {
                return;
            }

            // Draw movement direction when selected
            if (isMoving && rb != null)
            {
                Gizmos.color = Color.blue;
                Vector3 movementDirection = new Vector3(moveInput.x, 0f, moveInput.y).normalized;
                Gizmos.DrawLine(transform.position, transform.position + movementDirection * 2f);
            }
        }

        private void Reset()
        {
            maxSpeed = 3f;
            acceleration = 5f;
            deceleration = 5f;
            rotationSpeed = 8f;
            minRatsToMove = 2;
            maxHeight = 2f;
            minHeight = 0.2f;
            heightAdjustmentSpeed = 3f;
            maxTiltAngle = 30f;
            tiltAdjustmentSpeed = 5f;
            useGravity = false;
            drag = 5f;
        }
    }
}
