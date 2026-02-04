using UnityEngine;

namespace FindersCheesers
{
    /// <summary>
    /// Base component for enemy AI behavior.
    /// Provides common functionality for enemy AI behaviors like Patrolling, Chasing, and Attacking.
    /// </summary>
    [AddComponentMenu("Finders Cheesers/EnemyAI")]
    public class EnemyAI : MonoBehaviour
    {
        #region Settings

        [Header("AI Settings")]
        [Tooltip("The target to track (e.g., player)")]
        [SerializeField]
        private Transform target;

        [Tooltip("Detection range for the enemy")]
        [SerializeField]
        private float detectionRange = 10f;

        [Tooltip("Attack range for the enemy")]
        [SerializeField]
        private float attackRange = 2f;

        [Tooltip("Movement speed")]
        [SerializeField]
        private float moveSpeed = 3f;

        [Tooltip("Rotation speed for facing targets")]
        [SerializeField]
        private float rotationSpeed = 5f;

        [Tooltip("Whether to use NavMeshAgent for movement")]
        [SerializeField]
        private bool useNavMeshAgent = true;

        [Tooltip("Angular speed for NavMeshAgent rotation")]
        [SerializeField]
        private float navMeshAngularSpeed = 120f;

        [Tooltip("Stopping distance for NavMeshAgent")]
        [SerializeField]
        private float navMeshStoppingDistance = 0.5f;

        [Tooltip("Whether the AI is active and processing")]
        [SerializeField]
        private bool isActive = true;

        [Header("Debug")]
        [Tooltip("Show debug information in the console")]
        [SerializeField]
        private bool debugMode = false;

        [Tooltip("Show debug gizmos in the scene")]
        [SerializeField]
        private bool showGizmos = true;

        #endregion

        #region Events

        /// <summary>
        /// Event fired when the target is detected.
        /// </summary>
        public event System.Action<Transform> OnTargetDetected;

        /// <summary>
        /// Event fired when the target is lost.
        /// </summary>
        public event System.Action OnTargetLost;

        /// <summary>
        /// Event fired when the target enters attack range.
        /// </summary>
        public event System.Action<Transform> OnTargetInAttackRange;

        /// <summary>
        /// Event fired when the target leaves attack range.
        /// </summary>
        public event System.Action OnTargetOutOfAttackRange;

        #endregion

        #region Component References

        private UnityEngine.AI.NavMeshAgent navMeshAgent;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the target transform.
        /// </summary>
        public Transform Target
        {
            get => target;
            set
            {
                if (target != value)
                {
                    target = value;
                    if (debugMode)
                    {
                        Debug.Log($"[EnemyAI] Target set to: {(target != null ? target.name : "null")}");
                    }
                }
            }
        }

        /// <summary>
        /// Gets the detection range.
        /// </summary>
        public float DetectionRange => detectionRange;

        /// <summary>
        /// Gets the attack range.
        /// </summary>
        public float AttackRange => attackRange;

        /// <summary>
        /// Gets or sets the movement speed.
        /// </summary>
        public float MoveSpeed
        {
            get => moveSpeed;
            set => moveSpeed = Mathf.Max(0f, value);
        }

        /// <summary>
        /// Gets or sets the rotation speed.
        /// </summary>
        public float RotationSpeed
        {
            get => rotationSpeed;
            set => rotationSpeed = Mathf.Max(0f, value);
        }

        /// <summary>
        /// Gets or sets whether the AI is active.
        /// </summary>
        public bool IsActive
        {
            get => isActive;
            set => isActive = value;
        }

        /// <summary>
        /// Gets whether a target is currently detected.
        /// </summary>
        public bool IsTargetDetected { get; private set; }

        /// <summary>
        /// Gets whether the target is in attack range.
        /// </summary>
        public bool IsTargetInAttackRange { get; private set; }

        /// <summary>
        /// Gets the distance to the target.
        /// </summary>
        public float DistanceToTarget
        {
            get
            {
                if (target == null)
                {
                    return float.MaxValue;
                }
                return Vector3.Distance(transform.position, target.position);
            }
        }

