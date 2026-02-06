using UnityEngine;

namespace FindersCheesers
{
    /// <summary>
    /// Component that adds dispersing behavior to an EnemyAI.
    /// Triggers rat dispersal when the target's RatInventory is in attack range.
    /// </summary>
    [AddComponentMenu("Finders Cheesers/EnemyAI/DispersingAI")]
    [RequireComponent(typeof(EnemyAI))]
    public class DispersingAI : MonoBehaviour
    {
        #region Settings

        [Header("Disperse Settings")]
        [Tooltip("Number of rats to disperse when target is in attack range")]
        [SerializeField]
        private int ratsToDisperse = 1;

        [Tooltip("Whether to auto-disperse when target is in attack range")]
        [SerializeField]
        private bool autoDisperse = true;

        [Tooltip("Whether to stop dispersing when target leaves attack range")]
        [SerializeField]
        private bool stopOnTargetOutOfRange = true;

        [Tooltip("Cooldown time between dispersals (in seconds)")]
        [SerializeField]
        private float disperseCooldown = 2f;

        [Tooltip("Whether to disperse all rats when triggered")]
        [SerializeField]
        private bool disperseAllRats = false;

        [Header("Debug")]
        [Tooltip("Show debug information in the console")]
        [SerializeField]
        private bool debugMode = false;

        [Tooltip("Show disperse gizmos in the scene")]
        [SerializeField]
        private bool showGizmos = true;

        #endregion

        #region Events

        /// <summary>
        /// Event fired when dispersing starts.
        /// </summary>
        public event System.Action OnDispersingStarted;

        /// <summary>
        /// Event fired when dispersing stops.
        /// </summary>
        public event System.Action OnDispersingStopped;

        /// <summary>
        /// Event fired when rats are dispersed.
        /// </summary>
        public event System.Action<int> OnRatsDispersed;

        #endregion

        #region Properties

        /// <summary>
        /// Gets whether the AI is currently dispersing.
        /// </summary>
        public bool IsDispersing { get; private set; }

        /// <summary>
        /// Gets whether the AI is on cooldown.
        /// </summary>
        public bool IsOnCooldown { get; private set; }

        /// <summary>
        /// Gets the remaining cooldown time.
        /// </summary>
        public float RemainingCooldown { get; private set; }

        /// <summary>
        /// Gets or sets the number of rats to disperse.
        /// </summary>
        public int RatsToDisperse
        {
            get => ratsToDisperse;
            set => ratsToDisperse = Mathf.Max(0, value);
        }

        /// <summary>
        /// Gets or sets the disperse cooldown.
        /// </summary>
        public float DisperseCooldown
        {
            get => disperseCooldown;
            set => disperseCooldown = Mathf.Max(0f, value);
        }

        #endregion

        #region Component References

        private EnemyAI enemyAI;
        private RatInventory targetRatInventory;

        #endregion

        #region State Variables

        private float cooldownTimer;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            enemyAI = GetComponent<EnemyAI>();
            
            if (enemyAI == null)
            {
                Debug.LogError("[DispersingAI] EnemyAI component not found!");
                return;
            }

            // Find RatInventory on the target GameObject (the player)
            if (enemyAI.Target != null)
            {
                targetRatInventory = enemyAI.Target.GetComponent<RatInventory>();
            }

            if (targetRatInventory == null)
            {
                Debug.LogWarning("[DispersingAI] No RatInventory found on target! Dispersing will not work.");
            }

            // Subscribe to EnemyAI events
            enemyAI.OnTargetInAttackRange += HandleTargetInAttackRange;
            enemyAI.OnTargetOutOfAttackRange += HandleTargetOutOfRange;
        }

        private void Update()
        {
            if (!enemyAI.IsActive)
            {
                return;
            }

            UpdateCooldown();
        }

