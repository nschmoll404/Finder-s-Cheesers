using UnityEngine;
using UnityEngine.InputSystem;

namespace FindersCheesers
{
    /// <summary>
    /// A component that tracks whether the Rat Pack is currently lifting the King Rat.
    /// Acts as an inventory-like component for managing the King Rat's grab state.
    /// Uses a detection overlap box to find the King Rat when in range and input to grab/release.
    /// </summary>
    [AddComponentMenu("Finders Cheesers/King Rat Grabber")]
    public class KingRatGrabber : MonoBehaviour
    {
        [Header("Input References")]
        [Tooltip("Reference to the Grab/Release input action")]
        [SerializeField]
        private InputActionReference grabActionReference;

        [Tooltip("Use PlayerInputSingleton to get PlayerInput")]
        [SerializeField]
        private bool usePlayerInputSingleton = false;

        [Header("Component References")]
        [Tooltip("Reference to the KingRatThrower component")]
        [SerializeField]
        private KingRatThrower kingRatThrower;

        [Header("King Rat Detection")]
        [Tooltip("Reference to the King Rat GameObject (optional, will auto-detect if not assigned)")]
        [SerializeField]
        private GameObject kingRat;

        [Tooltip("Layer mask for detecting the King Rat")]
        [SerializeField]
        private LayerMask kingRatLayerMask = 1;

        [Tooltip("Tag that identifies the King Rat GameObject")]
        [SerializeField]
        private string kingRatTag = "KingRat";

        [Tooltip("Size of the detection overlap box")]
        [SerializeField]
        private Vector3 detectionBoxSize = new Vector3(2f, 2f, 2f);

        [Tooltip("Offset of the detection box from the grabber")]
        [SerializeField]
        private Vector3 detectionBoxOffset = new Vector3(0f, 0f, 1f);

        [Header("Grab Settings")]
        [Tooltip("Offset position when King Rat is being carried")]
        [SerializeField]
        private Vector3 carryOffset = new Vector3(0f, 1f, 0f);

        [Tooltip("How smoothly the King Rat moves to the carry position")]
        [SerializeField]
        private float carrySmoothSpeed = 5f;

        [Header("Debug")]
        [Tooltip("Show debug information in the console")]
        [SerializeField]
        private bool debugMode = false;

        [Tooltip("Visualize grab state in Scene view")]
        [SerializeField]
        private bool visualizeGrab = true;

        [Tooltip("Visualize detection box in Scene view")]
        [SerializeField]
        private bool visualizeDetectionBox = true;

        // Component references
        private PlayerInput playerInput;
        private InputAction grabAction;
        private BoxCollider detectionCollider;

        // Current state
        private bool isGrabbing = false;
        private Rigidbody kingRatRigidbody;
        private Vector3 kingRatOriginalPosition;
        private Quaternion kingRatOriginalRotation;
        private bool kingRatWasKinematic;
        private bool kingRatInRange = false;

        /// <summary>
        /// Event fired when the King Rat is grabbed.
        /// </summary>
        public event System.Action OnKingRatGrabbed;

        /// <summary>
        /// Event fired when the King Rat is released.
        /// </summary>
        public event System.Action OnKingRatReleased;

        /// <summary>
        /// Gets whether the King Rat is currently being grabbed.
        /// </summary>
        public bool IsGrabbing => isGrabbing;

        /// <summary>
        /// Gets the King Rat GameObject.
        /// </summary>
        public GameObject KingRat => kingRat;

        /// <summary>
        /// Gets whether the King Rat is currently in range for grabbing.
        /// </summary>
        public bool KingRatInRange => kingRatInRange;

        /// <summary>
        /// Gets the KingRatThrower component.
        /// </summary>
        public KingRatThrower KingRatThrower => kingRatThrower;

