using UnityEngine;

namespace FindersCheesers
{
    /// <summary>
    /// Component that adds attacking behavior to an EnemyAI.
    /// Automatically attacks targets that enter attack range.
    /// </summary>
    [AddComponentMenu("Finders Cheesers/EnemyAI/AttackingAI")]
    [RequireComponent(typeof(EnemyAI))]
    public class AttackingAI : MonoBehaviour
    {
        #region Settings

        [Header("Attack Settings")]
        [Tooltip("Damage dealt per attack")]
        [SerializeField]
        private float attackDamage = 10f;

        [Tooltip("Time between attacks (in seconds)")]
        [SerializeField]
        private float attackCooldown = 1f;

        [Tooltip("Whether to auto-attack when target is in range")]
        [SerializeField]
        private bool autoAttack = true;

        [Tooltip("Whether to stop attacking when target leaves range")]
        [SerializeField]
        private bool stopOnTargetOutOfRange = true;

        [Tooltip("Whether to attack only when facing the target")]
        [SerializeField]
        private bool requireFacingTarget = true;

        [Tooltip("Angle tolerance for facing the target (in degrees)")]
        [SerializeField]
        private float facingAngleTolerance = 45f;

        [Header("Attack Animation")]
        [Tooltip("Optional animator trigger name for attack animation")]
        [SerializeField]
        private string attackTriggerName = "Attack";

        [Tooltip("Optional animator bool name for attacking state")]
        [SerializeField]
        private string attackBoolName = "IsAttacking";

        [Header("Debug")]
        [Tooltip("Show debug information in the console")]
        [SerializeField]
        private bool debugMode = false;

        [Tooltip("Show attack range gizmos in the scene")]
        [SerializeField]
        private bool showGizmos = true;

        #endregion

        #region Events

        /// <summary>
        /// Event fired when an attack starts.
        /// </summary>
        public event System.Action<Transform> OnAttackStarted;

        /// <summary>
        /// Event fired when an attack hits a target.
        /// </summary>
        public event System.Action<Transform, float> OnAttackHit;

        /// <summary>
        /// Event fired when an attack is completed.
        /// </summary>
        public event System.Action OnAttackCompleted;

        /// <summary>
        /// Event fired when attacking starts (continuous state).
        /// </summary>
        public event System.Action OnAttackingStarted;

        /// <summary>
        /// Event fired when attacking stops (continuous state).
        /// </summary>
        public event System.Action OnAttackingStopped;

        #endregion

        #region Properties

        /// <summary>
        /// Gets whether the AI is currently attacking.
        /// </summary>
        public bool IsAttacking { get; private set; }

        /// <summary>
        /// Gets whether the AI can attack (not on cooldown).
        /// </summary>
        public bool CanAttack => !IsOnCooldown;

        /// <summary>
        /// Gets whether the AI is currently on attack cooldown.
        /// </summary>
        public bool IsOnCooldown { get; private set; }

        /// <summary>
        /// Gets the remaining cooldown time.
        /// </summary>
        public float RemainingCooldown { get; private set; }

        /// <summary>
        /// Gets the current target being attacked.
        /// </summary>
        public Transform CurrentTarget { get; private set; }

        /// <summary>
        /// Gets or sets the attack damage.
        /// </summary>
        public float AttackDamage
        {
            get => attackDamage;
            set => attackDamage = Mathf.Max(0f, value);
        }

        /// <summary>
        /// Gets or sets the attack cooldown.
        /// </summary>
        public float AttackCooldown
        {
            get => attackCooldown;
            set => attackCooldown = Mathf.Max(0f, value);
        }

        #endregion

        #region Component References

        private EnemyAI enemyAI;
        private Animator animator;

        #endregion

        #region State Variables

        private float cooldownTimer;
        private bool wasAttacking;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            enemyAI = GetComponent<EnemyAI>();
            animator = GetComponent<Animator>();
            
