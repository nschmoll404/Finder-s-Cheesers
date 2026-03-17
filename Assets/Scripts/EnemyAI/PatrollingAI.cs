using UnityEngine;

namespace FindersCheesers
{
    /// <summary>
    /// Component that adds patrolling behavior to an EnemyAI.
    /// Moves between waypoints in a patrol path.
    /// </summary>
    [AddComponentMenu("Finders Cheesers/EnemyAI/PatrollingAI")]
    [RequireComponent(typeof(EnemyAI))]
    public class PatrollingAI : MonoBehaviour, IEnemyAIComponent
    {
        #region IEnemyAIComponent Implementation

        public bool IsTriggered => enemyAI != null && !enemyAI.IsTargetDetected && (IsPatrolling || (!IsPatrolling && waypoints != null && waypoints.Length > 0));
        public bool IsRunning { get; set; }

        public event System.Action OnActivated;
        public event System.Action OnDeactivated;

        /// <summary>
        /// Called by EnemyAI when this component transitions into the running state.
        /// Starts patrolling behavior.
        /// </summary>
        public void OnStartRunning()
        {
            IsRunning = true;
            StartPatrolling();
        }

        /// <summary>
        /// Called by EnemyAI when this component transitions out of the running state.
        /// Stops patrolling behavior.
        /// </summary>
        public void OnExitRunning()
        {
            IsRunning = false;
            StopPatrolling();
        }

        #endregion

        #region Settings

        [Header("Patrol Settings")]
        [Tooltip("The waypoints to patrol between")]
        [SerializeField]
        private Transform[] waypoints;

        [Tooltip("Whether to patrol in a loop or ping-pong back and forth")]
        [SerializeField]
        private PatrolMode patrolMode = PatrolMode.Loop;

        [Tooltip("How close the enemy needs to be to a waypoint before moving to the next")]
        [SerializeField]
        private float waypointThreshold = 0.5f;

        [Tooltip("Time to wait at each waypoint before moving to the next")]
        [SerializeField]
        private float waitTimeAtWaypoint = 1f;

        [Tooltip("Whether to start patrolling immediately")]
        [SerializeField]
        private bool startPatrollingOnAwake = true;

        [Tooltip("Whether to stop patrolling when a target is detected")]
        [SerializeField]
        private bool stopOnTargetDetected = true;

        [Tooltip("Whether to resume patrolling when target is lost")]
        [SerializeField]
        private bool resumeOnTargetLost = true;

        [Header("Priority Settings")]
        [Tooltip("Priority of this AI component (higher values take precedence when multiple AI components are triggered)")]
        [SerializeField]
        private int priority = 0;

        [Header("Debug")]
        [Tooltip("Show debug information in the console")]
        [SerializeField]
        private bool debugMode = false;

        [Tooltip("Show patrol path gizmos in the scene")]
        [SerializeField]
        private bool showGizmos = true;

        #endregion

        #region Enums

        /// <summary>
        /// Defines the patrol mode behavior.
        /// </summary>
        public enum PatrolMode
        {
            /// <summary>Patrol in a continuous loop</summary>
            Loop,
            /// <summary>Ping-pong back and forth between waypoints</summary>
            PingPong
        }

        #endregion

        #region Events

        /// <summary>
        /// Event fired when patrolling starts.
        /// </summary>
        public event System.Action OnPatrolStarted;

        /// <summary>
        /// Event fired when patrolling stops.
        /// </summary>
        public event System.Action OnPatrolStopped;

        /// <summary>
        /// Event fired when a waypoint is reached.
        /// </summary>
        public event System.Action<int> OnWaypointReached;

        /// <summary>
        /// Event fired when waiting at a waypoint.
        /// </summary>
        public event System.Action<float> OnWaitingAtWaypoint;

        #endregion

        #region Properties

        /// <summary>
        /// Gets whether the AI is currently patrolling.
        /// </summary>
        public bool IsPatrolling { get; private set; }

        /// <summary>
        /// Gets the current waypoint index.
        /// </summary>
        public int CurrentWaypointIndex { get; private set; }

        /// <summary>
        /// Gets whether the AI is currently waiting at a waypoint.
        /// </summary>
        public bool IsWaiting { get; private set; }

        /// <summary>
        /// Gets the patrol mode.
        /// </summary>
        public PatrolMode Mode => patrolMode;

        /// <summary>
        /// Gets the array of waypoints.
        /// </summary>
        public Transform[] Waypoints => waypoints;

