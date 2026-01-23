using UnityEngine;
using UnityEngine.InputSystem;
using FindersCheesers.Player;

namespace FindersCheesers.SentRat
{
    /// <summary>
    /// Handles launching SentRats from KingRat to mouse reticle position.
    /// </summary>
    [AddComponentMenu("Finder's Cheesers/SentRat/SentRat Launcher")]
    public class SentRatLauncher : MonoBehaviour
    {
        [Header("Input References")]
        [SerializeField] private InputActionReference launchActionReference;
        [SerializeField] private InputActionReference pointActionReference;
        
        [Header("Prefabs")]
        [SerializeField] private GameObject sentRatPrefab;
        
        [Header("Settings")]
        [SerializeField] private float launchSpeed = 20f;
        [SerializeField] private float launchHeight = 1f;
        
        // Components
        private PlayerInput playerInput;
        private InputAction launchAction;
        private InputAction pointAction;
        private UnityEngine.Camera mainCamera;
        private KingRatController kingRatController;
        
        // Active SentRats
        private System.Collections.Generic.List<SentRat> activeSentRats = new System.Collections.Generic.List<SentRat>();
        
        // Events
        public event System.Action<SentRat> OnSentRatLaunched;
        
        private void Awake()
        {
            playerInput = GetComponent<PlayerInput>();
            mainCamera = UnityEngine.Camera.main;
            kingRatController = GetComponent<KingRatController>();
        }
        
        private void OnEnable()
        {
            if (playerInput != null)
            {
                if (launchActionReference != null)
                {
                    launchAction = playerInput.actions.FindAction(launchActionReference.action.id);
                    if (launchAction != null)
                    {
                        launchAction.performed += OnLaunchPerformed;
                    }
                }
                
                if (pointActionReference != null)
                {
                    pointAction = playerInput.actions.FindAction(pointActionReference.action.id);
                }
            }
        }
        
        private void OnDisable()
        {
            if (launchAction != null)
            {
                launchAction.performed -= OnLaunchPerformed;
            }
        }
        
        private void OnLaunchPerformed(InputAction.CallbackContext context)
        {
            // Check if we have rats available
            if (kingRatController != null && kingRatController.GetCurrentRats() > 0)
            {
                LaunchSentRat();
            }
        }
        
        private void LaunchSentRat()
        {
            // Get mouse position in world space
            Vector2 mousePosition = pointAction != null ? pointAction.ReadValue<Vector2>() : Vector2.zero;
            Vector3 targetPosition = GetWorldPositionFromMouse(mousePosition);
            
            // Calculate launch direction
            Vector3 launchDirection = (targetPosition - transform.position).normalized;
            launchDirection.y = 0; // Keep horizontal
            
            // Spawn SentRat
            GameObject sentRatObj = Instantiate(sentRatPrefab, transform.position + Vector3.up * launchHeight, Quaternion.identity);
            SentRat sentRat = sentRatObj.GetComponent<SentRat>();
            
            if (sentRat != null)
            {
                sentRat.Initialize(this, kingRatController.GetReturnTarget(), launchDirection * launchSpeed);
                activeSentRats.Add(sentRat);
                
                // Decrease rat count
                kingRatController.DecreaseRats();
                
                OnSentRatLaunched?.Invoke(sentRat);
            }
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
        
        /// <summary>
        /// Called by SentRat when it is recalled.
        /// </summary>
        public void OnSentRatRecalled(SentRat sentRat)
        {
            activeSentRats.Remove(sentRat);
        }
        
        /// <summary>
        /// Gets all active SentRats.
        /// </summary>
        public System.Collections.Generic.List<SentRat> GetActiveSentRats() => activeSentRats;
    }
}
