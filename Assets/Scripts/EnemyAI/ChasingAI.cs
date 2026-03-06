using UnityEngine;

namespace FindersCheesers
{
    /// <summary>
    /// Component that adds chasing behavior to an EnemyAI.
    /// Pursues the target when detected and optionally stops at attack range.
    /// </summary>
    [AddComponentMenu("Finders Cheesers/EnemyAI/ChasingAI")]
    [RequireComponent(typeof(EnemyAI))]
    public class ChasingAI : MonoBehaviour
    {
        #region Settings

        [Header("Chase Settings")]
        [Tooltip("Whether to start chasing when a target is detected")]
        [SerializeField]
        private bool autoChaseOnDetection = true;

        [Tooltip("Whether to stop chasing when target enters attack range")]
        [SerializeField]
        private bool stopAtAttackRange = true;

        [Tooltip("Whether to resume chasing when target leaves attack range")]
        [SerializeField]
        private bool resumeChaseAfterAttack = true;

        [Tooltip("Whether to stop chasing when target is lost")]
        [SerializeField]
        private bool stopOnTargetLost = true;

        [Tooltip("Whether to return to last known position when target is lost")]
        [SerializeField]
        private bool returnToLastKnownPosition = false;

        [Tooltip("How close to get to the last known position")]
        [SerializeField]
        private float lastKnownPositionThreshold = 1f;

        [Tooltip("Movement speed multiplier while chasing (1.0 = normal speed)")]
        [SerializeField]
        private float chaseSpeedMultiplier = 1.2f;

        [Tooltip("Rotation speed multiplier while chasing (1.0 = normal speed)")]
        [SerializeField]
        private float chaseRotationMultiplier = 1.5f;

        [Header("Debug")]
        [Tooltip("Show debug information in the console")]
        [SerializeField]
        private bool debugMode = false;

        [Tooltip("Show chase gizmos in the scene")]
        [SerializeField]
        private bool showGizmos = true;

        #endregion

        #region Events

        /// <summary>
        /// Event fired when chasing starts.
        /// </summary>
        public event System.Action<Transform> OnChaseStarted;

        /// <summary>
        /// Event fired when chasing stops.
        /// </summary>
        public event System.Action OnChaseStopped;

        /// <summary>
        /// Event fired when returning to last known position.
        /// </summary>
        public event System.Action<Vector3> OnReturningToLastKnownPosition;

        /// <summary>
        /// Event fired when the last known position is reached.
        /// </summary>
        public event System.Action OnLastKnownPositionReached;

        #endregion

        #region Properties

        /// <summary>
        /// Gets whether the AI is currently chasing.
        /// </summary>
        public bool IsChasing { get; private set; }

        /// <summary>
        /// Gets whether the AI is returning to last known position.
        /// </summary>
        public bool IsReturningToLastKnownPosition { get; private set; }

        /// <summary>
        /// Gets the last known position of the target.
        /// </summary>
        public Vector3 LastKnownPosition { get; private set; }

        /// <summary>
        /// Gets the current target being chased.
        /// </summary>
        public Transform CurrentTarget { get; private set; }

        #endregion

        #region Component References

        private EnemyAI enemyAI;

        #endregion

        #region State Variables

        private float originalMoveSpeed;
        private float originalRotationSpeed;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            enemyAI = GetComponent<EnemyAI>();
            
            if (enemyAI == null)
            {
                Debug.LogError("[ChasingAI] EnemyAI component not found!");
                return;
            }

            // Store original speeds
            originalMoveSpeed = enemyAI.MoveSpeed;
            originalRotationSpeed = enemyAI.RotationSpeed;

            // Subscribe to EnemyAI events
            enemyAI.OnTargetDetected += HandleTargetDetected;
            enemyAI.OnTargetLost += HandleTargetLost;
            enemyAI.OnTargetInAttackRange += HandleTargetInAttackRange;
            enemyAI.OnTargetOutOfAttackRange += HandleTargetOutOfAttackRange;
        }

        private void Update()
        {
            if (!enemyAI.IsActive)
            {
                return;
            }

            UpdateChaseBehavior();
        }