        /// <summary>
        /// Gets the priority of this AI component.
        /// Higher priority values take precedence when multiple AI components are triggered.
        /// </summary>
        public int Priority => priority;

        #endregion

        #region Component References

        private EnemyAI enemyAI;

        #endregion

        #region State Variables

        private int patrolDirection = 1; // 1 for forward, -1 for backward (ping-pong)
        private float waitTimer;
        private bool hasStartedPatrolling;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            enemyAI = GetComponent<EnemyAI>();
            
            if (enemyAI == null)
            {
                Debug.LogError("[PatrollingAI] EnemyAI component not found!");
                return;
            }

            // Subscribe to EnemyAI events
            enemyAI.OnTargetDetected += HandleTargetDetected;
            enemyAI.OnTargetLost += HandleTargetLost;

            if (startPatrollingOnAwake && waypoints != null && waypoints.Length > 0)
            {
                hasStartedPatrolling = true;
            }
        }

        private void Start()
        {
            if (hasStartedPatrolling)
            {
                StartPatrolling();
            }
        }

        private void Update()
        {
            if (!IsRunning || !IsPatrolling || enemyAI == null || !enemyAI.IsActive)
            {
                return;
            }

            if (waypoints == null || waypoints.Length == 0)
            {
                return;
            }

            if (IsWaiting)
            {
                UpdateWaitTimer();
            }
            else
            {
                UpdatePatrolMovement();
            }
        }

