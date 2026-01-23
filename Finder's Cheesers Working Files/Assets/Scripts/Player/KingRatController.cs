using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

namespace FindersCheesers.Player
{
    /// <summary>
    /// Main player controller for KingRat character.
    /// Handles WASD movement and rat-dependent speed scaling.
    /// </summary>
    [AddComponentMenu("Finder's Cheesers/Player/KingRat Controller")]
    public class KingRatController : MonoBehaviour
    {
        [Header("Input References")]
        [SerializeField] private InputActionReference moveActionReference;
        
        [Header("Movement Settings")]
        [SerializeField] private float baseMoveSpeed = 5f;
        [SerializeField] private int maxRats = 10;
        [SerializeField] private int minRatsToMove = 2;
        
        [Header("Camera")]
       //[SerializeField] private CinemachineCamera DroneCamera;
        
        // Components
        private PlayerInput playerInput;
        private InputAction moveAction;
        private CharacterController characterController;
        private UnityEngine.Camera mainCamera;

        // State
        private Vector2 moveInput;
        private int currentRats;
        private bool canMove;

        
        // Events
        public event System.Action<int> OnRatsChanged;
        public event System.Action<bool> OnCanMoveChanged;
        
        private void Awake()
        {
            // Get PlayerInput from singleton or component
            playerInput = GetComponent<PlayerInput>();
            characterController = GetComponent<CharacterController>();
            mainCamera = UnityEngine.Camera.main;

            // Initialize with max rats
            currentRats = maxRats;
            UpdateCanMove();
        }
        
        private void OnEnable()
        {
            if (playerInput != null && moveActionReference != null)
            {
                moveAction = playerInput.actions.FindAction(moveActionReference.action.id);
                if (moveAction != null)
                {
                    moveAction.performed += OnMovePerformed;
                    moveAction.canceled += OnMoveCanceled;
                }
            }
        }
        
        private void OnDisable()
        {
            if (moveAction != null)
            {
                moveAction.performed -= OnMovePerformed;
                moveAction.canceled -= OnMoveCanceled;
            }
        }
        
        private void OnMovePerformed(InputAction.CallbackContext context)
        {
            moveInput = context.ReadValue<Vector2>();
        }
        
        private void OnMoveCanceled(InputAction.CallbackContext context)
        {
            moveInput = Vector2.zero;
        }
        
        private void Update()
        {


            if (canMove && moveInput != Vector2.zero)
            {
                HandleMovement();
            }

            Vector2 mousePosition = Mouse.current.position.ReadValue();
      
        }
        
        private void HandleMovement()
        {
            // Calculate speed based on rat count
            float speedMultiplier = CalculateSpeedMultiplier();
            float currentSpeed = baseMoveSpeed * speedMultiplier;

            //Vector2 mousePosition = InputSystem.mousePosition;//pointAction != null ? pointAction.ReadValue<Vector2>() : Vector2.zero;
            //Vector3 targetPosition = GetWorldPositionFromMouse(Mouse.current.position);


            // Get camera-relative movement direction
            Vector3 forward = mainCamera.transform.forward;
            forward.y = 0;
            forward.Normalize();
            
            Vector3 right = mainCamera.transform.right;
            right.y = 0;
            right.Normalize();
            
            Vector3 moveDirection = (forward * moveInput.y + right * moveInput.x).normalized;
            
            // Apply movement
            characterController.Move(moveDirection * currentSpeed * Time.deltaTime);

            //characterController.transform.LookAt(targetPosition);
        }

        private Vector3 GetWorldPositionFromMouse(Vector2 mousePosition)
        {
            // Raycast from camera to ground plane
            Ray ray = mainCamera.ScreenPointToRay(mousePosition);
            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

            if (groundPlane.Raycast(ray, out float distance))
            {
                return ray.GetPoint(distance);
            }

            return transform.position;
        }

        private float CalculateSpeedMultiplier()
        {
            // Scale from 25% at minRatsToMove to 100% at maxRats
            float ratio = (float)(currentRats - minRatsToMove) / (maxRats - minRatsToMove);
            return 0.25f + (0.75f * Mathf.Clamp01(ratio));
        }
        
        private void UpdateCanMove()
        {
            canMove = currentRats >= minRatsToMove;
            OnCanMoveChanged?.Invoke(canMove);
        }
        
        /// <summary>
        /// Decreases rat count when a SentRat is launched.
        /// </summary>
        public void DecreaseRats()
        {
            currentRats = Mathf.Max(0, currentRats - 1);
            UpdateCanMove();
            OnRatsChanged?.Invoke(currentRats);
        }
        
        /// <summary>
        /// Increases rat count when a SentRat is recalled.
        /// </summary>
        public void IncreaseRats()
        {
            currentRats = Mathf.Min(maxRats, currentRats + 1);
            UpdateCanMove();
            OnRatsChanged?.Invoke(currentRats);
        }
        
        /// <summary>
        /// Gets current rat count.
        /// </summary>
        public int GetCurrentRats() => currentRats;
        
        /// <summary>
        /// Gets whether player can currently move.
        /// </summary>
        public bool CanMove() => canMove;
        
        /// <summary>
        /// Gets transform for SentRats to return to.
        /// </summary>
        public Transform GetReturnTarget() => transform;
    }
}