        private void OnDestroy()
        {
            if (enemyAI != null)
            {
                enemyAI.OnTargetDetected -= HandleTargetDetected;
                enemyAI.OnTargetLost -= HandleTargetLost;
                enemyAI.OnTargetInAttackRange -= HandleTargetInAttackRange;
                enemyAI.OnTargetOutOfAttackRange -= HandleTargetOutOfAttackRange;
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Starts chasing the current target.
        /// </summary>
        public void StartChasing()
        {
            if (enemyAI.Target == null)
            {
                Debug.LogWarning("[ChasingAI] No target to chase!");
                return;
            }

            if (IsChasing)
            {
                return;
            }

            IsChasing = true;
            IsReturningToLastKnownPosition = false;
            CurrentTarget = enemyAI.Target;

            // Apply chase speed multipliers
            ApplyChaseSpeeds();

            OnChaseStarted?.Invoke(CurrentTarget);

            if (debugMode)
            {
                Debug.Log($"[ChasingAI] Started chasing {CurrentTarget.name}");
            }
        }

        /// <summary>
        /// Stops chasing the current target.
        /// </summary>
        public void StopChasing()
        {
            if (!IsChasing)
            {
                return;
            }

            IsChasing = false;
            CurrentTarget = null;

            // Stop NavMeshAgent movement
            enemyAI.StopMovement();

            // Restore original speeds
            RestoreOriginalSpeeds();

            OnChaseStopped?.Invoke();

            if (debugMode)
            {
                Debug.Log("[ChasingAI] Stopped chasing");
            }
        }

        /// <summary>
        /// Sets the chase speed multiplier.
        /// </summary>
        /// <param name="multiplier">The new speed multiplier.</param>
        public void SetChaseSpeedMultiplier(float multiplier)
        {
            chaseSpeedMultiplier = Mathf.Max(0.1f, multiplier);
            
            if (IsChasing)
            {
                ApplyChaseSpeeds();
            }
        }

        /// <summary>
        /// Sets the chase rotation speed multiplier.
        /// </summary>
        /// <param name="multiplier">The new rotation speed multiplier.</param>
        public void SetChaseRotationMultiplier(float multiplier)
        {
            chaseRotationMultiplier = Mathf.Max(0.1f, multiplier);
            
            if (IsChasing)
            {
                ApplyChaseSpeeds();
            }
        }

        /// <summary>
        /// Forces a chase of a specific target.
        /// </summary>
        /// <param name="target">The target to chase.</param>
        public void ChaseTarget(Transform target)
        {
            if (target == null)
            {
                Debug.LogWarning("[ChasingAI] Cannot chase null target!");
                return;
            }

            enemyAI.SetTarget(target, true);
            StartChasing();
        }

        /// <summary>
        /// Returns to the last known position of the target.
        /// </summary>
        public void ReturnToLastKnownPosition()
        {
            if (IsReturningToLastKnownPosition)
            {
                return;
            }

            // Check if already at last known position
            bool atPosition = false;
            if (enemyAI.UseNavMeshAgent && enemyAI.IsNavMeshAgentAvailable)
            {
                atPosition = enemyAI.NavMeshAgent.remainingDistance <= lastKnownPositionThreshold &&
                           enemyAI.NavMeshAgent.hasPath;
            }
            else if (enemyAI.UseNavAgentHopping && enemyAI.IsNavAgentHoppingAvailable)
            {
                atPosition = enemyAI.NavAgentHoppingController.RemainingDistance <= lastKnownPositionThreshold &&
                           enemyAI.NavAgentHoppingController.HasPath;
            }
            else
            {
                atPosition = Vector3.Distance(transform.position, LastKnownPosition) < lastKnownPositionThreshold;
            }

            if (atPosition)
            {
                OnLastKnownPositionReached?.Invoke();
                return;
            }

            IsChasing = false;
            IsReturningToLastKnownPosition = true;

            // Reset NavMeshAgent path to force recalculation
            if (enemyAI.UseNavMeshAgent && enemyAI.IsNavMeshAgentAvailable)
            {
                enemyAI.NavMeshAgent.ResetPath();
            }
            // Reset NavAgentHoppingController path to force recalculation
            else if (enemyAI.UseNavAgentHopping && enemyAI.IsNavAgentHoppingAvailable)
            {
                enemyAI.NavAgentHoppingController.RecalculatePath();
            }

            // Restore original speeds for returning
            RestoreOriginalSpeeds();

            OnReturningToLastKnownPosition?.Invoke(LastKnownPosition);

            if (debugMode)
            {
                Debug.Log($"[ChasingAI] Returning to last known position at {LastKnownPosition}");
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Updates the chase behavior.
        /// </summary>
        private void UpdateChaseBehavior()
        {
            if (IsChasing && CurrentTarget != null)
            {
                UpdateChaseMovement();
            }
            else if (IsReturningToLastKnownPosition)
            {
                UpdateReturnMovement();
            }
        }

        /// <summary>
        /// Updates movement while chasing the target.
        /// </summary>
        private void UpdateChaseMovement()
        {
            if (CurrentTarget == null)
            {
                StopChasing();
                return;
            }

            // Update last known position
            LastKnownPosition = CurrentTarget.position;

            // Move towards target
            enemyAI.MoveTowards(CurrentTarget.position, Time.deltaTime);
            enemyAI.FaceTarget(CurrentTarget.position, Time.deltaTime);
        }

        /// <summary>
        /// Updates movement while returning to last known position.
        /// </summary>
        private void UpdateReturnMovement()
        {
            // Check if last known position is reached
            bool positionReached = false;

            if (enemyAI.UseNavMeshAgent && enemyAI.IsNavMeshAgentAvailable)
            {
                // Use NavMeshAgent's remaining distance to check if position is reached
                positionReached = enemyAI.NavMeshAgent.remainingDistance <= lastKnownPositionThreshold &&
                               enemyAI.NavMeshAgent.hasPath;
            }
            else if (enemyAI.UseNavAgentHopping && enemyAI.IsNavAgentHoppingAvailable)
            {
                // Use NavAgentHoppingController's remaining distance to check if position is reached
                positionReached = enemyAI.NavAgentHoppingController.RemainingDistance <= lastKnownPositionThreshold &&
                               enemyAI.NavAgentHoppingController.HasPath;
            }
            else
            {
                // Use direct distance check
                positionReached = Vector3.Distance(transform.position, LastKnownPosition) <= lastKnownPositionThreshold;
            }

            if (positionReached)
            {
                IsReturningToLastKnownPosition = false;
                OnLastKnownPositionReached?.Invoke();

                if (debugMode)
                {
                    Debug.Log("[ChasingAI] Reached last known position");
                }
            }
            else
            {
                enemyAI.MoveTowards(LastKnownPosition, Time.deltaTime);
                enemyAI.FaceTarget(LastKnownPosition, Time.deltaTime);
            }
        }

        /// <summary>
        /// Applies chase speed multipliers.
        /// </summary>
        private void ApplyChaseSpeeds()
        {
            enemyAI.MoveSpeed = originalMoveSpeed * chaseSpeedMultiplier;
            enemyAI.RotationSpeed = originalRotationSpeed * chaseRotationMultiplier;
        }

        /// <summary>
        /// Restores original speeds.
        /// </summary>
        private void RestoreOriginalSpeeds()
        {
            enemyAI.MoveSpeed = originalMoveSpeed;
            enemyAI.RotationSpeed = originalRotationSpeed;
        }

        /// <summary>
        /// Handles target detected event from EnemyAI.
        /// </summary>
        private void HandleTargetDetected(Transform target)
        {
            if (autoChaseOnDetection)
            {
                StartChasing();
            }
        }

        /// <summary>
        /// Handles target lost event from EnemyAI.
        /// </summary>
        private void HandleTargetLost()
        {
            if (stopOnTargetLost)
            {
                StopChasing();

                if (returnToLastKnownPosition)
                {
                    ReturnToLastKnownPosition();
                }

                if (debugMode)
                {
                    Debug.Log("[ChasingAI] Target lost");
                }
            }
        }

        /// <summary>
        /// Handles target in attack range event from EnemyAI.
        /// </summary>
        private void HandleTargetInAttackRange(Transform target)
        {
            if (stopAtAttackRange)
            {
                StopChasing();

                if (debugMode)
                {
                    Debug.Log("[ChasingAI] Stopped chasing - target in attack range");
                }
            }
        }

        /// <summary>
        /// Handles target out of attack range event from EnemyAI.
        /// </summary>
        private void HandleTargetOutOfAttackRange()
        {
            if (resumeChaseAfterAttack && enemyAI.IsTargetDetected)
            {
                StartChasing();

                if (debugMode)
                {
                    Debug.Log("[ChasingAI] Resumed chasing - target out of attack range");
                }
            }
        }

        #endregion

        #region Editor

        private void OnValidate()
        {
            chaseSpeedMultiplier = Mathf.Max(0.1f, chaseSpeedMultiplier);
            chaseRotationMultiplier = Mathf.Max(0.1f, chaseRotationMultiplier);
            lastKnownPositionThreshold = Mathf.Max(0.1f, lastKnownPositionThreshold);
        }

        private void OnDrawGizmos()
        {
            if (!showGizmos)
            {
                return;
            }

            // Draw chase line
            if (IsChasing && CurrentTarget != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, CurrentTarget.position);
            }

            // Draw last known position
            if (IsReturningToLastKnownPosition)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(LastKnownPosition, lastKnownPositionThreshold);
                Gizmos.DrawLine(transform.position, LastKnownPosition);
            }
        }

        #endregion
    }
}