        private void OnDestroy()
        {
            if (enemyAI != null)
            {
                enemyAI.OnTargetDetected -= HandleTargetDetected;
                enemyAI.OnTargetLost -= HandleTargetLost;
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Starts patrolling behavior.
        /// </summary>
        public void StartPatrolling()
        {
            if (waypoints == null || waypoints.Length == 0)
            {
                Debug.LogWarning("[PatrollingAI] No waypoints assigned!");
                return;
            }

            if (IsPatrolling)
            {
                return;
            }

            IsPatrolling = true;
            IsWaiting = false;
            waitTimer = 0f;
            
            // Find nearest waypoint to start from
            CurrentWaypointIndex = FindNearestWaypointIndex();
            
            OnPatrolStarted?.Invoke();
            OnActivated?.Invoke();
            
            if (debugMode)
            {
                Debug.Log($"[PatrollingAI] Started patrolling from waypoint {CurrentWaypointIndex}");
            }
        }

        /// <summary>
        /// Stops patrolling behavior.
        /// </summary>
        public void StopPatrolling()
        {
            if (!IsPatrolling)
            {
                return;
            }

            IsPatrolling = false;
            IsWaiting = false;
            
            // Stop NavMeshAgent movement
            enemyAI.StopMovement();
            
            OnPatrolStopped?.Invoke();
            OnDeactivated?.Invoke();
            
            if (debugMode)
            {
                Debug.Log("[PatrollingAI] Stopped patrolling");
            }
        }

        /// <summary>
        /// Sets the waypoints for patrolling.
        /// </summary>
        /// <param name="newWaypoints">The new waypoint array.</param>
        public void SetWaypoints(Transform[] newWaypoints)
        {
            waypoints = newWaypoints;
            
            if (debugMode)
            {
                Debug.Log($"[PatrollingAI] Set {waypoints?.Length ?? 0} waypoints");
            }
        }

        /// <summary>
        /// Adds a waypoint to the patrol path.
        /// </summary>
        /// <param name="waypoint">The waypoint to add.</param>
        public void AddWaypoint(Transform waypoint)
        {
            if (waypoint == null)
            {
                Debug.LogWarning("[PatrollingAI] Cannot add null waypoint!");
                return;
            }

            if (waypoints == null)
            {
                waypoints = new Transform[] { waypoint };
            }
            else
            {
                System.Array.Resize(ref waypoints, waypoints.Length + 1);
                waypoints[waypoints.Length - 1] = waypoint;
            }

            if (debugMode)
            {
                Debug.Log($"[PatrollingAI] Added waypoint at {waypoint.position}");
            }
        }

        /// <summary>
        /// Removes a waypoint from the patrol path.
        /// </summary>
        /// <param name="index">The index of the waypoint to remove.</param>
        public void RemoveWaypoint(int index)
        {
            if (waypoints == null || index < 0 || index >= waypoints.Length)
            {
                Debug.LogWarning("[PatrollingAI] Invalid waypoint index!");
                return;
            }

            // Create new array without the removed waypoint
            Transform[] newWaypoints = new Transform[waypoints.Length - 1];
            int newIndex = 0;
            for (int i = 0; i < waypoints.Length; i++)
            {
                if (i != index)
                {
                    newWaypoints[newIndex] = waypoints[i];
                    newIndex++;
                }
            }

            waypoints = newWaypoints;

            // Adjust current index if necessary
            if (CurrentWaypointIndex >= waypoints.Length)
            {
                CurrentWaypointIndex = 0;
            }

            if (debugMode)
            {
                Debug.Log($"[PatrollingAI] Removed waypoint at index {index}");
            }
        }

        /// <summary>
        /// Sets the patrol mode.
        /// </summary>
        /// <param name="mode">The new patrol mode.</param>
        public void SetPatrolMode(PatrolMode mode)
        {
            patrolMode = mode;
            
            if (debugMode)
            {
                Debug.Log($"[PatrollingAI] Patrol mode set to {mode}");
            }
        }

        /// <summary>
        /// Sets the waypoint threshold distance.
        /// </summary>
        /// <param name="threshold">The new threshold distance.</param>
        public void SetWaypointThreshold(float threshold)
        {
            waypointThreshold = Mathf.Max(0.1f, threshold);
        }

        /// <summary>
        /// Sets the wait time at waypoints.
        /// </summary>
        /// <param name="waitTime">The new wait time in seconds.</param>
        public void SetWaitTime(float waitTime)
        {
            waitTimeAtWaypoint = Mathf.Max(0f, waitTime);
        }

        /// <summary>
        /// Forces movement to a specific waypoint.
        /// </summary>
        /// <param name="waypointIndex">The index of the waypoint to move to.</param>
        public void GoToWaypoint(int waypointIndex)
        {
            if (waypoints == null || waypointIndex < 0 || waypointIndex >= waypoints.Length)
            {
                Debug.LogWarning("[PatrollingAI] Invalid waypoint index!");
                return;
            }

            CurrentWaypointIndex = waypointIndex;
            IsWaiting = false;
            waitTimer = 0f;

            if (debugMode)
            {
                Debug.Log($"[PatrollingAI] Going to waypoint {waypointIndex}");
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Updates patrol movement towards the current waypoint.
        /// </summary>
        private void UpdatePatrolMovement()
        {
            Transform currentWaypoint = waypoints[CurrentWaypointIndex];
            
            if (currentWaypoint == null)
            {
                AdvanceToNextWaypoint();
                return;
            }

            // Check if waypoint is reached
            bool waypointReached = false;

            if (enemyAI.UseNavMeshAgent && enemyAI.IsNavMeshAgentAvailable)
            {
                // Use NavMeshAgent's remaining distance to check if waypoint is reached
                waypointReached = enemyAI.NavMeshAgent.remainingDistance <= waypointThreshold &&
                               enemyAI.NavMeshAgent.hasPath;
            }
            else if (enemyAI.UseNavAgentHopping && enemyAI.IsNavAgentHoppingAvailable)
            {
                // Use NavAgentHoppingController's remaining distance to check if waypoint is reached
                waypointReached = enemyAI.NavAgentHoppingController.RemainingDistance <= waypointThreshold &&
                               enemyAI.NavAgentHoppingController.HasPath;
            }
            else
            {
                // Use direct distance check
                waypointReached = Vector3.Distance(transform.position, currentWaypoint.position) <= waypointThreshold;
            }

            if (waypointReached)
            {
                // Reached waypoint
                OnWaypointReached?.Invoke(CurrentWaypointIndex);
                
                if (waitTimeAtWaypoint > 0f)
                {
                    StartWaiting();
                }
                else
                {
                    AdvanceToNextWaypoint();
                }
            }
            else
            {
                // Move towards waypoint
                enemyAI.MoveTowards(currentWaypoint.position, Time.deltaTime);
                enemyAI.FaceTarget(currentWaypoint.position, Time.deltaTime);
            }
        }

        /// <summary>
        /// Updates the wait timer.
        /// </summary>
        private void UpdateWaitTimer()
        {
            waitTimer -= Time.deltaTime;

            OnWaitingAtWaypoint?.Invoke(waitTimer);

            if (waitTimer <= 0f)
            {
                IsWaiting = false;
                AdvanceToNextWaypoint();
            }
        }

        /// <summary>
        /// Starts waiting at the current waypoint.
        /// </summary>
        private void StartWaiting()
        {
            IsWaiting = true;
            waitTimer = waitTimeAtWaypoint;
            
            if (debugMode)
            {
                Debug.Log($"[PatrollingAI] Waiting at waypoint {CurrentWaypointIndex} for {waitTimeAtWaypoint} seconds");
            }
        }

        /// <summary>
        /// Advances to the next waypoint in the patrol path.
        /// </summary>
        private void AdvanceToNextWaypoint()
        {
            if (patrolMode == PatrolMode.Loop)
            {
                CurrentWaypointIndex = (CurrentWaypointIndex + 1) % waypoints.Length;
            }
            else // PingPong
            {
                CurrentWaypointIndex += patrolDirection;

                // Reverse direction at endpoints
                if (CurrentWaypointIndex >= waypoints.Length - 1)
                {
                    patrolDirection = -1;
                }
                else if (CurrentWaypointIndex <= 0)
                {
                    patrolDirection = 1;
                }
            }

            // Reset NavMeshAgent path to force recalculation for new waypoint
            if (enemyAI.UseNavMeshAgent && enemyAI.IsNavMeshAgentAvailable)
            {
                enemyAI.NavMeshAgent.ResetPath();
            }
            // Set new destination for NavAgentHoppingController to force recalculation for new waypoint
            else if (enemyAI.UseNavAgentHopping && enemyAI.IsNavAgentHoppingAvailable)
            {
                if (debugMode)
                {
                    Debug.Log($"[PatrollingAI] AdvanceToNextWaypoint: Calling SetDestination() for waypoint {CurrentWaypointIndex}");
                }
                enemyAI.NavAgentHoppingController.SetDestination(waypoints[CurrentWaypointIndex].position);
            }

            if (debugMode)
            {
                Debug.Log($"[PatrollingAI] Advancing to waypoint {CurrentWaypointIndex}");
            }
        }

        /// <summary>
        /// Finds the index of the nearest waypoint.
        /// </summary>
        private int FindNearestWaypointIndex()
        {
            if (waypoints == null || waypoints.Length == 0)
            {
                return 0;
            }

            int nearestIndex = 0;
            float nearestDistance = float.MaxValue;

            for (int i = 0; i < waypoints.Length; i++)
            {
                if (waypoints[i] == null)
                {
                    continue;
                }

                float distance = Vector3.Distance(transform.position, waypoints[i].position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestIndex = i;
                }
            }

            return nearestIndex;
        }

        /// <summary>
        /// Handles target detected event from EnemyAI.
        /// </summary>
        private void HandleTargetDetected(Transform target)
        {
            if (stopOnTargetDetected)
            {
                StopPatrolling();
                
                if (debugMode)
                {
                    Debug.Log("[PatrollingAI] Stopped patrolling due to target detection");
                }
            }
        }

        /// <summary>
        /// Handles target lost event from EnemyAI.
        /// </summary>
        private void HandleTargetLost()
        {
            if (resumeOnTargetLost)
            {
                StartPatrolling();
                
                if (debugMode)
                {
                    Debug.Log("[PatrollingAI] Resumed patrolling after target lost");
                }
            }
        }

        #endregion

        #region Editor

        private void OnValidate()
        {
            waypointThreshold = Mathf.Max(0.1f, waypointThreshold);
            waitTimeAtWaypoint = Mathf.Max(0f, waitTimeAtWaypoint);
        }

        private void OnDrawGizmos()
        {
            if (!showGizmos || waypoints == null || waypoints.Length == 0)
            {
                return;
            }

            // Draw patrol path
            Gizmos.color = Color.magenta;

            for (int i = 0; i < waypoints.Length; i++)
            {
                if (waypoints[i] == null)
                {
                    continue;
                }

                // Draw waypoint marker
                Gizmos.color = (i == CurrentWaypointIndex && IsPatrolling) ? Color.yellow : Color.magenta;
                Gizmos.DrawWireSphere(waypoints[i].position, waypointThreshold);

                // Draw line to next waypoint
                int nextIndex = (i + 1) % waypoints.Length;
                if (waypoints[nextIndex] != null)
                {
                    Gizmos.color = Color.magenta;
                    Gizmos.DrawLine(waypoints[i].position, waypoints[nextIndex].position);
                }

                // Draw waypoint index label
#if UNITY_EDITOR
                UnityEditor.Handles.Label(waypoints[i].position + Vector3.up * 0.5f, $"Waypoint {i}");
#endif
            }

            // Draw current movement direction
            if (IsPatrolling && !IsWaiting && waypoints[CurrentWaypointIndex] != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, waypoints[CurrentWaypointIndex].position);
            }
        }

        #endregion
    }
}
