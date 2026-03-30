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

        [Header("Camera Settings")]
        [Tooltip("Camera to use for relative movement direction")]
        [SerializeField]
        private Camera mainCamera;

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

        [Header("Carry Speed")]
        [Tooltip("Reference to the KingRatHandler component")]
        [SerializeField]
        private KingRatHandler kingRatHandler;

        [Tooltip("Movement speed while carrying an object")]
        [SerializeField]
        private float carryingSpeed = 2.5f;

        [Header("Physics Settings")]
        [Tooltip("Should the Rigidbody use gravity?")]
        [SerializeField]
        private bool useGravity = true;

        [Tooltip("Gravity force applied when not grounded")]
        [SerializeField]
        private float gravity = -20f;

        [Tooltip("Distance from the bottom to check for ground")]
        [SerializeField]
        private float groundCheckDistance = 0.1f;

        [Tooltip("Layers to consider as ground")]
        [SerializeField]
        private LayerMask groundLayers;

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
        private bool isGrounded;
        
        // Movement override - allows external systems (like SplineRider) to take control
        private bool isMovementOverridden = false;

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

            // Get camera if not assigned
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            // Initialize rotation
            currentRotation = transform.rotation;

            // Configure Rigidbody
            if (rb != null)
            {
                // Disable Unity's built-in gravity when using custom gravity
                rb.useGravity = false;
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

            CheckGrounded();
            HandleGravity();
            HandleMovement();
            HandleRotation();
            ApplyRotation();
        }

        /// <summary>
        /// Handles movement physics using velocity-based approach.
        /// Movement direction is relative to the camera's Y rotation.
        /// </summary>
        private void HandleMovement()
        {
            // Skip movement if overridden by external system (e.g., SplineRider)
            if (isMovementOverridden)
            {
                return;
            }

            Vector3 targetVelocity = Vector3.zero;

            // Determine effective speed: use carrying speed when grabbing, default otherwise
            float effectiveSpeed = (kingRatHandler != null && kingRatHandler.IsGrabbing) ? carryingSpeed : maxSpeed;

            if (isMoving)
            {
                // Calculate input direction in local space
                Vector3 inputDirection = new Vector3(moveInput.x, 0f, moveInput.y);
                
                // Rotate input direction by camera's Y rotation for camera-relative movement
                if (mainCamera != null)
                {
                    float cameraYRotation = mainCamera.transform.eulerAngles.y;
                    inputDirection = Quaternion.Euler(0f, cameraYRotation, 0f) * inputDirection;
                }
                
                // Calculate target velocity in world space
                targetVelocity = inputDirection * effectiveSpeed;
                
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
        /// Rotation direction is relative to the camera's Y rotation.
        /// </summary>
        private void HandleRotation()
        {
            // Skip rotation if overridden by external system (e.g., SplineRider)
            if (isMovementOverridden)
            {
                return;
            }

            if (!isMoving)
            {
                return;
            }

            // Calculate input direction in local space
            Vector3 movementDirection = new Vector3(moveInput.x, 0f, moveInput.y);
            
            // Rotate input direction by camera's Y rotation for camera-relative rotation
            if (mainCamera != null)
            {
                float cameraYRotation = mainCamera.transform.eulerAngles.y;
                movementDirection = Quaternion.Euler(0f, cameraYRotation, 0f) * movementDirection;
            }
            
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
        /// Checks if the controller is grounded using a raycast.
        /// </summary>
        private void CheckGrounded()
        {
            isGrounded = false;

            if (groundLayers.value == 0)
            {
                // If no ground layers are set, use default ground check
                isGrounded = Physics.Raycast(
                    transform.position,
                    Vector3.down,
                    groundCheckDistance
                );
            }
            else
            {
                // Use specified ground layers
                isGrounded = Physics.Raycast(
                    transform.position,
                    Vector3.down,
                    groundCheckDistance,
                    groundLayers
                );
            }

            if (debugMode)
            {
                Debug.DrawRay(
                    transform.position,
                    Vector3.down * groundCheckDistance,
                    isGrounded ? Color.green : Color.red
                );
            }
        }

        /// <summary>
        /// Applies custom gravity when not grounded.
        /// </summary>
        private void HandleGravity()
        {
            if (!useGravity)
            {
                return;
            }

            if (isGrounded)
            {
                // Reset vertical velocity when grounded
                if (rb.linearVelocity.y < 0)
                {
                    rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
                }
            }
            else
            {
                // Apply gravity force when not grounded
                rb.AddForce(Vector3.up * gravity, ForceMode.Acceleration);
            }
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

        /// <summary>
        /// Gets whether the controller is currently grounded.
        /// </summary>
        /// <returns>True if the controller is grounded, false otherwise.</returns>
        public bool IsGrounded()
        {
            return isGrounded;
        }

        /// <summary>
        /// Sets whether movement is overridden by an external system.
        /// When overridden, the controller will not apply velocity or rotation.
        /// </summary>
        /// <param name="overridden">True if movement should be overridden.</param>
        public void SetMovementOverride(bool overridden)
        {
            isMovementOverridden = overridden;
        }

        /// <summary>
        /// Gets whether movement is currently overridden.
        /// </summary>
        /// <returns>True if movement is overridden, false otherwise.</returns>
        public bool IsMovementOverridden()
        {
            return isMovementOverridden;
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
            carryingSpeed = 2.5f;
            useGravity = true;
            gravity = -20f;
            groundCheckDistance = 0.1f;
            drag = 3f;
        }
    }
}