        private void Awake()
        {
            // Create or get detection collider
            detectionCollider = GetComponent<BoxCollider>();
            if (detectionCollider == null)
            {
                detectionCollider = gameObject.AddComponent<BoxCollider>();
            }

            // Configure as trigger for detection
            detectionCollider.isTrigger = true;
            detectionCollider.size = detectionBoxSize;
            detectionCollider.center = detectionBoxOffset;
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
                        Debug.Log("[KingRatGrabber] Using PlayerInputSingleton for input.");
                    }
                }
                else
                {
                    Debug.LogError("[KingRatGrabber] PlayerInputSingleton is not initialized!");
                }
            }
            else
            {
                playerInput = GetComponent<PlayerInput>();
                
                if (playerInput == null)
                {
                    Debug.LogError("[KingRatGrabber] PlayerInput component not found on this GameObject!");
                }
            }

            // Get the grab action using the ID from InputActionReference
            if (playerInput != null && grabActionReference != null)
            {
                grabAction = playerInput.actions.FindAction(grabActionReference.action.id);
                
                if (grabAction == null)
                {
                    Debug.LogError("[KingRatGrabber] Grab action not found in PlayerInput actions!");
                }
            }
            else if (grabActionReference == null)
            {
                Debug.LogError("[KingRatGrabber] Grab Action Reference is not assigned!");
            }

            // Initialize King Rat if assigned in inspector
            if (kingRat != null)
            {
                kingRatRigidbody = kingRat.GetComponent<Rigidbody>();
                kingRatOriginalPosition = kingRat.transform.position;
                kingRatOriginalRotation = kingRat.transform.rotation;
                kingRatInRange = true;
            }
        }

        private void Update()
        {
            // Check for grab/release input
            if (grabAction != null)
            {
                if (grabAction.WasPressedThisFrame())
                {
                    HandleGrabInput();
                }
            }

            // Update King Rat position if being grabbed
            if (isGrabbing && kingRat != null)
            {
                Vector3 targetPosition = transform.position + carryOffset;
                kingRat.transform.position = Vector3.Lerp(
                    kingRat.transform.position,
                    targetPosition,
                    carrySmoothSpeed * Time.deltaTime
                );
            }

            // Update detection collider size and offset
            if (detectionCollider != null)
            {
                detectionCollider.size = detectionBoxSize;
                detectionCollider.center = detectionBoxOffset;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            // Check if the collider belongs to the King Rat
            if (IsKingRat(other.gameObject))
            {
                kingRat = other.gameObject;
                kingRatRigidbody = kingRat.GetComponent<Rigidbody>();
                kingRatInRange = true;

                if (debugMode)
                {
                    Debug.Log($"[KingRatGrabber] King Rat detected in range!");
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            // Check if the collider belongs to the King Rat
            if (IsKingRat(other.gameObject))
            {
                // Only clear if we're not currently grabbing
                if (!isGrabbing)
                {
                    kingRat = null;
                    kingRatRigidbody = null;
                    kingRatInRange = false;

                    if (debugMode)
                    {
                        Debug.Log($"[KingRatGrabber] King Rat left range!");
                    }
                }
                else
                {
                    kingRatInRange = false;
                }
            }
        }

        /// <summary>
        /// Checks if a GameObject is the King Rat.
        /// </summary>
        private bool IsKingRat(GameObject obj)
        {
            if (obj == null)
            {
                return false;
            }

            // Check by tag
            if (!string.IsNullOrEmpty(kingRatTag) && obj.CompareTag(kingRatTag))
            {
                return true;
            }

            // Check by layer mask
            if (kingRatLayerMask != (kingRatLayerMask | (1 << obj.layer)))
            {
                return false;
            }

            // If both checks pass, it's considered the King Rat
            return true;
        }

        /// <summary>
        /// Handles grab/release input.
        /// </summary>
        private void HandleGrabInput()
        {
            if (isGrabbing)
            {
                // Release the King Rat
                ReleaseKingRat();
            }
            else
            {
                // Grab the King Rat
                GrabKingRat();
            }
        }

        /// <summary>
        /// Grabs the King Rat.
        /// </summary>
        /// <returns>True if the King Rat was successfully grabbed, false otherwise.</returns>
        public bool GrabKingRat()
        {
            if (kingRat == null)
            {
                if (debugMode)
                {
                    Debug.LogWarning("[KingRatGrabber] King Rat is not in range!");
                }
                return false;
            }

            if (isGrabbing)
            {
                if (debugMode)
                {
                    Debug.LogWarning("[KingRatGrabber] Already grabbing the King Rat!");
                }
                return false;
            }

            // Store original state
            kingRatOriginalPosition = kingRat.transform.position;
            kingRatOriginalRotation = kingRat.transform.rotation;

            // Disable physics on King Rat while being carried
            if (kingRatRigidbody != null)
            {
                kingRatWasKinematic = kingRatRigidbody.isKinematic;
                kingRatRigidbody.isKinematic = true;
            }

            isGrabbing = true;
            OnKingRatGrabbed?.Invoke();

            if (debugMode)
            {
                Debug.Log("[KingRatGrabber] King Rat grabbed!");
            }

            return true;
        }

        /// <summary>
        /// Releases the King Rat.
        /// </summary>
        /// <returns>True if the King Rat was successfully released, false otherwise.</returns>
        public bool ReleaseKingRat()
        {
            if (kingRat == null)
            {
                Debug.LogWarning("[KingRatGrabber] King Rat is not assigned!");
                return false;
            }

            if (!isGrabbing)
            {
                if (debugMode)
                {
                    Debug.LogWarning("[KingRatGrabber] Not currently grabbing the King Rat!");
                }
                return false;
            }

            // Re-enable physics on King Rat
            if (kingRatRigidbody != null)
            {
                kingRatRigidbody.isKinematic = kingRatWasKinematic;
            }

            isGrabbing = false;
            OnKingRatReleased?.Invoke();

            // Clear King Rat reference if out of range
            if (!kingRatInRange)
            {
                kingRat = null;
                kingRatRigidbody = null;
            }

            if (debugMode)
            {
                Debug.Log("[KingRatGrabber] King Rat released!");
            }

            return true;
        }

        /// <summary>
        /// Sets the King Rat GameObject manually (optional).
        /// </summary>
        /// <param name="newKingRat">The new King Rat GameObject.</param>
        public void SetKingRat(GameObject newKingRat)
        {
            if (isGrabbing)
            {
                Debug.LogWarning("[KingRatGrabber] Cannot change King Rat while grabbing!");
                return;
            }

            kingRat = newKingRat;

            if (kingRat != null)
            {
                kingRatRigidbody = kingRat.GetComponent<Rigidbody>();
                kingRatOriginalPosition = kingRat.transform.position;
                kingRatOriginalRotation = kingRat.transform.rotation;
                kingRatInRange = true;
            }
            else
            {
                kingRatRigidbody = null;
                kingRatInRange = false;
            }
        }

        /// <summary>
        /// Sets the carry offset.
        /// </summary>
        /// <param name="offset">The new carry offset.</param>
        public void SetCarryOffset(Vector3 offset)
        {
            carryOffset = offset;
        }

        /// <summary>
        /// Sets the carry smooth speed.
        /// </summary>
        /// <param name="speed">The new carry smooth speed.</param>
        public void SetCarrySmoothSpeed(float speed)
        {
            carrySmoothSpeed = Mathf.Max(0.1f, speed);
        }

        /// <summary>
        /// Sets the detection box size.
        /// </summary>
        /// <param name="size">The new detection box size.</param>
        public void SetDetectionBoxSize(Vector3 size)
        {
            detectionBoxSize = size;
        }

        /// <summary>
        /// Sets the detection box offset.
        /// </summary>
        /// <param name="offset">The new detection box offset.</param>
        public void SetDetectionBoxOffset(Vector3 offset)
        {
            detectionBoxOffset = offset;
        }

        /// <summary>
        /// Sets the KingRatThrower component.
        /// </summary>
        /// <param name="thrower">The KingRatThrower component.</param>
        public void SetKingRatThrower(KingRatThrower thrower)
        {
            kingRatThrower = thrower;
        }

        private void OnDrawGizmos()
        {
            if (!visualizeGrab && !visualizeDetectionBox)
            {
                return;
            }

            // Draw detection box
            if (visualizeDetectionBox)
            {
                Gizmos.color = kingRatInRange ? Color.green : Color.yellow;
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawWireCube(detectionBoxOffset, detectionBoxSize);
                Gizmos.matrix = Matrix4x4.identity;
            }

            if (kingRat == null)
            {
                return;
            }

            // Draw carry position indicator
            if (visualizeGrab)
            {
                Gizmos.color = isGrabbing ? Color.green : Color.red;
                Vector3 carryPosition = transform.position + carryOffset;
                Gizmos.DrawWireSphere(carryPosition, 0.3f);

                // Draw line to King Rat
                if (isGrabbing)
                {
                    Gizmos.DrawLine(transform.position, carryPosition);
                    Gizmos.DrawLine(carryPosition, kingRat.transform.position);
                }
            }
        }

        private void Reset()
        {
            carryOffset = new Vector3(0f, 1f, 0f);
            carrySmoothSpeed = 5f;
            detectionBoxSize = new Vector3(2f, 2f, 2f);
            detectionBoxOffset = new Vector3(0f, 0f, 1f);
            kingRatTag = "KingRat";
        }
    }
}
