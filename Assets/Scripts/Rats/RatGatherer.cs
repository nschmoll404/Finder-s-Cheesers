using UnityEngine;
using UnityEngine.InputSystem;

namespace FindersCheesers
{
    /// <summary>
    /// A component that handles gathering and dispersing rats for a RatInventory.
    /// Can be used as a penalty event to disperse rats.
    ///
    /// Input Options:
    /// - Use PlayerInputSingleton for centralized input access
    /// - Use local PlayerInput component for direct access
    /// - Leave input actions unassigned for programmatic-only control
    /// </summary>
    [AddComponentMenu("Finders Cheesers/Rat Gatherer")]
    public class RatGatherer : MonoBehaviour
    {
        [Header("Input Settings")]
        [Tooltip("If enabled, uses PlayerInputSingleton instead of local PlayerInput component")]
        [SerializeField]
        private bool usePlayerInputSingleton = false;

        [Tooltip("Reference to Gather input action (optional - if not set, input gathering will be disabled)")]
        [SerializeField]
        private InputActionReference gatherActionReference;

        [Tooltip("Reference to Disperse input action (optional - if not set, input dispersing will be disabled)")]
        [SerializeField]
        private InputActionReference disperseActionReference;

        [Header("Auto Gather Settings")]
        [Tooltip("If enabled, automatically gathers rats that enter the detection radius")]
        [SerializeField]
        private bool autoGather = true;

        [Tooltip("Radius for auto-gathering rats (uses RatInventory's gather radius if not set)")]
        [SerializeField]
        private float autoGatherRadius = 5f;

        [Tooltip("Layer mask for finding rats in auto-gather")]
        [SerializeField]
        private LayerMask autoGatherLayerMask = 1;

        [Tooltip("Tag that identifies rat GameObjects for auto-gather")]
        [SerializeField]
        private string autoGatherRatTag = "Rat";

        [Header("Disperse Settings")]
        [Tooltip("Number of rats to disperse when penalty event occurs")]
        [SerializeField]
        private int penaltyDisperseAmount = 3;

        [Header("Rat Inventory Reference")]
        [Tooltip("Reference to the RatInventory component")]
        [SerializeField]
        private RatInventory ratInventory;

        [Header("Debug")]
        [Tooltip("Show debug information in the console")]
        [SerializeField]
        private bool debugMode = false;

        [Tooltip("Visualize gather radius in Scene view")]
        [SerializeField]
        private bool visualizeGatherRadius = true;

        [Tooltip("Visualize auto-gather radius in Scene view")]
        [SerializeField]
        private bool visualizeAutoGatherRadius = true;

        // Component references
        private PlayerInput playerInput;
        private InputAction gatherAction;
        private InputAction disperseAction;

        /// <summary>
        /// Event fired when rats are gathered.
        /// </summary>
        public event System.Action<int> OnRatsGathered;

        /// <summary>
        /// Event fired when rats are dispersed.
        /// </summary>
        public event System.Action<int> OnRatsDispersed;

        /// <summary>
        /// Gets the number of rats to disperse on penalty.
        /// </summary>
        public int PenaltyDisperseAmount => penaltyDisperseAmount;

        private void Start()
        {
            // Get RatInventory component
            if (ratInventory == null)
            {
                ratInventory = GetComponent<RatInventory>();
            }

            // Get PlayerInput (optional - either local or from singleton)
            if (usePlayerInputSingleton)
            {
                if (PlayerInputSingleton.Instance != null)
                {
                    playerInput = PlayerInputSingleton.Instance.PlayerInput;
                }
                else
                {
                    Debug.LogWarning("[RatGatherer] PlayerInputSingleton is not available in the scene. Input gathering/dispersing will be disabled.");
                }
            }
            else
            {
                playerInput = GetComponent<PlayerInput>();
            }

            // Initialize input actions
            InitializeInputActions();
        }

        private void InitializeInputActions()
        {
            // Get gather action using the ID from InputActionReference (optional)
            if (playerInput != null && gatherActionReference != null)
            {
                gatherAction = playerInput.actions.FindAction(gatherActionReference.action.id);

                if (gatherAction == null)
                {
                    string inputSource = usePlayerInputSingleton ? "PlayerInputSingleton" : "local PlayerInput";
                    Debug.LogWarning($"[RatGatherer] Gather action not found in {inputSource} actions. Input gathering will be disabled.");
                }
            }

            // Get the disperse action using the ID from InputActionReference (optional)
            if (playerInput != null && disperseActionReference != null)
            {
                disperseAction = playerInput.actions.FindAction(disperseActionReference.action.id);

                if (disperseAction == null)
                {
                    string inputSource = usePlayerInputSingleton ? "PlayerInputSingleton" : "local PlayerInput";
                    Debug.LogWarning($"[RatGatherer] Disperse action not found in {inputSource} actions. Input dispersing will be disabled.");
                }
            }
        }

