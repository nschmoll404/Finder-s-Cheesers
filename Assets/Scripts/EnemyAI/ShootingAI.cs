using UnityEngine;

namespace FindersCheesers
{
    /// <summary>
    /// Component that adds shooting behavior to an EnemyAI.
    /// Automatically aims and fires a RangedWeapon at detected targets.
    /// </summary>
    [AddComponentMenu("Finders Cheesers/EnemyAI/ShootingAI")]
    [RequireComponent(typeof(EnemyAI))]
    public class ShootingAI : MonoBehaviour
    {
        #region Settings

        [Header("Weapon Settings")]
        [Tooltip("The ranged weapon to use for shooting")]
        [SerializeField]
        private RangedWeapon rangedWeapon;

        [Tooltip("Whether to automatically find a RangedWeapon if not assigned")]
        [SerializeField]
        private bool autoFindWeapon = true;

        [Header("Shooting Settings")]
        [Tooltip("Whether to auto-shoot when target is in range")]
        [SerializeField]
        private bool autoShoot = true;

        [Tooltip("Whether to stop shooting when target leaves range")]
        [SerializeField]
        private bool stopOnTargetOutOfRange = true;

        [Tooltip("Whether to only shoot when facing the target")]
        [SerializeField]
        private bool requireFacingTarget = true;

        [Tooltip("Angle tolerance for facing the target (in degrees)")]
        [SerializeField]
        private float facingAngleTolerance = 30f;

        [Tooltip("Whether to lead the target (predict movement)")]
        [SerializeField]
        private bool leadTarget = true;

        [Tooltip("Lead prediction time (in seconds)")]
        [SerializeField]
        private float leadPredictionTime = 0.5f;

        [Header("Aiming Settings")]
        [Tooltip("Aim rotation speed (degrees per second)")]
        [SerializeField]
        private float aimSpeed = 90f;

        [Tooltip("Whether to aim at the target's center")]
        [SerializeField]
        private bool aimAtCenter = true;

        [Tooltip("Aim offset from target center")]
        [SerializeField]
        private Vector3 aimOffset = Vector3.zero;

        [Header("Debug")]
        [Tooltip("Show debug information in the console")]
        [SerializeField]
        private bool debugMode = false;

        [Tooltip("Show shooting gizmos in the scene")]
        [SerializeField]
        private bool showGizmos = true;

        #endregion

        #region Events

        /// <summary>
        /// Event fired when shooting starts.
        /// </summary>
        public event System.Action<Transform> OnShootingStarted;

        /// <summary>
        /// Event fired when shooting stops.
        /// </summary>
        public event System.Action OnShootingStopped;

        /// <summary>
        /// Event fired when a shot is fired.
        /// </summary>
        public event System.Action<Vector3> OnShotFired;

        /// <summary>
        /// Event fired when aiming at a target.
        /// </summary>
        public event System.Action<Transform, Vector3> OnAimingAtTarget;

        #endregion

        #region Properties

        /// <summary>
        /// Gets whether the AI is currently shooting.
        /// </summary>
        public bool IsShooting { get; private set; }

        /// <summary>
        /// Gets whether the AI is currently aiming.
        /// </summary>
        public bool IsAiming { get; private set; }

        /// <summary>
        /// Gets the current target being shot at.
        /// </summary>
        public Transform CurrentTarget { get; private set; }

        /// <summary>
        /// Gets the current aim position.
        /// </summary>
        public Vector3 CurrentAimPosition { get; private set; }

        /// <summary>
        /// Gets or sets the ranged weapon.
        /// </summary>
        public RangedWeapon RangedWeapon
        {
            get => rangedWeapon;
            set => rangedWeapon = value;
        }

        /// <summary>
        /// Gets or sets the aim speed.
        /// </summary>
        public float AimSpeed
        {
            get => aimSpeed;
            set => aimSpeed = Mathf.Max(0f, value);
        }

        /// <summary>
        /// Gets or sets whether to lead the target.
        /// </summary>
        public bool LeadTarget
        {
            get => leadTarget;
            set => leadTarget = value;
        }

        #endregion

        #region Component References

        private EnemyAI enemyAI;

        #endregion

        #region State Variables

        private bool wasShooting;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            enemyAI = GetComponent<EnemyAI>();
            
            if (enemyAI == null)
            {
                Debug.LogError("[ShootingAI] EnemyAI component not found!");
                return;
            }

