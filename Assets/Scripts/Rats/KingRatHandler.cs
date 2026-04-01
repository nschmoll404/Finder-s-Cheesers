using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FindersCheesers
{
    /// <summary>
    /// Maps a layer mask to a specific reticle prefab.
    /// When the throw target hovers over a collider in the layer mask,
    /// the associated reticle prefab is displayed instead of the default.
    /// </summary>
    [System.Serializable]
    public class TargetReticleMapping
    {
        [Tooltip("Layer mask for detecting which colliders should use this reticle")]
        public LayerMask layerMask;

        [Tooltip("Reticle prefab to display when hovering over objects in the layer mask")]
        public GameObject reticlePrefab;
    }

    /// <summary>
    /// Defines when to update the grabbed King Rat's position.
    /// </summary>
    public enum KingRatUpdateMode
    {
        Update,
        FixedUpdate,
        LateUpdate
    }

    /// <summary>
    /// A unified component that handles both grabbing and throwing King Rat.
    /// Manages King Rat's grab state, detection, and throw mechanics with physics arc visualization.
    /// The throw distance is based on number of rats in the inventory - more rats = further throw.
    /// </summary>
    [AddComponentMenu("Finders Cheesers/King Rat Handler")]
    public class KingRatHandler : MonoBehaviour
    {
        #region Grab Settings

        [Header("Grab Input")]
        [Tooltip("Reference to Grab/Release input action")]
        [SerializeField]
        private InputActionReference grabActionReference;

        [Header("King Rat Detection")]
        [Tooltip("Layer mask for detecting King Rat")]
        [SerializeField]
        private LayerMask kingRatLayerMask = 1;

        [Tooltip("Tag that identifies King Rat GameObject")]
        [SerializeField]
        private string kingRatTag = "KingRat";

        [Tooltip("Layer mask for detecting throwable objects")]
        [SerializeField]
        private LayerMask throwableLayerMask = -1;

        [Tooltip("Layer mask for detecting ThrowableProducer objects")]
        [SerializeField]
        private LayerMask producerLayerMask = -1;

        [Tooltip("Size of detection overlap box")]
        [SerializeField]
        private Vector3 detectionBoxSize = new Vector3(2f, 2f, 2f);

        [Tooltip("Offset of detection box from grabber")]
        [SerializeField]
        private Vector3 detectionBoxOffset = new Vector3(0f, 0f, 1f);

        [Header("Carry Settings")]
        [Tooltip("Minimum number of rats required to carry King Rat")]
        [SerializeField]
        private int minRatsToCarry = 2;

        [Tooltip("Offset position when King Rat is being carried")]
        [SerializeField]
        private Vector3 carryOffset = new Vector3(0f, 1f, 0f);

        [Tooltip("How smoothly King Rat moves to the carry position")]
        [SerializeField]
        private float carrySmoothSpeed = 5f;

        [Tooltip("When to update the grabbed King Rat's position")]
        [SerializeField]
        private KingRatUpdateMode updateMode = KingRatUpdateMode.Update;

        [Tooltip("Switch rat inventory to crowd mode when carrying King Rat")]
        [SerializeField]
        private bool switchToCrowdOnCarry = true;

        [Tooltip("The default following mode to return to when not carrying King Rat")]
        [SerializeField]
        private RatInventory.RatFollowingMode defaultFollowingMode = RatInventory.RatFollowingMode.Trail;

        #endregion

        #region Throw Settings

        [Header("Throw Input")]
        [Tooltip("Reference to the Pointer input action for getting mouse position")]
        [SerializeField]
        private InputActionReference pointerActionReference;

        [Tooltip("Reference to the Throw input action (click to throw)")]
        [SerializeField]
        private InputActionReference throwActionReference;

        [Header("Camera Settings")]
        [Tooltip("Camera to use for raycasting to world position")]
        [SerializeField]
        private Camera mainCamera;

        [Tooltip("Layer mask for raycast to find target position")]
        [SerializeField]
        private LayerMask targetLayerMask = -1; // -1 = Everything

        [Header("Rat Pack")]
        [Tooltip("Reference to the RatPackController component")]
        [SerializeField]
        private RatPackController ratPackController;

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

        [Tooltip("Color of the arc line when target is within valid distance")]
        [SerializeField]
        private Color arcColor = Color.yellow;

        [Tooltip("Color of the arc line when target is beyond valid distance")]
        [SerializeField]
        private Color invalidDistanceColor = Color.red;

        [Header("Target Reticle")]
        [Tooltip("Default reticle prefab shown when no specific layer is detected at the target")]
        [SerializeField]
        private GameObject defaultReticlePrefab;

        [Tooltip("List of reticle mappings that override the default reticle when the target hovers over a collider in the matching layer")]
        [SerializeField]
        private List<TargetReticleMapping> targetReticleMappings = new List<TargetReticleMapping>();

        [Tooltip("Base launch speed for the King Rat")]
        [SerializeField]
        private float baseLaunchSpeed = 10f;

        [Tooltip("Additional launch speed per rat in inventory")]
        [SerializeField]
        private float speedPerRat = 2f;

        [Tooltip("Maximum launch speed")]
        [SerializeField]
        private float maxLaunchSpeed = 30f;

        [Tooltip("Launch angle in degrees")]
        [SerializeField]
        private float launchAngle = 45f;

        [Tooltip("Duration of the throw animation")]
        [SerializeField]
        private float throwDuration = 1f;

        [Tooltip("Height offset for the launch position")]
        [SerializeField]
        private float launchHeightOffset = 1f;

        [Header("Arc Collision")]
        [Tooltip("Enable collision detection along the throw arc")]
        [SerializeField]
        private bool checkArcCollision = true;

        [Tooltip("Layer mask for detecting obstacles along the throw arc (walls, floors, etc.)")]
        [SerializeField]
        private LayerMask arcCollisionLayerMask = -1; // -1 = Everything

        [Tooltip("Color of the arc line when it would hit an obstacle")]
        [SerializeField]
        private Color blockedArcColor = Color.red;

        [Header("Distance Settings")]
        [Tooltip("Base throw distance without any rats")]
        [SerializeField]
        private float baseThrowDistance = 5f;

        [Tooltip("Additional throw distance per rat in inventory")]
        [SerializeField]
        private float distancePerRat = 1f;

        [Tooltip("Maximum throw distance")]
        [SerializeField]
        private float maxThrowDistance = 20f;

        [Tooltip("Lock target to max distance when it exceeds the valid range")]
        [SerializeField]
        private bool lockTargetToMaxDistance = false;

        #endregion

        #region Debug Settings

        [Header("Debug")]
        [Tooltip("Use PlayerInputSingleton to get PlayerInput")]
        [SerializeField]
        private bool usePlayerInputSingleton = false;

        [Tooltip("Show debug information in the console")]
        [SerializeField]
        private bool debugMode = false;

        [Tooltip("Visualize grab state in Scene view")]
        [SerializeField]
        private bool visualizeGrab = true;

        [Tooltip("Visualize detection box in Scene view")]
        [SerializeField]
        private bool visualizeDetectionBox = true;

        [Tooltip("Visualize throw target in Scene view")]
        [SerializeField]
        private bool visualizeTarget = true;

        #endregion

        #region Component References

        private PlayerInput playerInput;
        private InputAction grabAction;
        private InputAction pointerAction;
        private InputAction throwAction;
        private GameObject defaultReticleInstance;
        private Dictionary<GameObject, GameObject> reticleInstances = new Dictionary<GameObject, GameObject>();
        private GameObject activeReticleInstance;
        private Collider targetHitCollider;
        private IThrowable throwable;
        private GameObject currentThrowableObject; // Track any throwable object being held (not just King Rat)

        #endregion

        #region State Variables

        // Grab state
        private bool isGrabbing = false;
        private GameObject detectedKingRat;
        private Rigidbody kingRatRigidbody;
        private Vector3 kingRatOriginalPosition;
        private Quaternion kingRatOriginalRotation;
        private bool kingRatWasKinematic;
        private bool kingRatInRange = false;
        private ThrowableProducer detectedProducer;
        private bool producerInRange = false;

        // Following mode state
        private RatInventory.RatFollowingMode previousFollowingMode;

        // Throw state
        private Vector2 pointerPosition;
        private Vector3? targetPosition;
        private Vector3[] arcPoints;
        private bool isTargetValid;
        private Vector3 clampedTargetPosition;

        // Arc collision state
        private bool isArcBlocked;
        private Vector3? arcHitPoint;
        private int arcHitSegment;
        private Vector3 originalTargetPosition;
        private bool originalTargetValid; // Track original target validity before collision adjustment

        #endregion

        #region Events

        /// <summary>
        /// Event fired when King Rat is grabbed.
        /// </summary>
        public event System.Action OnKingRatGrabbed;

        /// <summary>
        /// Event fired when King Rat is released.
        /// </summary>
        public event System.Action OnKingRatReleased;

        /// <summary>
        /// Event fired when the King Rat is thrown.
        /// </summary>
        public event System.Action<Vector3> OnKingRatThrown;

        /// <summary>
        /// Event fired when the King Rat lands.
        /// </summary>
        public event System.Action<Vector3> OnKingRatLanded;

        #endregion

        #region Properties

        /// <summary>
        /// Gets whether King Rat is currently being grabbed.
        /// </summary>
        public bool IsGrabbing => isGrabbing;

        /// <summary>
        /// Gets the detected King Rat GameObject (null if not in range).
        /// </summary>
        public GameObject DetectedKingRat => kingRatInRange ? detectedKingRat : null;

        /// <summary>
        /// Gets whether King Rat is currently in range for grabbing.
        /// </summary>
        public bool KingRatInRange => kingRatInRange;

        /// <summary>
        /// Gets the King Rat GameObject (null if not grabbed).
        /// </summary>
        public GameObject KingRat => isGrabbing ? detectedKingRat : null;

        /// <summary>
        /// Gets whether a ThrowableProducer is currently in range for interaction.
        /// </summary>
        public bool ProducerInRange => producerInRange;

        /// <summary>
        /// Gets the detected ThrowableProducer (null if not in range).
        /// </summary>
        public ThrowableProducer DetectedProducer => producerInRange ? detectedProducer : null;

        /// <summary>
        /// Gets whether the King Rat is currently being thrown.
        /// </summary>
        public bool IsThrowing => throwable != null && throwable.IsThrowing;

        #endregion

        #region Unity Lifecycle

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

            // Instantiate default reticle prefab if assigned
            if (defaultReticlePrefab != null)
            {
                defaultReticleInstance = Instantiate(defaultReticlePrefab);
                defaultReticleInstance.SetActive(false);
                reticleInstances[defaultReticlePrefab] = defaultReticleInstance;
            }

            // Instantiate reticle prefabs from mappings
            foreach (var mapping in targetReticleMappings)
            {
                if (mapping.reticlePrefab != null && !reticleInstances.ContainsKey(mapping.reticlePrefab))
                {
                    GameObject instance = Instantiate(mapping.reticlePrefab);
                    instance.SetActive(false);
                    reticleInstances[mapping.reticlePrefab] = instance;
                }
            }
        }

        private void Start()
        {
            InitializePlayerInput();
            InitializeInputActions();
            InitializeComponents();
        }

        private void Update()
        {
            // Check if we need to drop King Rat due to insufficient rats
            if (isGrabbing && ratInventory != null && ratInventory.RatCount < minRatsToCarry)
            {
                if (debugMode)
                {
                    Debug.Log($"[KingRatHandler] Rat count ({ratInventory.RatCount}) below minimum ({minRatsToCarry}). Dropping King Rat!");
                }

                // Drop King Rat
                ReleaseKingRat();
            }

            // Check if the held object has been destroyed (e.g., bomb exploded)
            if (isGrabbing && detectedKingRat == null)
            {
                if (debugMode)
                {
                    Debug.LogWarning("[KingRatHandler] Held object is null! Resetting grab state.");
                }

                // Reset grab state
                isGrabbing = false;
                OnKingRatReleased?.Invoke();

                // Switch back to the default following mode
                if (switchToCrowdOnCarry && ratInventory != null)
                {
                    ratInventory.SetFollowingMode(defaultFollowingMode);

                    if (debugMode)
                    {
                        Debug.Log($"[KingRatHandler] Switched back to {defaultFollowingMode} following mode (held object destroyed)");
                    }
                }

                // Clear references
                kingRatRigidbody = null;
                throwable = null;

                if (debugMode)
                {
                    Debug.Log("[KingRatHandler] Grab state reset. Can now pick up objects again.");
                }
            }

            // Check for grab/release input
            if (grabAction != null)
            {
                if (grabAction.WasPressedThisFrame())
                {
                    HandleGrabInput();
                }
            }

            // Read pointer position
            if (pointerAction != null)
            {
                pointerPosition = pointerAction.ReadValue<Vector2>();
            }

            // Update arc visualization first (must be before throw input check to get collision point)
            UpdateArcVisualization();

            // Check for throw input
            if (throwAction != null && !(throwable != null && throwable.IsThrowing))
            {
                if (throwAction.WasPressedThisFrame())
                {
                    TryThrowKingRat();
                }
            }

            // Update King Rat position if being grabbed (only in Update mode)
            if (updateMode == KingRatUpdateMode.Update)
            {
                UpdateKingRatPosition();
            }

            // Update target reticle position
            UpdateTargetReticle();
        }

        private void FixedUpdate()
        {
            // Update King Rat position if being grabbed (only in FixedUpdate mode)
            if (updateMode == KingRatUpdateMode.FixedUpdate)
            {
                UpdateKingRatPosition();
            }

            // Update target position
            UpdateTargetPosition();

            // Detect King Rat using overlap box
            DetectKingRatWithOverlapBox();
        }

        private void LateUpdate()
        {
            // Update King Rat position if being grabbed (only in LateUpdate mode)
            if (updateMode == KingRatUpdateMode.LateUpdate)
            {
                UpdateKingRatPosition();
            }

            // Update target position
            UpdateTargetPosition();
        }

        /// <summary>
        /// Updates the King Rat's position when being grabbed.
        /// Uses Time.deltaTime for Update mode and Time.fixedDeltaTime for FixedUpdate mode.
        /// </summary>
        private void UpdateKingRatPosition()
        {
            if (isGrabbing && detectedKingRat != null)
            {
                // Check if the throwable object has a custom carry offset
                Vector3 effectiveCarryOffset = carryOffset;
                ThrowableObject throwableObj = detectedKingRat.GetComponent<ThrowableObject>();
                if (throwableObj != null && throwableObj.CarryOffset != Vector3.zero)
                {
                    effectiveCarryOffset = throwableObj.CarryOffset;
                }

                Vector3 targetPosition = transform.position + effectiveCarryOffset;
                float deltaTime = (updateMode == KingRatUpdateMode.FixedUpdate) ? Time.fixedDeltaTime : Time.deltaTime;
                detectedKingRat.transform.position = Vector3.Lerp(
                    detectedKingRat.transform.position,
                    targetPosition,
                    carrySmoothSpeed * deltaTime
                );
            }
        }

        /// <summary>
        /// Detects King Rat and ThrowableProducer using an overlap box in FixedUpdate.
        /// </summary>
        private void DetectKingRatWithOverlapBox()
        {
            // Skip detection updates while already grabbing to prevent auto-swapping
            // with other throwable objects in the detection area
            if (isGrabbing)
            {
                return;
            }

            // Calculate the center of the overlap box in world space
            Vector3 boxCenter = transform.position + transform.TransformDirection(detectionBoxOffset);

            // Detect King Rat
            Collider[] kingRatColliders = Physics.OverlapBox(
                boxCenter,
                detectionBoxSize * 0.5f,
                transform.rotation,
                kingRatLayerMask,
                QueryTriggerInteraction.Ignore
            );

            // Find the King Rat among colliders
            GameObject newDetectedKingRat = null;
            foreach (Collider collider in kingRatColliders)
            {
                if (IsKingRat(collider.gameObject))
                {
                    newDetectedKingRat = collider.gameObject;
                    break;
                }
            }

            // Update King Rat detection state
            bool wasInRange = kingRatInRange;
            kingRatInRange = newDetectedKingRat != null;

            // Handle King Rat entering range
            if (kingRatInRange && !wasInRange && newDetectedKingRat != detectedKingRat)
            {
                detectedKingRat = newDetectedKingRat;
                kingRatRigidbody = detectedKingRat.GetComponent<Rigidbody>();
                throwable = detectedKingRat.GetComponent<IThrowable>();

                // Subscribe to IThrowable events
                if (throwable != null)
                {
                    throwable.OnLanded += OnKingRatLanded;
                }

                if (debugMode)
                {
                    Debug.Log($"[KingRatHandler] King Rat detected in range!");
                }
            }
            // Handle King Rat leaving range (only if not currently grabbing)
            else if (!kingRatInRange && wasInRange && !isGrabbing)
            {
                // Unsubscribe from IThrowable events
                if (throwable != null)
                {
                    throwable.OnLanded -= OnKingRatLanded;
                }

                detectedKingRat = null;
                kingRatRigidbody = null;
                throwable = null;

                if (debugMode)
                {
                    Debug.Log($"[KingRatHandler] King Rat left range!");
                }
            }

            // Detect ThrowableProducer
            Collider[] producerColliders = Physics.OverlapBox(
                boxCenter,
                detectionBoxSize * 0.5f,
                transform.rotation,
                producerLayerMask,
                QueryTriggerInteraction.Ignore
            );

            // Find the ThrowableProducer among colliders
            ThrowableProducer newDetectedProducer = null;
            foreach (Collider collider in producerColliders)
            {
                ThrowableProducer producer = collider.GetComponent<ThrowableProducer>();
                if (producer != null && producer.CanSpawn)
                {
                    newDetectedProducer = producer;
                    break;
                }
            }

            // Update Producer detection state
            bool producerWasInRange = producerInRange;
            producerInRange = newDetectedProducer != null;

            // Handle Producer entering range
            if (producerInRange && !producerWasInRange && newDetectedProducer != detectedProducer)
            {
                detectedProducer = newDetectedProducer;

                if (debugMode)
                {
                    Debug.Log($"[KingRatHandler] ThrowableProducer detected in range!");
                }
            }
            // Handle Producer leaving range
            else if (!producerInRange && producerWasInRange)
            {
                detectedProducer = null;

                if (debugMode)
                {
                    Debug.Log($"[KingRatHandler] ThrowableProducer left range!");
                }
            }
        }

        private void OnDestroy()
        {
            // Clean up all reticle instances
            foreach (var kvp in reticleInstances)
            {
                if (kvp.Value != null)
                {
                    Destroy(kvp.Value);
                }
            }
            reticleInstances.Clear();
            defaultReticleInstance = null;
            activeReticleInstance = null;

            // Unsubscribe from IThrowable events
            if (throwable != null)
            {
                throwable.OnLanded -= OnKingRatLanded;
            }

            // Unsubscribe from bomb explosion event if it was a bomb
            if (currentThrowableObject != null)
            {
                Bomb bomb = currentThrowableObject.GetComponent<Bomb>();
                if (bomb != null)
                {
                    bomb.OnExploded -= OnBombExploded;

                    if (debugMode)
                    {
                        Debug.Log("[KingRatHandler] Unsubscribed from bomb explosion event in OnDestroy!");
                    }
                }
            }

            // Unsubscribe from RatInventory events
            if (ratInventory != null)
            {
                ratInventory.OnRatRemoved -= OnRatRemoved;
            }
        }

        private void OnDrawGizmos()
        {
            // Draw detection box (overlap box)
            if (visualizeDetectionBox)
            {
                Gizmos.color = kingRatInRange ? Color.green : Color.yellow;
                Vector3 boxCenter = transform.position + transform.TransformDirection(detectionBoxOffset);
                Gizmos.matrix = Matrix4x4.TRS(boxCenter, transform.rotation, Vector3.one);
                Gizmos.DrawWireCube(Vector3.zero, detectionBoxSize);
                Gizmos.matrix = Matrix4x4.identity;
            }

            // Draw carry position indicator
            if (visualizeGrab && detectedKingRat != null)
            {
                Gizmos.color = isGrabbing ? Color.green : Color.red;
                Vector3 carryPosition = transform.position + carryOffset;
                Gizmos.DrawWireSphere(carryPosition, 0.3f);

                // Draw line to King Rat
                if (isGrabbing)
                {
                    Gizmos.DrawLine(transform.position, carryPosition);
                    Gizmos.DrawLine(carryPosition, detectedKingRat.transform.position);
                }
            }

            // Draw target position
            if (visualizeTarget && targetPosition.HasValue)
            {
                Gizmos.color = isTargetValid ? Color.green : Color.red;
                Gizmos.DrawWireSphere(targetPosition.Value, 0.3f);
                Gizmos.DrawLine(transform.position + Vector3.up * launchHeightOffset, targetPosition.Value);

                // Draw max throw distance circle
                float maxDistance = Application.isPlaying ? CalculateMaxThrowDistance() : (baseThrowDistance + (ratInventory != null ? ratInventory.Count * distancePerRat : 0));
                maxDistance = Mathf.Min(maxDistance, maxThrowDistance);
                Vector3 launchPos = transform.position + Vector3.up * launchHeightOffset;
                Gizmos.color = new Color(1f, 1f, 0f, 0.3f); // Semi-transparent yellow
                Gizmos.DrawWireSphere(launchPos, maxDistance);

                // Draw arc if in play mode
                if (Application.isPlaying && arcPoints != null && arcPoints.Length > 0)
                {
                    Gizmos.color = isTargetValid ? Color.yellow : Color.red;
                    for (int i = 0; i < arcPoints.Length - 1; i++)
                    {
                        Gizmos.DrawLine(arcPoints[i], arcPoints[i + 1]);
                    }
                }
            }

            // Draw King Rat indicator
            if (detectedKingRat != null)
            {
                bool isThrowing = throwable != null && throwable.IsThrowing;
                Gizmos.color = isThrowing ? Color.magenta : (isGrabbing ? Color.green : Color.gray);
                Gizmos.DrawWireSphere(detectedKingRat.transform.position, 0.5f);
            }
        }

        private void Reset()
        {
            // Grab settings
            minRatsToCarry = 2;
            carryOffset = new Vector3(0f, 1f, 0f);
            carrySmoothSpeed = 5f;
            detectionBoxSize = new Vector3(2f, 2f, 2f);
            detectionBoxOffset = new Vector3(0f, 0f, 1f);
            kingRatTag = "KingRat";
            switchToCrowdOnCarry = true;
            defaultFollowingMode = RatInventory.RatFollowingMode.Trail;

            // Throw settings
            baseLaunchSpeed = 10f;
            speedPerRat = 2f;
            maxLaunchSpeed = 30f;
            launchAngle = 45f;
            throwDuration = 1f;
            launchHeightOffset = 1f;
            arcSegments = 30;
            arcColor = Color.yellow;
            baseThrowDistance = 5f;
            distancePerRat = 1f;
            maxThrowDistance = 20f;
            lockTargetToMaxDistance = false;
            invalidDistanceColor = Color.red;
        }

        #endregion

        #region Initialization

        private void InitializePlayerInput()
        {
            if (usePlayerInputSingleton)
            {
                if (PlayerInputSingleton.IsInitialized())
                {
                    playerInput = PlayerInputSingleton.Instance.PlayerInput;

                    if (debugMode)
                    {
                        Debug.Log("[KingRatHandler] Using PlayerInputSingleton for input.");
                    }
                }
                else
                {
                    Debug.LogError("[KingRatHandler] PlayerInputSingleton is not initialized!");
                }
            }
            else
            {
                playerInput = GetComponent<PlayerInput>();

                if (playerInput == null)
                {
                    Debug.LogError("[KingRatHandler] PlayerInput component not found on this GameObject!");
                }
            }
        }

        private void InitializeInputActions()
        {
            // Get grab action using the ID from InputActionReference
            if (playerInput != null && grabActionReference != null)
            {
                grabAction = playerInput.actions.FindAction(grabActionReference.action.id);

                if (grabAction == null)
                {
                    Debug.LogError("[KingRatHandler] Grab action not found in PlayerInput actions!");
                }
            }
            else if (grabActionReference == null)
            {
                Debug.LogError("[KingRatHandler] Grab Action Reference is not assigned!");
            }

            // Get the pointer action using the ID from InputActionReference
            if (playerInput != null && pointerActionReference != null)
            {
                pointerAction = playerInput.actions.FindAction(pointerActionReference.action.id);

                if (pointerAction == null)
                {
                    Debug.LogError("[KingRatHandler] Pointer action not found in PlayerInput actions!");
                }
            }
            else if (pointerActionReference == null)
            {
                Debug.LogError("[KingRatHandler] Pointer Action Reference is not assigned!");
            }

            // Get the throw action using the ID from InputActionReference
            if (playerInput != null && throwActionReference != null)
            {
                throwAction = playerInput.actions.FindAction(throwActionReference.action.id);

                if (throwAction == null)
                {
                    Debug.LogError("[KingRatHandler] Throw action not found in PlayerInput actions!");
                }
            }
            else if (throwActionReference == null)
            {
                Debug.LogError("[KingRatHandler] Throw Action Reference is not assigned!");
            }
        }

        private void InitializeComponents()
        {
            // Get RatPackController component
            if (ratPackController == null)
            {
                ratPackController = GetComponent<RatPackController>();

                if (ratPackController == null)
                {
                    Debug.LogError("[KingRatHandler] RatPackController component not found on this GameObject!");
                }
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
                    Debug.LogError("[KingRatHandler] RatInventory component not found!");
                }
                else
                {
                    // Subscribe to rat removal events to check minimum requirement
                    ratInventory.OnRatRemoved += OnRatRemoved;
                }
            }

            // Get camera if not assigned
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }
        }

        #endregion

        #region Grab Methods

        /// <summary>
        /// Handles grab/release input.
        /// </summary>
        private void HandleGrabInput()
        {
            if (isGrabbing)
            {
                // Release King Rat
                ReleaseKingRat();
            }
            else
            {
                // Check if we should interact with a producer first
                if (producerInRange && detectedProducer != null)
                {
                    // Spawn throwable from producer and grab it
                    if (SpawnAndGrabFromProducer())
                    {
                        return;
                    }
                }

                // Grab King Rat
                GrabKingRat();
            }
        }

        /// <summary>
        /// Grabs King Rat.
        /// </summary>
        /// <returns>True if King Rat was successfully grabbed, false otherwise.</returns>
        public bool GrabKingRat()
        {
            // Check if King Rat is in range (within overlap box)
            if (!kingRatInRange || detectedKingRat == null)
            {
                if (debugMode)
                {
                    Debug.LogWarning("[KingRatHandler] King Rat is not in range!");
                }
                return false;
            }

            if (isGrabbing)
            {
                if (debugMode)
                {
                    Debug.LogWarning("[KingRatHandler] Already grabbing King Rat!");
                }
                return false;
            }

            // Check if we have enough rats to carry King Rat
            if (ratInventory != null && ratInventory.RatCount < minRatsToCarry)
            {
                if (debugMode)
                {
                    Debug.LogWarning($"[KingRatHandler] Not enough rats to carry King Rat! Need {minRatsToCarry}, have {ratInventory.RatCount}.");
                }
                return false;
            }

            // Store original state
            kingRatOriginalPosition = detectedKingRat.transform.position;
            kingRatOriginalRotation = detectedKingRat.transform.rotation;

            // Disable physics on King Rat while being carried
            if (kingRatRigidbody != null)
            {
                kingRatWasKinematic = kingRatRigidbody.isKinematic;
                kingRatRigidbody.isKinematic = true;
            }

            isGrabbing = true;
            OnKingRatGrabbed?.Invoke();

            // Switch to crowd following mode when carrying King Rat
            if (switchToCrowdOnCarry && ratInventory != null)
            {
                previousFollowingMode = ratInventory.FollowingMode;
                ratInventory.UseCrowdFollowing();

                if (debugMode)
                {
                    Debug.Log($"[KingRatHandler] Switched to crowd following mode (was: {previousFollowingMode})");
                }
            }

            // Call OnPickup on ThrowableObject if it has one
            ThrowableObject throwableObj = detectedKingRat.GetComponent<ThrowableObject>();
            if (throwableObj != null)
            {
                throwableObj.OnPickup();
                currentThrowableObject = detectedKingRat;

                // Check if this is a bomb and subscribe to its explosion event
                Bomb bomb = detectedKingRat.GetComponent<Bomb>();
                if (bomb != null)
                {
                    bomb.OnExploded += OnBombExploded;

                    if (debugMode)
                    {
                        Debug.Log("[KingRatHandler] Subscribed to bomb explosion event!");
                    }
                }

                if (debugMode)
                {
                    Debug.Log("[KingRatHandler] Called OnPickup on ThrowableObject!");
                }
            }

            if (debugMode)
            {
                Debug.Log("[KingRatHandler] King Rat grabbed!");
            }

            return true;
        }

        /// <summary>
        /// Releases King Rat.
        /// </summary>
        /// <returns>True if King Rat was successfully released, false otherwise.</returns>
        public bool ReleaseKingRat()
        {
            if (detectedKingRat == null)
            {
                Debug.LogWarning("[KingRatHandler] King Rat is not assigned!");
                return false;
            }

            if (!isGrabbing)
            {
                if (debugMode)
                {
                    Debug.LogWarning("[KingRatHandler] Not currently grabbing King Rat!");
                }
                return false;
            }

            // Drop the King Rat (ensures rigidbody is not kinematic so it falls)
            if (throwable != null)
            {
                throwable.Drop();
            }
            else if (kingRatRigidbody != null)
            {
                kingRatRigidbody.isKinematic = false;
            }

            isGrabbing = false;
            OnKingRatReleased?.Invoke();

            // Switch back to the default following mode when releasing King Rat
            if (switchToCrowdOnCarry && ratInventory != null)
            {
                ratInventory.SetFollowingMode(defaultFollowingMode);

                if (debugMode)
                {
                    Debug.Log($"[KingRatHandler] Switched back to {defaultFollowingMode} following mode");
                }
            }

            // Clear King Rat reference if out of range
            if (!kingRatInRange)
            {
                // Unsubscribe from IThrowable events
                if (throwable != null)
                {
                    throwable.OnLanded -= OnKingRatLanded;
                }

                // Unsubscribe from bomb explosion event if it was a bomb
                if (currentThrowableObject != null)
                {
                    Bomb bomb = currentThrowableObject.GetComponent<Bomb>();
                    if (bomb != null)
                    {
                        bomb.OnExploded -= OnBombExploded;

                        if (debugMode)
                        {
                            Debug.Log("[KingRatHandler] Unsubscribed from bomb explosion event!");
                        }
                    }
                }

                detectedKingRat = null;
                kingRatRigidbody = null;
                throwable = null;
                currentThrowableObject = null;
            }

            if (debugMode)
            {
                Debug.Log("[KingRatHandler] King Rat released!");
            }

            return true;
        }

        /// <summary>
        /// Called when a bomb explodes while being held.
        /// Resets the grab state and disperses all rats.
        /// </summary>
        private void OnBombExploded(Vector3 explosionPosition)
        {
            if (debugMode)
            {
                Debug.Log($"[KingRatHandler] Bomb exploded at {explosionPosition} while being held!");
            }

            // Reset grab state
            isGrabbing = false;
            OnKingRatReleased?.Invoke();

            // Switch back to the default following mode
            if (switchToCrowdOnCarry && ratInventory != null)
            {
                ratInventory.SetFollowingMode(defaultFollowingMode);

                if (debugMode)
                {
                    Debug.Log($"[KingRatHandler] Switched back to {defaultFollowingMode} following mode (bomb exploded)");
                }
            }

            // Clear references
            detectedKingRat = null;
            kingRatRigidbody = null;
            throwable = null;
            currentThrowableObject = null;

            // Disperse all rats from inventory
            if (ratInventory != null && ratInventory.RatCount > 0)
            {
                int dispersedCount = ratInventory.DisperseRats(ratInventory.RatCount);

                if (debugMode)
                {
                    Debug.Log($"[KingRatHandler] Dispersed {dispersedCount} rats due to bomb explosion!");
                }
            }

            if (debugMode)
            {
                Debug.Log("[KingRatHandler] Grab state reset. Can now pick up objects again.");
            }
        }

        /// <summary>
        /// Called when a rat is removed from the inventory.
        /// Checks if we fall below the minimum rats requirement and drops King Rat if needed.
        /// </summary>
        private void OnRatRemoved(Rat rat)
        {
            if (!isGrabbing || ratInventory == null)
            {
                return;
            }

            // Check if we're below the minimum rats requirement
            if (ratInventory.Count < minRatsToCarry)
            {
                if (debugMode)
                {
                    Debug.Log($"[KingRatHandler] Rat count ({ratInventory.Count}) below minimum ({minRatsToCarry}). Dropping King Rat!");
                }

                // Drop King Rat
                ReleaseKingRat();
            }
        }

        /// <summary>
        /// Checks if a GameObject is King Rat.
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

            // If both checks pass, it's considered King Rat
            return true;
        }

        /// <summary>
        /// Spawns a throwable from the detected producer and grabs it.
        /// </summary>
        /// <returns>True if the throwable was successfully spawned and grabbed, false otherwise.</returns>
        private bool SpawnAndGrabFromProducer()
        {
            if (detectedProducer == null)
            {
                if (debugMode)
                {
                    Debug.LogWarning("[KingRatHandler] No producer detected!");
                }
                return false;
            }

            if (!detectedProducer.CanSpawn)
            {
                if (debugMode)
                {
                    Debug.LogWarning("[KingRatHandler] Producer cannot spawn throwable!");
                }
                return false;
            }

            // Check if we have enough rats to carry the throwable
            if (ratInventory != null && ratInventory.RatCount < minRatsToCarry)
            {
                if (debugMode)
                {
                    Debug.LogWarning($"[KingRatHandler] Not enough rats to carry throwable! Need {minRatsToCarry}, have {ratInventory.RatCount}.");
                }
                return false;
            }

            // Spawn the throwable from the producer
            GameObject spawnedThrowable = detectedProducer.SpawnThrowable();

            if (spawnedThrowable == null)
            {
                if (debugMode)
                {
                    Debug.LogWarning("[KingRatHandler] Failed to spawn throwable from producer!");
                }
                return false;
            }

            // Set the spawned throwable as the detected object
            detectedKingRat = spawnedThrowable;
            kingRatRigidbody = spawnedThrowable.GetComponent<Rigidbody>();
            throwable = spawnedThrowable.GetComponent<IThrowable>();

            if (throwable == null)
            {
                Debug.LogError("[KingRatHandler] Spawned throwable does not have IThrowable component!");
                Destroy(spawnedThrowable);
                return false;
            }

            // Subscribe to IThrowable events
            throwable.OnLanded += OnKingRatLanded;

            // Now grab the spawned throwable
            return GrabKingRat();
        }

        #endregion

        #region Throw Methods

        /// <summary>
        /// Updates the target position based on pointer input.
        /// </summary>
        private void UpdateTargetPosition()
        {
            if (mainCamera == null)
            {
                return;
            }

            // Raycast from camera using the configured layer mask
            Ray ray = mainCamera.ScreenPointToRay(pointerPosition);

            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, targetLayerMask, QueryTriggerInteraction.Ignore))
            {
                // Store the hit collider for reticle layer detection
                targetHitCollider = hit.collider;
                Vector3 rawTarget = hit.point;
                Vector3 launchPos = GetLaunchPosition();

                // Calculate horizontal distance to target
                float targetDistance = Vector3.Distance(new Vector3(launchPos.x, 0, launchPos.z), new Vector3(rawTarget.x, 0, rawTarget.z));
                float maxDistance = CalculateMaxThrowDistance();

                // Check if target is within valid range
                isTargetValid = targetDistance <= maxDistance;

                if (lockTargetToMaxDistance && !isTargetValid)
                {
                    // Clamp target to max distance
                    Vector3 direction = (rawTarget - launchPos).normalized;
                    clampedTargetPosition = launchPos + direction * maxDistance;
                    clampedTargetPosition.y = rawTarget.y;
                    targetPosition = clampedTargetPosition;
                }
                else
                {
                    targetPosition = rawTarget;
                    clampedTargetPosition = rawTarget;
                }
            }
            else
            {
                targetPosition = null;
                isTargetValid = false;
                targetHitCollider = null;
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

            // Only show arc if we have a target and King Rat is being grabbed
            if (targetPosition.HasValue && isGrabbing && !(throwable != null && throwable.IsThrowing))
            {
                Vector3 start = GetLaunchPosition();
                Vector3 end = targetPosition.Value;

                // Store original target for collision detection
                originalTargetPosition = end;
                // Store original target validity before collision adjustment
                originalTargetValid = isTargetValid;

                // Calculate arc points
                CalculateArcPoints(start, end, arcPoints);

                // Check for arc collision if enabled
                isArcBlocked = false;
                arcHitPoint = null;
                arcHitSegment = -1;

                if (checkArcCollision)
                {
                    CheckArcCollision(start, end);
                }

                // If arc is blocked, truncate the arc visualization and update target position
                int segmentsToDraw = arcPoints.Length;
                if (isArcBlocked && arcHitPoint.HasValue)
                {
                    segmentsToDraw = arcHitSegment + 2; // Include the hit point segment
                    if (segmentsToDraw > arcPoints.Length)
                    {
                        segmentsToDraw = arcPoints.Length;
                    }

                    // Update the target position to the hit point
                    targetPosition = arcHitPoint.Value;
                    // Note: isTargetValid is NOT updated here - we preserve originalTargetValid for color priority
                }

                // Update LineRenderer position count and positions
                arcLineRenderer.positionCount = segmentsToDraw;
                for (int i = 0; i < segmentsToDraw; i++)
                {
                    if (i < arcPoints.Length)
                    {
                        arcLineRenderer.SetPosition(i, arcPoints[i]);
                    }
                }

                // Set arc color based on target validity and collision
                // Priority: invalid distance > blocked arc > valid
                // Use originalTargetValid to preserve original target validity before collision adjustment
                if (!originalTargetValid)
                {
                    arcLineRenderer.startColor = invalidDistanceColor;
                    arcLineRenderer.endColor = invalidDistanceColor;
                }
                else if (isArcBlocked)
                {
                    arcLineRenderer.startColor = blockedArcColor;
                    arcLineRenderer.endColor = blockedArcColor;
                }
                else
                {
                    arcLineRenderer.startColor = arcColor;
                    arcLineRenderer.endColor = arcColor;
                }

                arcLineRenderer.enabled = true;
            }
            else
            {
                arcLineRenderer.enabled = false;
                isArcBlocked = false;
                arcHitPoint = null;
            }
        }

        /// <summary>
        /// Checks for collisions along the throw arc.
        /// </summary>
        private void CheckArcCollision(Vector3 start, Vector3 end)
        {
            // Check each segment of the arc for collisions
            for (int i = 0; i < arcPoints.Length - 1; i++)
            {
                Vector3 segmentStart = arcPoints[i];
                Vector3 segmentEnd = arcPoints[i + 1];
                float segmentLength = Vector3.Distance(segmentStart, segmentEnd);

                // Raycast along this segment
                Vector3 direction = (segmentEnd - segmentStart).normalized;
                if (Physics.Raycast(segmentStart, direction, out RaycastHit hit, segmentLength, arcCollisionLayerMask, QueryTriggerInteraction.Ignore))
                {
                    // Found a collision!
                    isArcBlocked = true;
                    arcHitPoint = hit.point;
                    arcHitSegment = i;

                    if (debugMode)
                    {
                        Debug.Log($"[KingRatHandler] Arc collision detected at {hit.point} on segment {i}");
                    }

                    return; // Stop at first collision
                }
            }
        }

        /// <summary>
        /// Updates the target reticle position to follow the target destination.
        /// Selects the appropriate reticle based on the layer of the collider under the target.
        /// </summary>
        private void UpdateTargetReticle()
        {
            // Determine which reticle to show
            GameObject desiredReticleInstance = null;

            // Show reticle when we have a target and King Rat is being grabbed
            if (targetPosition.HasValue && isGrabbing && !(throwable != null && throwable.IsThrowing))
            {
                // Check if the target hit collider matches any reticle mapping
                if (targetHitCollider != null)
                {
                    int hitLayer = targetHitCollider.gameObject.layer;
                    foreach (var mapping in targetReticleMappings)
                    {
                        if (mapping.reticlePrefab != null && ((mapping.layerMask.value & (1 << hitLayer)) != 0))
                        {
                            if (reticleInstances.TryGetValue(mapping.reticlePrefab, out GameObject instance))
                            {
                                desiredReticleInstance = instance;
                                break;
                            }
                        }
                    }
                }

                // Fall back to default reticle if no mapping matched
                if (desiredReticleInstance == null)
                {
                    desiredReticleInstance = defaultReticleInstance;
                }

                // Activate the desired reticle and deactivate all others
                foreach (var kvp in reticleInstances)
                {
                    if (kvp.Value != null)
                    {
                        kvp.Value.SetActive(kvp.Value == desiredReticleInstance);
                    }
                }

                activeReticleInstance = desiredReticleInstance;

                if (activeReticleInstance != null)
                {
                    // Position the reticle at the target destination with a slight offset to prevent clipping
                    activeReticleInstance.transform.position = targetPosition.Value + Vector3.up * 0.01f;
                    // Make the reticle face upward (billboard style)
                    activeReticleInstance.transform.rotation = Quaternion.LookRotation(Vector3.up, Vector3.forward);
                }
            }
            else
            {
                // Hide all reticles
                foreach (var kvp in reticleInstances)
                {
                    if (kvp.Value != null)
                    {
                        kvp.Value.SetActive(false);
                    }
                }
                activeReticleInstance = null;
            }
        }

        /// <summary>
        /// Gets the launch position for the King Rat.
        /// </summary>
        private Vector3 GetLaunchPosition()
        {
            return transform.position + Vector3.up * launchHeightOffset;
        }

        /// <summary>
        /// Gets the current launch speed based on the number of rats in the inventory.
        /// </summary>
        private float GetLaunchSpeed()
        {
            int ratCount = (ratInventory != null) ? ratInventory.Count : 0;
            float speed = baseLaunchSpeed + (ratCount * speedPerRat);
            return Mathf.Min(speed, maxLaunchSpeed);
        }

        /// <summary>
        /// Calculates the maximum throw distance based on the number of rats in the inventory.
        /// </summary>
        private float CalculateMaxThrowDistance()
        {
            int ratCount = (ratInventory != null) ? ratInventory.Count : 0;
            float distance = baseThrowDistance + (ratCount * distancePerRat);
            return Mathf.Min(distance, maxThrowDistance);
        }

        /// <summary>
        /// Calculates arc points for visualization.
        /// Uses a quadratic Bezier curve to ensure the arc always completes from start to end.
        /// </summary>
        private void CalculateArcPoints(Vector3 start, Vector3 end, Vector3[] points)
        {
            // Calculate horizontal distance
            float distance = Vector3.Distance(new Vector3(start.x, 0, start.z), new Vector3(end.x, 0, end.z));

            // Calculate control point for the arc (midpoint horizontally, elevated vertically)
            Vector3 midPoint = Vector3.Lerp(start, end, 0.5f);
            float arcHeight = Mathf.Max(distance * 0.3f, GetLaunchSpeed() * 0.15f);
            Vector3 controlPoint = new Vector3(midPoint.x, Mathf.Max(start.y, end.y) + arcHeight, midPoint.z);

            // Generate arc points using quadratic Bezier curve
            for (int i = 0; i < points.Length; i++)
            {
                float t = (float)i / (points.Length - 1);

                // Quadratic Bezier formula: (1-t)² * P0 + 2(1-t)t * P1 + t² * P2
                float oneMinusT = 1f - t;
                Vector3 point = (oneMinusT * oneMinusT * start) +
                                (2f * oneMinusT * t * controlPoint) +
                                (t * t * end);

                points[i] = point;
            }
        }

        /// <summary>
        /// Attempts to throw the King Rat to the target position.
        /// </summary>
        private void TryThrowKingRat()
        {
            if (!isGrabbing)
            {
                if (debugMode)
                {
                    Debug.LogWarning("[KingRatHandler] King Rat is not being grabbed!");
                }
                return;
            }

            if (!targetPosition.HasValue)
            {
                if (debugMode)
                {
                    Debug.LogWarning("[KingRatHandler] No target position set!");
                }
                return;
            }

            // Check if target is within valid range (unless locked to max distance)
            if (!lockTargetToMaxDistance && !isTargetValid)
            {
                if (debugMode)
                {
                    float maxDistance = CalculateMaxThrowDistance();
                    Debug.LogWarning($"[KingRatHandler] Target is beyond maximum throw distance of {maxDistance:F2}!");
                }
                return;
            }

            // Start throw animation
            StartThrowAnimation(targetPosition.Value);
        }

        /// <summary>
        /// Starts the throw animation for the King Rat.
        /// </summary>
        private void StartThrowAnimation(Vector3 destination)
        {
            // Set isGrabbing to false
            isGrabbing = false;
            OnKingRatReleased?.Invoke();

            // Switch back to the default following mode when throwing King Rat
            if (switchToCrowdOnCarry && ratInventory != null)
            {
                ratInventory.SetFollowingMode(defaultFollowingMode);

                if (debugMode)
                {
                    Debug.Log($"[KingRatHandler] Switched back to {defaultFollowingMode} following mode (throw)");
                }
            }

            // Unsubscribe from bomb explosion event if it was a bomb
            if (currentThrowableObject != null)
            {
                Bomb bomb = currentThrowableObject.GetComponent<Bomb>();
                if (bomb != null)
                {
                    bomb.OnExploded -= OnBombExploded;

                    if (debugMode)
                    {
                        Debug.Log("[KingRatHandler] Unsubscribed from bomb explosion event (throwing)!");
                    }
                }
                currentThrowableObject = null;
            }

            // Move King Rat to launch position
            if (detectedKingRat != null)
            {
                detectedKingRat.transform.position = GetLaunchPosition();
                kingRatOriginalPosition = GetLaunchPosition();
                kingRatOriginalRotation = detectedKingRat.transform.rotation;
            }

            // Use IThrowable to handle the throw
            if (throwable != null)
            {
                // Set throw duration on IThrowable
                throwable.SetThrowDuration(throwDuration);
                throwable.ThrowTo(destination, GetLaunchSpeed());
            }
            else
            {
                Debug.LogError("[KingRatHandler] IThrowable component not found on King Rat!");
            }

            // Fire event
            OnKingRatThrown?.Invoke(destination);

            if (debugMode)
            {
                int ratCount = (ratInventory != null) ? ratInventory.Count : 0;
                float launchSpeed = GetLaunchSpeed();
                Debug.Log($"[KingRatHandler] Throwing King Rat to {destination} (Rats: {ratCount}, Speed: {launchSpeed:F2})");
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Gets the current target position.
        /// </summary>
        public Vector3? GetTargetPosition()
        {
            return targetPosition;
        }

        /// <summary>
        /// Gets the current launch speed based on rat count.
        /// </summary>
        public float GetCurrentLaunchSpeed()
        {
            return GetLaunchSpeed();
        }

        /// <summary>
        /// Gets the current maximum throw distance based on rat count.
        /// </summary>
        public float GetMaxThrowDistance()
        {
            return CalculateMaxThrowDistance();
        }

        /// <summary>
        /// Gets whether the current target is within valid throw distance.
        /// </summary>
        public bool IsTargetValid()
        {
            return isTargetValid;
        }

        // Throw Settings Getters
        /// <summary>
        /// Gets the base throw distance.
        /// </summary>
        public float BaseThrowDistance => baseThrowDistance;

        /// <summary>
        /// Gets the distance added per rat.
        /// </summary>
        public float DistancePerRat => distancePerRat;

        /// <summary>
        /// Gets the maximum throw distance.
        /// </summary>
        public float MaxThrowDistance => maxThrowDistance;

        /// <summary>
        /// Gets the base launch speed.
        /// </summary>
        public float BaseLaunchSpeed => baseLaunchSpeed;

        /// <summary>
        /// Gets the speed added per rat.
        /// </summary>
        public float SpeedPerRat => speedPerRat;

        /// <summary>
        /// Gets the maximum launch speed.
        /// </summary>
        public float MaxLaunchSpeed => maxLaunchSpeed;

        /// <summary>
        /// Gets the launch height offset.
        /// </summary>
        public float LaunchHeightOffset => launchHeightOffset;

        // Grab Settings
        public void SetMinRatsToCarry(int minRats) => minRatsToCarry = Mathf.Max(1, minRats);
        public void SetCarryOffset(Vector3 offset) => carryOffset = offset;
        public void SetCarrySmoothSpeed(float speed) => carrySmoothSpeed = Mathf.Max(0.1f, speed);
        public void SetDetectionBoxSize(Vector3 size) => detectionBoxSize = size;
        public void SetDetectionBoxOffset(Vector3 offset) => detectionBoxOffset = offset;

        // Throw Settings
        public void SetBaseLaunchSpeed(float speed) => baseLaunchSpeed = Mathf.Max(1f, speed);
        public void SetSpeedPerRat(float speed) => speedPerRat = Mathf.Max(0f, speed);
        public void SetMaxLaunchSpeed(float speed) => maxLaunchSpeed = Mathf.Max(1f, speed);
        public void SetLaunchAngle(float angle) => launchAngle = Mathf.Clamp(angle, 10f, 80f);
        public void SetThrowDuration(float duration) => throwDuration = Mathf.Max(0.1f, duration);
        public void SetBaseThrowDistance(float distance) => baseThrowDistance = Mathf.Max(1f, distance);
        public void SetDistancePerRat(float distance) => distancePerRat = Mathf.Max(0f, distance);
        public void SetMaxThrowDistance(float distance) => maxThrowDistance = Mathf.Max(1f, distance);
        public void SetLockTargetToMaxDistance(bool lockTarget) => lockTargetToMaxDistance = lockTarget;

        #endregion
    }
}