        private void FixedUpdate()
        {
            // Auto-gather rats if enabled
            if (!autoGather || ratInventory == null)
            {
                return;
            }

            // Find all colliders within auto-gather radius
            Collider[] colliders = Physics.OverlapSphere(
                transform.position,
                autoGatherRadius,
                autoGatherLayerMask,
                QueryTriggerInteraction.Ignore
            );

            // Process each collider to find rats
            foreach (Collider collider in colliders)
            {
                if (collider == null)
                {
                    continue;
                }

                // Check if collider has a Rat component
                Rat rat = collider.GetComponent<Rat>();
                if (rat == null)
                {
                    // Check parent object
                    rat = collider.GetComponentInParent<Rat>();
                }

                if (rat == null)
                {
                    continue;
                }

                // Skip if already in this inventory
                if (ratInventory.Contains(rat))
                {
                    continue;
                }

                // Skip if already moving to an inventory
                if (rat.IsMovingToInventory)
                {
                    continue;
                }

                // Skip if running away
                if (rat.IsRunningAway)
                {
                    continue;
                }

                // Skip if deposited to a RatInteractable
                if (rat.IsDeposited)
                {
                    continue;
                }

                // Check if rat can be gathered (drop cooldown check)
                if (!rat.CanBeGathered(transform.position))
                {
                    continue;
                }

                // Check if rat matches tag (if specified)
                if (!string.IsNullOrEmpty(autoGatherRatTag) && !collider.gameObject.CompareTag(autoGatherRatTag))
                {
                    continue;
                }

                // Make rat move toward this inventory
                rat.MoveToInventory(ratInventory);

                if (debugMode)
                {
                    Debug.Log($"[RatGatherer] Auto-gathering rat. Rat ID: {rat.RatId}");
                }
            }
        }

        private void Update()
        {
            // Check for gather input (optional - only if gatherAction is assigned)
            if (gatherAction != null && gatherAction.WasPressedThisFrame())
            {
                GatherRats();
            }

            // Check for disperse input (optional - only if disperseAction is assigned)
            if (disperseAction != null && disperseAction.WasPressedThisFrame())
            {
                DisperseRats(penaltyDisperseAmount);
            }
        }

        /// <summary>
        /// Gathers all rats within the inventory's gather radius.
        /// Can be called programmatically or via input (if input actions are assigned).
        /// </summary>
        public void GatherRats()
        {
            if (ratInventory == null)
            {
                Debug.LogWarning("[RatGatherer] RatInventory is not assigned!");
                return;
            }

            int gatheredCount = ratInventory.GatherRats();

            if (gatheredCount > 0)
            {
                OnRatsGathered?.Invoke(gatheredCount);

                if (debugMode)
                {
                    Debug.Log($"[RatGatherer] Gathered {gatheredCount} rats.");
                }
            }
        }

        /// <summary>
        /// Disperses the specified number of rats from the inventory.
        /// Can be called programmatically or via input (if input actions are assigned).
        /// </summary>
        /// <param name="amount">The number of rats to disperse.</param>
        public void DisperseRats(int amount)
        {
            if (ratInventory == null)
            {
                Debug.LogWarning("[RatGatherer] RatInventory is not assigned!");
                return;
            }

            int dispersedCount = ratInventory.DisperseRats(amount);

            if (dispersedCount > 0)
            {
                OnRatsDispersed?.Invoke(dispersedCount);

                if (debugMode)
                {
                    Debug.Log($"[RatGatherer] Dispersed {dispersedCount} rats.");
                }
            }
        }

        /// <summary>
        /// Sets the penalty disperse amount.
        /// </summary>
        /// <param name="amount">The new penalty disperse amount.</param>
        public void SetPenaltyDisperseAmount(int amount)
        {
            penaltyDisperseAmount = Mathf.Max(1, amount);

            if (debugMode)
            {
                Debug.Log($"[RatGatherer] Penalty disperse amount set to: {penaltyDisperseAmount}");
            }
        }

        private void OnDrawGizmos()
        {
            // Draw gather radius
            if (visualizeGatherRadius && ratInventory != null)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(ratInventory.transform.position, ratInventory.GatherRadius);
            }

            // Draw auto-gather radius
            if (visualizeAutoGatherRadius && autoGather)
            {
                Gizmos.color = new Color(0.5f, 1.0f, 0.5f, 0.3f); // Light green with transparency
                Gizmos.DrawWireSphere(transform.position, autoGatherRadius);
            }
        }

        private void Reset()
        {
            autoGatherRadius = 5f;
            penaltyDisperseAmount = 3;
        }
    }
}