        private void OnDestroy()
        {
            if (enemyAI != null)
            {
                enemyAI.OnTargetInAttackRange -= HandleTargetInAttackRange;
                enemyAI.OnTargetOutOfAttackRange -= HandleTargetOutOfRange;
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Attempts to disperse rats.
        /// </summary>
        /// <returns>True if dispersal was initiated, false otherwise.</returns>
        public bool TryDisperse()
        {
            if (IsOnCooldown)
            {
                if (debugMode)
                {
                    Debug.Log("[DispersingAI] Cannot disperse - on cooldown");
                }
                return false;
            }

            if (targetRatInventory == null)
            {
                Debug.LogWarning("[DispersingAI] Cannot disperse - no RatInventory component!");
                return false;
            }

            if (targetRatInventory.Count == 0)
            {
                if (debugMode)
                {
                    Debug.Log("[DispersingAI] Cannot disperse - no rats in inventory");
                }
                return false;
            }

            PerformDisperse();
            return true;
        }

        /// <summary>
        /// Starts continuous dispersing mode.
        /// </summary>
        public void StartDispersing()
        {
            if (IsDispersing)
            {
                return;
            }

            IsDispersing = true;

            OnDispersingStarted?.Invoke();

            if (debugMode)
            {
                Debug.Log("[DispersingAI] Started dispersing");
            }
        }

        /// <summary>
        /// Stops continuous dispersing mode.
        /// </summary>
        public void StopDispersing()
        {
            if (!IsDispersing)
            {
                return;
            }

            IsDispersing = false;

            OnDispersingStopped?.Invoke();

            if (debugMode)
            {
                Debug.Log("[DispersingAI] Stopped dispersing");
            }
        }

        /// <summary>
        /// Resets the disperse cooldown.
        /// </summary>
        public void ResetCooldown()
        {
            cooldownTimer = 0f;
            IsOnCooldown = false;
            RemainingCooldown = 0f;

            if (debugMode)
            {
                Debug.Log("[DispersingAI] Disperse cooldown reset");
            }
        }

        /// <summary>
        /// Sets the number of rats to disperse.
        /// </summary>
        /// <param name="amount">The new number of rats to disperse.</param>
        public void SetRatsToDisperse(int amount)
        {
            RatsToDisperse = amount;
        }

        /// <summary>
        /// Sets the disperse cooldown.
        /// </summary>
        /// <param name="cooldown">The new cooldown in seconds.</param>
        public void SetDisperseCooldown(float cooldown)
        {
            DisperseCooldown = cooldown;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Updates the disperse cooldown timer.
        /// </summary>
        private void UpdateCooldown()
        {
            if (IsOnCooldown)
            {
                cooldownTimer -= Time.deltaTime;
                RemainingCooldown = Mathf.Max(0f, cooldownTimer);

                if (cooldownTimer <= 0f)
                {
                    IsOnCooldown = false;
                    cooldownTimer = 0f;
                }
            }
        }

        /// <summary>
        /// Performs the dispersal.
        /// </summary>
        private void PerformDisperse()
        {
            if (targetRatInventory == null)
            {
                return;
            }

            // Start cooldown
            IsOnCooldown = true;
            cooldownTimer = disperseCooldown;
            RemainingCooldown = disperseCooldown;

            // Determine number of rats to disperse
            int amount = disperseAllRats ? targetRatInventory.Count : ratsToDisperse;
            amount = Mathf.Min(amount, targetRatInventory.Count);

            // Disperse rats
            int dispersedCount = targetRatInventory.DisperseRats(amount);

            OnRatsDispersed?.Invoke(dispersedCount);

            if (debugMode)
            {
                Debug.Log($"[DispersingAI] Dispersed {dispersedCount} rats");
            }
        }

        /// <summary>
        /// Handles target in attack range event from EnemyAI.
        /// </summary>
        private void HandleTargetInAttackRange(Transform target)
        {
            // Try to find RatInventory on the target GameObject
            if (targetRatInventory == null && target != null)
            {
                targetRatInventory = target.GetComponent<RatInventory>();
            }

            if (autoDisperse)
            {
                StartDispersing();
                TryDisperse();

                if (debugMode)
                {
                    Debug.Log($"[DispersingAI] Target in attack range - dispersing rats");
                }
            }
        }

        /// <summary>
        /// Handles target out of attack range event from EnemyAI.
        /// </summary>
        private void HandleTargetOutOfRange()
        {
            if (stopOnTargetOutOfRange)
            {
                StopDispersing();

                if (debugMode)
                {
                    Debug.Log("[DispersingAI] Target out of attack range - stopped dispersing");
                }
            }
        }

        #endregion

        #region Editor

        private void OnValidate()
        {
            ratsToDisperse = Mathf.Max(0, ratsToDisperse);
            disperseCooldown = Mathf.Max(0f, disperseCooldown);
        }

        private void OnDrawGizmos()
        {
            if (!showGizmos)
            {
                return;
            }

            // Draw attack range indicator
            if (IsDispersing)
            {
                Gizmos.color = Color.orange;
                Gizmos.DrawWireSphere(transform.position, enemyAI.AttackRange);
            }

            // Draw indicator to target's RatInventory
            if (targetRatInventory != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, targetRatInventory.transform.position);
            }
        }

        #endregion
    }
}
