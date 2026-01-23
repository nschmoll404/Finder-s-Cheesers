using UnityEngine;
using UnityEngine.InputSystem;

namespace FindersCheesers
{
    /// <summary>
    /// A top-down Rigidbody-based character controller that rotates to face movement direction.
    /// Uses Unity's InputSystem with PlayerInput component for input handling.
    /// </summary>
    [AddComponentMenu("Finders Cheesers/Rigidbody Controller")]
    public class RigidbodyController : MonoBehaviour
    {
        [Header("Input References")]
        [Tooltip("Reference to the Move input action")]
        [SerializeField]
        private InputActionReference moveActionReference;

        [Tooltip("Use PlayerInputSingleton to get PlayerInput (recommended for larger projects)")]
        [SerializeField]
        private bool usePlayerInputSingleton = false;

        [Header("Movement Settings")]
        [Tooltip("Maximum movement speed")]
        [SerializeField]
        private float maxSpeed = 5f;

        [Tooltip("Acceleration rate (how quickly the character reaches max speed)")]
        [SerializeField]
        private float acceleration = 10f;

        [Tooltip("Deceleration rate (how quickly the character stops when input is released)")]
        [SerializeField]
        private float deceleration = 10f;

        [Tooltip("Rotation speed for turning towards movement direction")]
        [SerializeField]
        private float rotationSpeed = 10f;

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

        // Component references
        private Rigidbody _rigidbody;
        private PlayerInput playerInput;
        private InputAction moveAction;

        // Current state
        private Vector2 moveInput;
        private Vector3 currentVelocity;
        private bool isMoving;

        private void Awake()
        {
            // Cache Rigidbody component
            _rigidbody = GetComponent<Rigidbody>();
            
            if (_rigidbody == null)
            {
                Debug.LogError("[RigidbodyController] Rigidbody component not found on this GameObject!");
            }
        }

        private void Start()
        {
            // Get PlayerInput - either from singleton or from the same GameObject
            if (usePlayerInputSingleton)
            {
                // Use PlayerInputSingleton to get PlayerInput
                if (PlayerInputSingleton.IsInitialized())
                {
                    playerInput = PlayerInputSingleton.Instance.PlayerInput;
                    
                    if (debugMode)
                    {
                        Debug.Log("[RigidbodyController] Using PlayerInputSingleton for input.");
                    }
                }
                else
                {
                    Debug.LogError("[RigidbodyController] PlayerInputSingleton is not initialized!");
                }
            }
            else
            {
                // Get PlayerInput from the same GameObject
                playerInput = GetComponent<PlayerInput>();
                
                if (playerInput == null)
                {
                    Debug.LogError("[RigidbodyController] PlayerInput component not found on this GameObject!");
                }
            }

            // Get the move action using the ID from InputActionReference
            if (playerInput != null && moveActionReference != null)
            {
                moveAction = playerInput.actions.FindAction(moveActionReference.action.id);
                
                if (moveAction == null)
                {
                    Debug.LogError("[RigidbodyController] Move action not found in PlayerInput actions!");
                }
            }
            else if (moveActionReference == null)
            {
                Debug.LogError("[RigidbodyController] Move Action Reference is not assigned!");
            }

            // Configure Rigidbody
            if (_rigidbody != null)
            {
                _rigidbody.useGravity = useGravity;
                _rigidbody.linearDamping = drag;
                _rigidbody.angularDamping = 10f; // Reduce unwanted rotation
                _rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            }
        }

        private void Update()
        {
            // Read input values using polling approach
            if (moveAction != null)
            {
                moveInput = moveAction.ReadValue<Vector2>();
                
                // Check if there is meaningful input (above a small threshold)
                isMoving = moveInput.sqrMagnitude > 0.01f;
            }
        }

        private void FixedUpdate()
        {
            if (_rigidbody == null)
            {
                return;
            }

            HandleMovement();
            HandleRotation();
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
            _rigidbody.linearVelocity = new Vector3(currentVelocity.x, _rigidbody.linearVelocity.y, currentVelocity.z);

            if (debugMode && isMoving)
            {
                Debug.Log($"[RigidbodyController] Moving with velocity: {currentVelocity}");
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
                _rigidbody.rotation = Quaternion.Slerp(
                    _rigidbody.rotation,
                    targetRotation,
                    rotationSpeed * Time.fixedDeltaTime
                );
            }
        }

        /// <summary>
        /// Gets the current velocity of the controller.
        /// </summary>
        /// <returns>The current velocity vector.</returns>
        public Vector3 GetVelocity()
        {
            if (_rigidbody != null)
            {
                return _rigidbody.linearVelocity;
            }
            return Vector3.zero;
        }

        /// <summary>
        /// Gets whether the controller is currently moving.
        /// </summary>
        /// <returns>True if the controller is moving, false otherwise.</returns>
        public bool IsMoving()
        {
            return isMoving;
        }

        /// <summary>
        /// Gets the normalized movement input.
        /// </summary>
        /// <returns>The normalized movement input vector.</returns>
        public Vector2 GetMoveInput()
        {
            return moveInput;
        }

        /// <summary>
        /// Sets the maximum movement speed.
        /// </summary>
        /// <param name="speed">The new maximum speed.</param>
        public void SetMaxSpeed(float speed)
        {
            maxSpeed = Mathf.Max(0f, speed);
        }

        /// <summary>
        /// Sets the acceleration rate.
        /// </summary>
        /// <param name="accel">The new acceleration rate.</param>
        public void SetAcceleration(float accel)
        {
            acceleration = Mathf.Max(0f, accel);
        }

        /// <summary>
        /// Sets the rotation speed.
        /// </summary>
        /// <param name="speed">The new rotation speed.</param>
        public void SetRotationSpeed(float speed)
        {
            rotationSpeed = Mathf.Max(0f, speed);
        }

        private void OnDrawGizmos()
        {
            // Draw movement direction when selected
            if (isMoving && _rigidbody != null)
            {
                Gizmos.color = Color.green;
                Vector3 movementDirection = new Vector3(moveInput.x, 0f, moveInput.y).normalized;
                Gizmos.DrawLine(transform.position, transform.position + movementDirection * 2f);
                
                Gizmos.color = Color.blue;
                Gizmos.DrawRay(transform.position, transform.forward * 2f);
            }
        }

        private void Reset()
        {
            // Set default values when component is first added
            maxSpeed = 5f;
            acceleration = 10f;
            deceleration = 10f;
            rotationSpeed = 10f;
            useGravity = false;
            drag = 5f;
        }
    }
}
