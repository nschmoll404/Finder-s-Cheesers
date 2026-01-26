using UnityEngine;
using UnityEngine.InputSystem;

namespace FindersCheesers
{
    /// <summary>
    /// A controller for the pack of rats that moves together.
    /// The pack's movement is controlled by player input.
    /// The number of rats in the inventory affects the throw distance of the King Rat.
    /// </summary>
    [AddComponentMenu("Finders Cheesers/Rat Pack Controller")]
    public class RatPackController : MonoBehaviour
    {
        [Header("Input References")]
        [Tooltip("Reference to the Move input action")]
        [SerializeField]
        private InputActionReference moveActionReference;

        [Tooltip("Use PlayerInputSingleton to get PlayerInput")]
        [SerializeField]
        private bool usePlayerInputSingleton = false;

        [Header("Movement Settings")]
        [Tooltip("Maximum movement speed")]
        [SerializeField]
        private float maxSpeed = 5f;

        [Tooltip("Acceleration rate")]
        [SerializeField]
        private float acceleration = 8f;

        [Tooltip("Deceleration rate")]
        [SerializeField]
        private float deceleration = 8f;

        [Tooltip("Rotation speed for turning towards movement direction")]
        [SerializeField]
        private float rotationSpeed = 10f;

        [Header("Rat Inventory")]
        [Tooltip("Reference to the RatInventory component")]
        [SerializeField]
        private RatInventory ratInventory;

        [Header("Physics Settings")]
        [Tooltip("Should the Rigidbody use gravity?")]
        [SerializeField]
        private bool useGravity = true;

        [Tooltip("Drag applied to the Rigidbody when not moving")]
        [SerializeField]
        private float drag = 3f;

        [Header("Debug")]
        [Tooltip("Show debug information in the console")]
        [SerializeField]
        private bool debugMode = false;

        [Tooltip("Visualize movement direction in Scene view")]
        [SerializeField]
        private bool visualizeMovement = true;

        // Component references
        private Rigidbody rb;
        private PlayerInput playerInput;
        private InputAction moveAction;

        // Current state
        private Vector2 moveInput;
        private Vector3 currentVelocity;
        private bool isMoving;
        private Quaternion currentRotation;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            
            if (rb == null)
            {
                Debug.LogError("[RatPackController] Rigidbody component not found on this GameObject!");
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
                        Debug.Log("[RatPackController] Using PlayerInputSingleton for input.");
                    }
                }
                else
                {
                    Debug.LogError("[RatPackController] PlayerInputSingleton is not initialized!");
                }
            }
            else
            {
                playerInput = GetComponent<PlayerInput>();
                
                if (playerInput == null)
                {
                    Debug.LogError("[RatPackController] PlayerInput component not found on this GameObject!");
                }
            }

            // Get the move action using the ID from InputActionReference
            if (playerInput != null && moveActionReference != null)
            {
                moveAction = playerInput.actions.FindAction(moveActionReference.action.id);
                
                if (moveAction == null)
                {
                    Debug.LogError("[RatPackController] Move action not found in PlayerInput actions!");
                }
            }
            else if (moveActionReference == null)
            {
                Debug.LogError("[RatPackController] Move Action Reference is not assigned!");
            }

            // Get RatInventory component
            if (ratInventory == null)
            {
                ratInventory = GetComponent<RatInventory>();
                
                if (ratInventory == null)
                {
                    Debug.LogError("[RatPackController] RatInventory component not found on this GameObject!");
                }
            }

            // Initialize rotation
            currentRotation = transform.rotation;

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
        }

        private void FixedUpdate()
        {
            if (rb == null)
            {
                return;
            }

            HandleMovement();
            HandleRotation();
            ApplyRotation();
        }

        /// <summary>
        /// Handles movement physics using velocity-based approach.
        /// </summary>
        private void HandleMovement()
        {
            Vector3 targetVelocity = Vector3.zero;

            if (isMoving)
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
                // Decelerate to zero when not moving
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
                Debug.Log($"[RatPackController] Moving: {isMoving}, Velocity: {currentVelocity}");
            }
        }

        /// <summary>
        /// Handles rotation to face the movement direction.
        /// </summary>
        private void HandleRotation()
        {
            if (!isMoving)
            {
                return;
            }

            // Calculate target rotation based on movement direction
            Vector3 movementDirection = new Vector3(moveInput.x, 0f, moveInput.y);
            
            if (movementDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(movementDirection, Vector3.up);
                
                // Smoothly rotate towards target rotation
                currentRotation = Quaternion.Slerp(
                    currentRotation,
                    targetRotation,
                    rotationSpeed * Time.fixedDeltaTime
                );
            }
        }

        /// <summary>
        /// Applies the calculated rotation to the Rigidbody.
        /// </summary>
        private void ApplyRotation()
        {
            if (rb == null)
            {
                return;
            }

            rb.MoveRotation(currentRotation);
        }

        /// <summary>
        /// Gets the current number of rats in the pack.
        /// </summary>
        /// <returns>The number of rats in the pack.</returns>
        public int GetRatCount()
        {
            return (ratInventory != null) ? ratInventory.Count : 0;
        }

        /// <summary>
        /// Gets the list of rats in the pack.
        /// </summary>
        /// <returns>A copy of the rats list.</returns>
        public System.Collections.Generic.List<Rat> GetRats()
        {
            return (ratInventory != null) ? ratInventory.GetAllRats() : new System.Collections.Generic.List<Rat>();
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
        /// Gets whether the pack is currently moving.
        /// </summary>
        /// <returns>True if the pack is moving, false otherwise.</returns>
        public bool IsMoving()
        {
            return isMoving;
        }

        private void OnDrawGizmos()
        {
            if (!visualizeMovement)
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
            maxSpeed = 5f;
            acceleration = 8f;
            deceleration = 8f;
            rotationSpeed = 10f;
            useGravity = true;
            drag = 3f;
        }
    }
}