            if (enemyAI == null)
            {
                Debug.LogError("[AttackingAI] EnemyAI component not found!");
                return;
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
            UpdateAttackingBehavior();
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
        /// Attempts to attack the current target.
        /// </summary>
        /// <returns>True if the attack was initiated, false otherwise.</returns>
        public bool TryAttack()
        {
            if (!CanAttack)
            {
                if (debugMode)
                {
                    Debug.Log("[AttackingAI] Cannot attack - on cooldown");
                }
                return false;
            }

            if (enemyAI.Target == null)
            {
                if (debugMode)
                {
                    Debug.Log("[AttackingAI] Cannot attack - no target");
                }
                return false;
            }

            if (!enemyAI.IsTargetInAttackRange)
            {
                if (debugMode)
                {
                    Debug.Log("[AttackingAI] Cannot attack - target out of range");
                }
                return false;
            }

            if (requireFacingTarget && !IsFacingTarget())
            {
                if (debugMode)
                {
                    Debug.Log("[AttackingAI] Cannot attack - not facing target");
                }
                return false;
            }

            PerformAttack();
            return true;
        }

        /// <summary>
        /// Performs an attack on the specified target.
        /// </summary>
        /// <param name="target">The target to attack.</param>
        /// <returns>True if the attack was initiated, false otherwise.</returns>
        public bool AttackTarget(Transform target)
        {
            if (target == null)
            {
                Debug.LogWarning("[AttackingAI] Cannot attack null target!");
                return false;
            }

            enemyAI.SetTarget(target, true);
            return TryAttack();
        }

        /// <summary>
        /// Starts continuous attacking mode.
        /// </summary>
        public void StartAttacking()
        {
            if (IsAttacking)
            {
                return;
            }

            IsAttacking = true;
            CurrentTarget = enemyAI.Target;

            OnAttackingStarted?.Invoke();

            if (debugMode)
            {
                Debug.Log("[AttackingAI] Started attacking");
            }
        }

        /// <summary>
        /// Stops continuous attacking mode.
        /// </summary>
        public void StopAttacking()
        {
            if (!IsAttacking)
            {
                return;
            }

            IsAttacking = false;
            CurrentTarget = null;

            OnAttackingStopped?.Invoke();

            if (debugMode)
            {
                Debug.Log("[AttackingAI] Stopped attacking");
            }
        }

        /// <summary>
        /// Resets the attack cooldown.
        /// </summary>
        public void ResetCooldown()
        {
            cooldownTimer = 0f;
            IsOnCooldown = false;
            RemainingCooldown = 0f;

            if (debugMode)
            {
                Debug.Log("[AttackingAI] Attack cooldown reset");
            }
        }

        /// <summary>
        /// Sets the attack damage.
        /// </summary>
        /// <param name="damage">The new damage value.</param>
        public void SetAttackDamage(float damage)
        {
            AttackDamage = damage;
        }

        /// <summary>
        /// Sets the attack cooldown.
        /// </summary>
        /// <param name="cooldown">The new cooldown in seconds.</param>
        public void SetAttackCooldown(float cooldown)
        {
            AttackCooldown = cooldown;
        }

        /// <summary>
        /// Checks if the enemy is facing the target within the tolerance angle.
        /// </summary>
        /// <returns>True if facing the target, false otherwise.</returns>
        public bool IsFacingTarget()
        {
            if (enemyAI.Target == null)
            {
                return false;
            }

            Vector3 directionToTarget = (enemyAI.Target.position - transform.position).normalized;
            directionToTarget.y = 0f;

            float angle = Vector3.Angle(transform.forward, directionToTarget);
            return angle <= facingAngleTolerance;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Updates the attack cooldown timer.
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
        /// Updates the attacking behavior.
        /// </summary>
        private void UpdateAttackingBehavior()
        {
            if (!IsAttacking)
            {
                return;
            }

            // Check if we should stop attacking
            if (enemyAI.Target == null || (!enemyAI.IsTargetInAttackRange && stopOnTargetOutOfRange))
            {
                StopAttacking();
                return;
            }

            // Try to attack if not on cooldown
            if (!IsOnCooldown && CanAttack)
            {
                TryAttack();
            }
        }

        /// <summary>
        /// Performs the attack.
        /// </summary>
        private void PerformAttack()
        {
            CurrentTarget = enemyAI.Target;

            // Start cooldown
            IsOnCooldown = true;
            cooldownTimer = attackCooldown;
            RemainingCooldown = attackCooldown;

            // Trigger attack animation
            TriggerAttackAnimation();

            // Deal damage to target
            DealDamageToTarget();

            OnAttackStarted?.Invoke(CurrentTarget);

            if (debugMode)
            {
                Debug.Log($"[AttackingAI] Attacking {CurrentTarget.name} for {attackDamage} damage");
            }

            // Complete attack (you may want to delay this based on animation)
            CompleteAttack();
        }

        /// <summary>
        /// Triggers the attack animation.
        /// </summary>
        private void TriggerAttackAnimation()
        {
            if (animator == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(attackTriggerName))
            {
                animator.SetTrigger(attackTriggerName);
            }

            if (!string.IsNullOrEmpty(attackBoolName))
            {
                animator.SetBool(attackBoolName, true);
            }
        }

        /// <summary>
        /// Deals damage to the target.
        /// </summary>
        private void DealDamageToTarget()
        {
            if (CurrentTarget == null)
            {
                return;
            }

            // Try to get Health component on target
            Health targetHealth = CurrentTarget.GetComponent<Health>();
            
            if (targetHealth != null)
            {
                targetHealth.TakeDamage(attackDamage);
                OnAttackHit?.Invoke(CurrentTarget, attackDamage);

                if (debugMode)
                {
                    Debug.Log($"[AttackingAI] Dealt {attackDamage} damage to {CurrentTarget.name}");
                }
            }
            else
            {
                if (debugMode)
                {
                    Debug.LogWarning($"[AttackingAI] Target {CurrentTarget.name} has no Health component");
                }
            }
        }

        /// <summary>
        /// Completes the attack.
        /// </summary>
        private void CompleteAttack()
        {
            // Reset attack animation bool
            if (animator != null && !string.IsNullOrEmpty(attackBoolName))
            {
                animator.SetBool(attackBoolName, false);
            }

            OnAttackCompleted?.Invoke();
        }

        /// <summary>
        /// Handles target in attack range event from EnemyAI.
        /// </summary>
        private void HandleTargetInAttackRange(Transform target)
        {
            if (autoAttack)
            {
                StartAttacking();
            }
        }

        /// <summary>
        /// Handles target out of attack range event from EnemyAI.
        /// </summary>
        private void HandleTargetOutOfRange()
        {
            if (stopOnTargetOutOfRange)
            {
                StopAttacking();

                if (debugMode)
                {
                    Debug.Log("[AttackingAI] Target out of attack range - stopped attacking");
                }
            }
        }

        #endregion

        #region Editor

        private void OnValidate()
        {
            attackDamage = Mathf.Max(0f, attackDamage);
            attackCooldown = Mathf.Max(0f, attackCooldown);
            facingAngleTolerance = Mathf.Clamp(facingAngleTolerance, 0f, 180f);
        }

        private void OnDrawGizmos()
        {
            if (!showGizmos)
            {
                return;
            }

            // Draw attack range
            Gizmos.color = IsAttacking ? Color.red : Color.green;
            
            // Get EnemyAI component for attack range
            EnemyAI ai = GetComponent<EnemyAI>();
            if (ai != null)
            {
                Gizmos.DrawWireSphere(transform.position, ai.AttackRange);
            }

            // Draw facing cone
            if (requireFacingTarget)
            {
                Gizmos.color = Color.yellow;
                Vector3 leftDirection = Quaternion.Euler(0f, -facingAngleTolerance, 0f) * transform.forward;
                Vector3 rightDirection = Quaternion.Euler(0f, facingAngleTolerance, 0f) * transform.forward;
                
                Gizmos.DrawRay(transform.position, leftDirection * 2f);
                Gizmos.DrawRay(transform.position, rightDirection * 2f);
            }

            // Draw attack line to target
            if (IsAttacking && CurrentTarget != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, CurrentTarget.position);
            }
        }

        #endregion
    }
}