            // Find ranged weapon if not assigned and auto-find is enabled
            if (rangedWeapon == null && autoFindWeapon)
            {
                FindRangedWeapon();
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

            UpdateAiming();
            UpdateShootingBehavior();
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
        /// Attempts to shoot at the current target.
        /// </summary>
        /// <returns>True if the shot was fired, false otherwise.</returns>
        public bool TryShoot()
        {
            if (rangedWeapon == null)
            {
                if (debugMode)
                {
                    Debug.LogWarning("[ShootingAI] No ranged weapon assigned!");
                }
                return false;
            }

            if (enemyAI.Target == null)
            {
                if (debugMode)
                {
                    Debug.Log("[ShootingAI] Cannot shoot - no target");
                }
                return false;
            }

            if (!enemyAI.IsTargetInAttackRange)
            {
                if (debugMode)
                {
                    Debug.Log("[ShootingAI] Cannot shoot - target out of range");
                }
                return false;
            }

            if (requireFacingTarget && !IsFacingTarget())
            {
                if (debugMode)
                {
                    Debug.Log("[ShootingAI] Cannot shoot - not facing target");
                }
                return false;
            }

            // Calculate aim position
            Vector3 aimPosition = CalculateAimPosition();
            CurrentAimPosition = aimPosition;

            // Fire at the aim position
            bool fired = rangedWeapon.FireAt(aimPosition);

            if (fired)
            {
                OnShotFired?.Invoke(aimPosition);

                if (debugMode)
                {
                    Debug.Log($"[ShootingAI] Fired at {aimPosition}");
                }
            }

            return fired;
        }

        /// <summary>
        /// Starts continuous shooting mode.
        /// </summary>
        public void StartShooting()
        {
            if (IsShooting)
            {
                return;
            }

            IsShooting = true;
            CurrentTarget = enemyAI.Target;

            OnShootingStarted?.Invoke(CurrentTarget);

            if (debugMode)
            {
                Debug.Log("[ShootingAI] Started shooting");
            }
        }

        /// <summary>
        /// Stops continuous shooting mode.
        /// </summary>
        public void StopShooting()
        {
            if (!IsShooting)
            {
                return;
            }

            IsShooting = false;
            CurrentTarget = null;

            // Stop weapon if it's firing automatically
            if (rangedWeapon != null && rangedWeapon.AutomaticFire)
            {
                rangedWeapon.StopFiring();
            }

            OnShootingStopped?.Invoke();

            if (debugMode)
            {
                Debug.Log("[ShootingAI] Stopped shooting");
            }
        }

        /// <summary>
        /// Sets the ranged weapon.
        /// </summary>
        /// <param name="weapon">The new ranged weapon.</param>
        public void SetRangedWeapon(RangedWeapon weapon)
        {
            rangedWeapon = weapon;
        }

        /// <summary>
        /// Sets the aim speed.
        /// </summary>
        /// <param name="speed">The new aim speed in degrees per second.</param>
        public void SetAimSpeed(float speed)
        {
            AimSpeed = speed;
        }

        /// <summary>
        /// Sets whether to lead the target.
        /// </summary>
        /// <param name="lead">Whether to lead the target.</param>
        public void SetLeadTarget(bool lead)
        {
            LeadTarget = lead;
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

        /// <summary>
        /// Calculates the aim position for shooting at the target.
        /// </summary>
        /// <returns>The aim position.</returns>
        public Vector3 CalculateAimPosition()
        {
            if (enemyAI.Target == null)
            {
                return transform.position + transform.forward * 10f;
            }

            Vector3 targetPosition = enemyAI.Target.position;

            // Apply aim offset
            if (!aimAtCenter)
            {
                targetPosition += aimOffset;
            }

            // Lead the target if enabled
            if (leadTarget)
            {
                targetPosition = CalculateLeadPosition(targetPosition);
            }

            return targetPosition;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Finds a RangedWeapon component on this GameObject or its children.
        /// </summary>
        private void FindRangedWeapon()
        {
            // Try to find on this GameObject first
            rangedWeapon = GetComponent<RangedWeapon>();

            // If not found, search in children
            if (rangedWeapon == null)
            {
                rangedWeapon = GetComponentInChildren<RangedWeapon>();
            }

            if (rangedWeapon != null)
            {
                if (debugMode)
                {
                    Debug.Log($"[ShootingAI] Found RangedWeapon: {rangedWeapon.name}");
                }
            }
            else
            {
                Debug.LogWarning("[ShootingAI] No RangedWeapon found on this GameObject or its children!");
            }
        }

        /// <summary>
        /// Updates aiming behavior.
        /// </summary>
        private void UpdateAiming()
        {
            // Only aim if we're shooting or if target is in attack range
            // This prevents rotation conflicts with PatrollingAI when target is out of range
            if (enemyAI.Target == null || (!IsShooting && !enemyAI.IsTargetInAttackRange))
            {
                IsAiming = false;
                return;
            }

            IsAiming = true;

            // Calculate aim position
            Vector3 aimPosition = CalculateAimPosition();
            CurrentAimPosition = aimPosition;

            // Aim at the target
            AimAtPosition(aimPosition);

            // Fire aiming event
            OnAimingAtTarget?.Invoke(enemyAI.Target, aimPosition);
        }

        /// <summary>
        /// Aims at a specific position.
        /// </summary>
        /// <param name="targetPosition">The position to aim at.</param>
        private void AimAtPosition(Vector3 targetPosition)
        {
            Vector3 direction = (targetPosition - transform.position).normalized;
            direction.y = 0f;

            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    targetRotation,
                    aimSpeed * Time.deltaTime
                );
            }
        }

        /// <summary>
        /// Calculates the lead position for target prediction.
        /// </summary>
        /// <param name="targetPosition">The current target position.</param>
        /// <returns>The predicted lead position.</returns>
        private Vector3 CalculateLeadPosition(Vector3 targetPosition)
        {
            if (enemyAI.Target == null)
            {
                return targetPosition;
            }

            // Try to get velocity from Rigidbody
            Rigidbody targetRigidbody = enemyAI.Target.GetComponent<Rigidbody>();
            Vector3 targetVelocity = Vector3.zero;

            if (targetRigidbody != null)
            {
                targetVelocity = targetRigidbody.linearVelocity;
            }
            else
            {
                // Try to get velocity from character controller or other movement component
                // This is a simplified approach - you may need to adjust based on your character controller
                targetVelocity = Vector3.zero;
            }

            // Calculate lead position
            Vector3 leadPosition = targetPosition + targetVelocity * leadPredictionTime;

            return leadPosition;
        }

        /// <summary>
        /// Updates the shooting behavior.
        /// </summary>
        private void UpdateShootingBehavior()
        {
            if (!IsShooting)
            {
                return;
            }

            // Check if we should stop shooting
            if (enemyAI.Target == null || (!enemyAI.IsTargetInAttackRange && stopOnTargetOutOfRange))
            {
                StopShooting();
                return;
            }

            // Check if weapon is available
            if (rangedWeapon == null)
            {
                return;
            }

            // Shoot if weapon can fire
            if (rangedWeapon.CanFire)
            {
                // Check if facing target (if required)
                if (requireFacingTarget && !IsFacingTarget())
                {
                    return;
                }

                // Fire the weapon
                TryShoot();
            }
        }

        /// <summary>
        /// Handles target in attack range event from EnemyAI.
        /// </summary>
        private void HandleTargetInAttackRange(Transform target)
        {
            if (autoShoot)
            {
                StartShooting();
            }
        }

        /// <summary>
        /// Handles target out of attack range event from EnemyAI.
        /// </summary>
        private void HandleTargetOutOfRange()
        {
            if (stopOnTargetOutOfRange)
            {
                StopShooting();

                if (debugMode)
                {
                    Debug.Log("[ShootingAI] Target out of attack range - stopped shooting");
                }
            }
        }

        #endregion

        #region Editor

        private void OnValidate()
        {
            aimSpeed = Mathf.Max(0f, aimSpeed);
            facingAngleTolerance = Mathf.Clamp(facingAngleTolerance, 0f, 180f);
            leadPredictionTime = Mathf.Max(0f, leadPredictionTime);
        }

        private void OnDrawGizmos()
        {
            if (!showGizmos)
            {
                return;
            }

            // Draw shooting line
            if (IsShooting && CurrentTarget != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, CurrentTarget.position);
            }

            // Draw aim line
            if (IsAiming && CurrentAimPosition != Vector3.zero)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, CurrentAimPosition);
                Gizmos.DrawWireSphere(CurrentAimPosition, 0.2f);
            }

            // Draw facing cone
            if (requireFacingTarget)
            {
                Gizmos.color = Color.cyan;
                Vector3 leftDirection = Quaternion.Euler(0f, -facingAngleTolerance, 0f) * transform.forward;
                Vector3 rightDirection = Quaternion.Euler(0f, facingAngleTolerance, 0f) * transform.forward;
                
                Gizmos.DrawRay(transform.position, leftDirection * 5f);
                Gizmos.DrawRay(transform.position, rightDirection * 5f);
            }
        }

        #endregion
    }
}