        /// <summary>
        /// Gets whether NavMeshAgent is being used for movement.
        /// </summary>
        public bool UseNavMeshAgent => useNavMeshAgent;

        /// <summary>
        /// Gets the NavMeshAgent component (null if not available).
        /// </summary>
        public UnityEngine.AI.NavMeshAgent NavMeshAgent => navMeshAgent;

        /// <summary>
        /// Gets whether NavMeshAgent is available and enabled.
        /// </summary>
        public bool IsNavMeshAgentAvailable => navMeshAgent != null && navMeshAgent.enabled;

        #endregion

        #region Unity Lifecycle

        protected virtual void Awake()
        {
            // Get NavMeshAgent component if it exists
            navMeshAgent = GetComponent<UnityEngine.AI.NavMeshAgent>();
            
            // Configure NavMeshAgent if available
            if (navMeshAgent != null)
            {
                navMeshAgent.speed = moveSpeed;
                navMeshAgent.angularSpeed = navMeshAngularSpeed;
                navMeshAgent.stoppingDistance = navMeshStoppingDistance;
                navMeshAgent.autoBraking = true;
            }
        }

        protected virtual void Update()
        {
            if (!isActive)
            {
                return;
            }

            CheckTargetDetection();
        }

        protected virtual void FixedUpdate()
        {
            if (!isActive)
            {
                return;
            }

            CheckAttackRange();
        }

        #endregion

        #region Public API

        /// <summary>
        /// Moves towards a target position.
        /// </summary>
        /// <param name="targetPosition">The position to move towards.</param>
        /// <param name="deltaTime">The time delta for movement.</param>
        public void MoveTowards(Vector3 targetPosition, float deltaTime)
        {
            // Use NavMeshAgent if available and enabled
            if (useNavMeshAgent && IsNavMeshAgentAvailable)
            {
                // Set destination if not already set
                if (navMeshAgent.destination != targetPosition || !navMeshAgent.hasPath)
                {
                    navMeshAgent.SetDestination(targetPosition);
                }
                
                // Update speed
                navMeshAgent.speed = moveSpeed;

                if (debugMode)
                {
                    Debug.Log($"[EnemyAI] Moving towards {targetPosition} using NavMeshAgent");
                }
            }
            else
            {
                // Use direct movement
                Vector3 direction = (targetPosition - transform.position).normalized;
                direction.y = 0f; // Keep movement on horizontal plane

                if (direction != Vector3.zero)
                {
                    transform.position += direction * moveSpeed * deltaTime;
                }

                if (debugMode)
                {
                    Debug.Log($"[EnemyAI] Moving towards {targetPosition} using direct movement");
                }
            }
        }

        /// <summary>
        /// Rotates to face a target position.
        /// </summary>
        /// <param name="targetPosition">The position to face.</param>
        /// <param name="deltaTime">The time delta for rotation.</param>
        public void FaceTarget(Vector3 targetPosition, float deltaTime)
        {
            // NavMeshAgent handles rotation automatically when moving
            if (useNavMeshAgent && IsNavMeshAgentAvailable)
            {
                // NavMeshAgent handles rotation, so we don't need to manually rotate
                // But we can update the angular speed if needed
                navMeshAgent.angularSpeed = navMeshAngularSpeed;
                return;
            }

            // Use manual rotation for direct movement
            Vector3 direction = (targetPosition - transform.position).normalized;
            direction.y = 0f; // Keep rotation on horizontal plane

            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    rotationSpeed * deltaTime
                );
            }
        }

        /// <summary>
        /// Checks if the target is within the detection range.
        /// </summary>
        /// <returns>True if target is detected, false otherwise.</returns>
        public bool CheckDetection()
        {
            return target != null && DistanceToTarget <= detectionRange;
        }

        /// <summary>
        /// Checks if the target is within attack range.
        /// </summary>
        /// <returns>True if target is in attack range, false otherwise.</returns>
        public bool CheckAttackRangeDistance()
        {
            return target != null && DistanceToTarget <= attackRange;
        }

        /// <summary>
        /// Sets the target and optionally activates the AI.
        /// </summary>
        /// <param name="newTarget">The new target transform.</param>
        /// <param name="activateAI">Whether to activate the AI.</param>
        public void SetTarget(Transform newTarget, bool activateAI = true)
        {
            Target = newTarget;
            if (activateAI)
            {
                isActive = true;
            }
        }

        /// <summary>
        /// Clears the current target.
        /// </summary>
        public void ClearTarget()
        {
            Target = null;
            IsTargetDetected = false;
            IsTargetInAttackRange = false;
        }

        /// <summary>
        /// Activates the AI.
        /// </summary>
        public void Activate()
        {
            isActive = true;
            if (debugMode)
            {
                Debug.Log($"[EnemyAI] AI activated on {gameObject.name}");
            }
        }

        /// <summary>
        /// Deactivates the AI.
        /// </summary>
        public void Deactivate()
        {
            isActive = false;
            if (debugMode)
            {
                Debug.Log($"[EnemyAI] AI deactivated on {gameObject.name}");
            }
        }

        /// <summary>
        /// Stops movement by clearing the NavMeshAgent path.
        /// </summary>
        public void StopMovement()
        {
            if (IsNavMeshAgentAvailable)
            {
                navMeshAgent.ResetPath();
            }

            if (debugMode)
            {
                Debug.Log($"[EnemyAI] Movement stopped on {gameObject.name}");
            }
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Checks for target detection and fires events.
        /// </summary>
        protected virtual void CheckTargetDetection()
        {
            bool wasDetected = IsTargetDetected;
            IsTargetDetected = CheckDetection();

            if (IsTargetDetected && !wasDetected)
            {
                OnTargetDetected?.Invoke(target);
                if (debugMode)
                {
                    Debug.Log($"[EnemyAI] Target detected: {target.name}");
                }
            }
            else if (!IsTargetDetected && wasDetected)
            {
                OnTargetLost?.Invoke();
                if (debugMode)
                {
                    Debug.Log($"[EnemyAI] Target lost");
                }
            }
        }

        /// <summary>
        /// Checks if target is in attack range and fires events.
        /// </summary>
        protected virtual void CheckAttackRange()
        {
            bool wasInRange = IsTargetInAttackRange;
            IsTargetInAttackRange = CheckAttackRangeDistance();

            if (IsTargetInAttackRange && !wasInRange)
            {
                OnTargetInAttackRange?.Invoke(target);
                if (debugMode)
                {
                    Debug.Log($"[EnemyAI] Target in attack range: {target.name}");
                }
            }
            else if (!IsTargetInAttackRange && wasInRange)
            {
                OnTargetOutOfAttackRange?.Invoke();
                if (debugMode)
                {
                    Debug.Log($"[EnemyAI] Target out of attack range");
                }
            }
        }

        #endregion

        #region Editor

        protected virtual void OnValidate()
        {
            detectionRange = Mathf.Max(0f, detectionRange);
            attackRange = Mathf.Max(0f, attackRange);
            moveSpeed = Mathf.Max(0f, moveSpeed);
            rotationSpeed = Mathf.Max(0f, rotationSpeed);
            navMeshAngularSpeed = Mathf.Max(0f, navMeshAngularSpeed);
            navMeshStoppingDistance = Mathf.Max(0f, navMeshStoppingDistance);
        }

        protected virtual void OnDrawGizmos()
        {
            if (!showGizmos)
            {
                return;
            }

            // Draw detection range
            Gizmos.color = IsTargetDetected ? Color.yellow : Color.cyan;
            Gizmos.DrawWireSphere(transform.position, detectionRange);

            // Draw attack range
            Gizmos.color = IsTargetInAttackRange ? Color.red : Color.green;
            Gizmos.DrawWireSphere(transform.position, attackRange);

            // Draw line to target if detected
            if (target != null && IsTargetDetected)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, target.position);
            }

            // Draw forward direction
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, transform.forward * 2f);
        }

        #endregion
    }
}
