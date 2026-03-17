using UnityEngine;

namespace FindersCheesers
{
    /// <summary>
    /// States for GrabKingRatAI behavior.
    /// </summary>
    public enum GrabKingRatState
    {
        Idle,           // Waiting for king rat to be available
        Seeking,        // Moving towards king rat
        Grabbing,       // In process of grabbing king rat
        Carrying,       // Carrying king rat away from player
        Dropping        // Dropping king rat at target location
    }

    /// <summary>
    /// AI component that grabs king rat if it's not picked up,
    /// carries it away from the player, and drops it at a certain distance.
    /// </summary>
    [AddComponentMenu("Finders Cheesers/EnemyAI/GrabKingRatAI")]
    [RequireComponent(typeof(EnemyAI))]
    public class GrabKingRatAI : MonoBehaviour, IEnemyAIComponent
    {
        #region IEnemyAIComponent Implementation

        public bool IsTriggered => enemyAI != null && grabCooldownTimer <= 0f && (maxPickupCount <= 0 || pickupCount < maxPickupCount) && IsAvailableKingRatReachable();
        public bool IsRunning { get; set; }

        public event System.Action OnActivated;
        public event System.Action OnDeactivated;

        /// <summary>
        /// Called by EnemyAI when this component transitions into the running state.
        /// Enables the grab/carry state machine.
        /// </summary>
        public void OnStartRunning()
        {
            IsRunning = true;
            // The internal state machine will handle progression from Idle -> Seeking etc.
        }

        /// <summary>
        /// Called by EnemyAI when this component transitions out of the running state.
        /// Drops the king rat if carried and resets to Idle.
        /// </summary>
        public void OnExitRunning()
        {
            IsRunning = false;

            if (isCarryingKingRat)
            {
                ForceDrop();
            }
            else if (currentState != GrabKingRatState.Idle)
            {
                ChangeState(GrabKingRatState.Idle);
            }
        }

        #endregion

        #region Settings

        [Header("King Rat Detection")]
        [Tooltip("Tag that identifies King Rat GameObject")]
        [SerializeField]
        private string kingRatTag = "KingRat";

        [Tooltip("Layer mask for detecting King Rat")]
        [SerializeField]
        private LayerMask kingRatLayerMask = 1;

        [Tooltip("Detection range for finding King Rat")]
        [SerializeField]
        private float kingRatDetectionRange = 20f;

        [Header("Grab Settings")]
        [Tooltip("Distance at which enemy can grab king rat")]
        [SerializeField]
        private float grabRange = 2f;

        [Tooltip("Time required to grab king rat (in seconds)")]
        [SerializeField]
        private float grabDuration = 1f;

        [Tooltip("Offset position when carrying king rat")]
        [SerializeField]
        private Vector3 carryOffset = new Vector3(0f, 1.5f, 0f);

        [Tooltip("How smoothly king rat moves to carry position")]
        [SerializeField]
        private float carrySmoothSpeed = 10f;

        [Header("Carry Settings")]
        [Tooltip("Distance to carry king rat away from player")]
        [SerializeField]
        private float carryDistance = 15f;

        [Tooltip("Minimum distance to maintain from player while carrying")]
        [SerializeField]
        private float minDistanceFromPlayer = 10f;

        [Tooltip("Movement speed multiplier when carrying (higher = faster)")]
        [SerializeField]
        private float carrySpeedMultiplier = 1.5f;

        [Header("Drop Settings")]
        [Tooltip("Distance at which to drop king rat")]
        [SerializeField]
        private float dropDistance = 20f;

        [Tooltip("Time required to drop king rat (in seconds)")]
        [SerializeField]
        private float dropDuration = 0.5f;

        [Tooltip("Distance to throw king rat when dropping")]
        [SerializeField]
        private float throwDistance = 5f;

        [Tooltip("Whether to return to idle after dropping")]
        [SerializeField]
        private bool returnToIdleAfterDrop = true;

        [Tooltip("Cooldown time before attempting to grab again (in seconds)")]
        [SerializeField]
        private float grabCooldown = 5f;

        [Header("Player Detection")]
        [Tooltip("Tag that identifies the player")]
        [SerializeField]
        private string playerTag = "Player";

        [Tooltip("Detection range for the player")]
        [SerializeField]
        private float playerDetectionRange = 25f;

        [Header("Patrol Coordination")]
        [Tooltip("Whether to stop patrolling when seeking king rat")]
        [SerializeField]
        private bool stopPatrollingWhenSeeking = true;

        [Tooltip("Whether to resume patrolling after dropping king rat")]
        [SerializeField]
        private bool resumePatrollingAfterDrop = false;

        [Header("Pickup Limits")]
        [Tooltip("Maximum number of times the agent can pick up the king rat")]
        [SerializeField]
        private int maxPickupCount = 3;

        [Tooltip("Time after which the pickup count resets (in seconds)")]
        [SerializeField]
        private float pickupCountResetTime = 60f;

        [Header("Priority Settings")]
        [Tooltip("Priority of this AI component (higher values take precedence when multiple AI components are triggered)")]
        [SerializeField]
        private int priority = 0;

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
        /// Event fired when the AI starts seeking the king rat.
        /// </summary>
        public event System.Action OnSeekingStarted;

        /// <summary>
        /// Event fired when the AI starts grabbing the king rat.
        /// </summary>
        public event System.Action<GameObject> OnGrabbingStarted;

        /// <summary>
        /// Event fired when the AI has successfully grabbed the king rat.
        /// </summary>
        public event System.Action<GameObject> OnKingRatGrabbed;

        /// <summary>
        /// Event fired when the AI starts carrying the king rat.
        /// </summary>
        public event System.Action<GameObject> OnCarryingStarted;

        /// <summary>
        /// Event fired when the AI starts dropping the king rat.
        /// </summary>
        public event System.Action<GameObject> OnDroppingStarted;

        /// <summary>
        /// Event fired when the AI has dropped the king rat.
        /// </summary>
        public event System.Action<GameObject, Vector3> OnKingRatDropped;

        /// <summary>
        /// Event fired when the AI state changes.
        /// </summary>
        public event System.Action<GrabKingRatState, GrabKingRatState> OnStateChanged;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the current state of the AI.
        /// </summary>
        public GrabKingRatState CurrentState => currentState;

        /// <summary>
        /// Gets the king rat GameObject (null if not carrying).
        /// </summary>
        public GameObject CarriedKingRat => isCarryingKingRat ? kingRat : null;

        /// <summary>
        /// Gets whether the AI is currently carrying the king rat.
        /// </summary>
        public bool IsCarryingKingRat => isCarryingKingRat;

        /// <summary>
        /// Gets whether the AI is currently grabbing the king rat.
        /// </summary>
        public bool IsGrabbing => currentState == GrabKingRatState.Grabbing;

        /// <summary>
        /// Gets the remaining grab cooldown time.
        /// </summary>
        public float RemainingGrabCooldown => Mathf.Max(0f, grabCooldownTimer);

        /// <summary>
        /// Gets the current pickup count.
        /// </summary>
        public int PickupCount => pickupCount;

        /// <summary>
        /// Gets the remaining pickup count reset time.
        /// </summary>
        public float RemainingPickupResetTime => Mathf.Max(0f, pickupCountResetTimer);

        /// <summary>
        /// Gets the priority of this AI component.
        /// Higher priority values take precedence when multiple AI components are triggered.
        /// </summary>
        public int Priority => priority;

        #endregion

        #region Component References

        private EnemyAI enemyAI;
        private PatrollingAI patrollingAI;

        #endregion

        #region State Variables

        private GrabKingRatState currentState = GrabKingRatState.Idle;
        private GameObject kingRat;
        private Transform playerTransform;
        private IThrowable kingRatThrowable;
        private Rigidbody kingRatRigidbody;
        private bool kingRatWasKinematic;
        private bool isCarryingKingRat = false;
        private float grabTimer = 0f;
        private float dropTimer = 0f;
        private float grabCooldownTimer = 0f;
        private Vector3 dropTargetPosition;
        private int pickupCount = 0;
        private float pickupCountResetTimer = 0f;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            enemyAI = GetComponent<EnemyAI>();

            if (enemyAI == null)
            {
                Debug.LogError("[GrabKingRatAI] EnemyAI component not found!");
                return;
            }

            // Get PatrollingAI component if available
            patrollingAI = GetComponent<PatrollingAI>();

            // Note: GrabKingRatAI implements its own king rat detection logic
            // and does not rely on EnemyAI's target detection system
        }

        private void Start()
        {
            FindPlayer();
        }

        private void Update()
        {
            if (!enemyAI.IsActive)
            {
                return;
            }

            // Timers always tick so cooldowns drain even when not running
            UpdateGrabCooldown();
            UpdatePickupCountResetTimer();

            if (!IsRunning)
            {
                return;
            }

            UpdateStateMachine();
        }

        private void FixedUpdate()
        {
            if (!enemyAI.IsActive)
            {
                return;
            }

            UpdateCarryPosition();
        }

        private void OnDestroy()
        {
            // Drop king rat if carrying when destroyed
            if (isCarryingKingRat && kingRat != null)
            {
                DropKingRat();
            }
        }

        #endregion

        #region State Machine

        /// <summary>
        /// Updates the state machine based on current conditions.
        /// </summary>
        private void UpdateStateMachine()
        {
            GrabKingRatState previousState = currentState;

            switch (currentState)
            {
                case GrabKingRatState.Idle:
                    UpdateIdleState();
                    break;

                case GrabKingRatState.Seeking:
                    UpdateSeekingState();
                    break;

                case GrabKingRatState.Grabbing:
                    UpdateGrabbingState();
                    break;

                case GrabKingRatState.Carrying:
                    UpdateCarryingState();
                    break;

                case GrabKingRatState.Dropping:
                    UpdateDroppingState();
                    break;
            }

            // Fire state changed event if state changed
            if (currentState != previousState)
            {
                OnStateChanged?.Invoke(previousState, currentState);

                if (debugMode)
                {
                    Debug.Log($"[GrabKingRatAI] State changed: {previousState} -> {currentState}");
                }
            }
        }

        /// <summary>
        /// Updates the idle state - looks for available king rat.
        /// </summary>
        private void UpdateIdleState()
        {
            // Check if we're on cooldown
            if (grabCooldownTimer > 0f)
            {
                return;
            }

            // Check if we've reached max pickup count
            if (maxPickupCount > 0 && pickupCount >= maxPickupCount)
            {
                if (debugMode)
                {
                    Debug.Log($"[GrabKingRatAI] Max pickup count ({maxPickupCount}) reached. Waiting for reset.");
                }
                return;
            }

            // Find available king rat that is also reachable
            if (IsAvailableKingRatReachable())
            {
                GameObject availableKingRat = FindAvailableKingRat();
                
                if (availableKingRat != null)
                {
                    kingRat = availableKingRat;
                    kingRatThrowable = kingRat.GetComponent<IThrowable>();
                    kingRatRigidbody = kingRat.GetComponent<Rigidbody>();

                    ChangeState(GrabKingRatState.Seeking);
                    OnSeekingStarted?.Invoke();

                    // Stop patrolling if configured
                    if (stopPatrollingWhenSeeking && patrollingAI != null && patrollingAI.IsPatrolling)
                    {
                        patrollingAI.StopPatrolling();

                        if (debugMode)
                        {
                            Debug.Log("[GrabKingRatAI] Stopped patrolling to seek king rat");
                        }
                    }

                    if (debugMode)
                    {
                        Debug.Log($"[GrabKingRatAI] Found available and reachable king rat: {kingRat.name} (Pickup {pickupCount + 1}/{maxPickupCount})");
                    }
                }
            }
        }

        /// <summary>
        /// Updates the seeking state - moves towards king rat.
        /// </summary>
        private void UpdateSeekingState()
        {
            if (kingRat == null)
            {
                ChangeState(GrabKingRatState.Idle);
                return;
            }

            // Check if king rat is no longer available (picked up by player)
            if (!IsKingRatAvailable(kingRat))
            {
                if (debugMode)
                {
                    Debug.Log("[GrabKingRatAI] King rat is no longer available");
                }
                ChangeState(GrabKingRatState.Idle);

                // Resume patrolling if configured
                if (resumePatrollingAfterDrop && patrollingAI != null && !patrollingAI.IsPatrolling)
                {
                    patrollingAI.StartPatrolling();

                    if (debugMode)
                    {
                        Debug.Log("[GrabKingRatAI] Resumed patrolling (king rat unavailable)");
                    }
                }
                return;
            }

            // Check if king rat is reachable via navigation
            if (!IsTargetPositionValid(kingRat.transform.position))
            {
                if (debugMode)
                {
                    Debug.LogWarning($"[GrabKingRatAI] King rat at {kingRat.transform.position} is unreachable. Aborting seek.");
                }
                ChangeState(GrabKingRatState.Idle);

                // Resume patrolling if configured
                if (resumePatrollingAfterDrop && patrollingAI != null && !patrollingAI.IsPatrolling)
                {
                    patrollingAI.StartPatrolling();

                    if (debugMode)
                    {
                        Debug.Log("[GrabKingRatAI] Resumed patrolling (king rat unreachable)");
                    }
                }
                return;
            }

            float distanceToKingRat = Vector3.Distance(transform.position, kingRat.transform.position);

            // Check if we're close enough to grab
            if (distanceToKingRat <= grabRange)
            {
                ChangeState(GrabKingRatState.Grabbing);
                grabTimer = 0f;
                OnGrabbingStarted?.Invoke(kingRat);

                if (debugMode)
                {
                    Debug.Log("[GrabKingRatAI] Starting to grab king rat");
                }
            }
            else
            {
                // Move towards king rat
                enemyAI.MoveTowards(kingRat.transform.position, Time.deltaTime);
                enemyAI.FaceTarget(kingRat.transform.position, Time.deltaTime);
            }
        }

        /// <summary>
        /// Updates the grabbing state - performs grab action.
        /// </summary>
        private void UpdateGrabbingState()
        {
            if (kingRat == null)
            {
                ChangeState(GrabKingRatState.Idle);
                return;
            }

            // Check if king rat is no longer available
            if (!IsKingRatAvailable(kingRat))
            {
                if (debugMode)
                {
                    Debug.Log("[GrabKingRatAI] King rat became unavailable during grab");
                }
                ChangeState(GrabKingRatState.Idle);

                // Resume patrolling if configured
                if (resumePatrollingAfterDrop && patrollingAI != null && !patrollingAI.IsPatrolling)
                {
                    patrollingAI.StartPatrolling();

                    if (debugMode)
                    {
                        Debug.Log("[GrabKingRatAI] Resumed patrolling (king rat unavailable during grab)");
                    }
                }
                return;
            }

            grabTimer += Time.deltaTime;

            // Check if grab is complete
            if (grabTimer >= grabDuration)
            {
                if (PerformGrab())
                {
                    ChangeState(GrabKingRatState.Carrying);
                    OnCarryingStarted?.Invoke(kingRat);

                    if (debugMode)
                    {
                        Debug.Log("[GrabKingRatAI] Successfully grabbed king rat");
                    }
                }
                else
                {
                    // Grab failed, return to idle with cooldown
                    grabCooldownTimer = grabCooldown;
                    ChangeState(GrabKingRatState.Idle);

                    // Resume patrolling if configured
                    if (resumePatrollingAfterDrop && patrollingAI != null && !patrollingAI.IsPatrolling)
                    {
                        patrollingAI.StartPatrolling();

                        if (debugMode)
                        {
                            Debug.Log("[GrabKingRatAI] Resumed patrolling (grab failed)");
                        }
                    }

                    if (debugMode)
                    {
                        Debug.LogWarning("[GrabKingRatAI] Failed to grab king rat");
                    }
                }
            }
            else
            {
                // Face the king rat while grabbing
                enemyAI.FaceTarget(kingRat.transform.position, Time.deltaTime);
            }
        }

        /// <summary>
        /// Updates the carrying state - moves away from player with king rat.
        /// </summary>
        private void UpdateCarryingState()
        {
            if (kingRat == null)
            {
                ChangeState(GrabKingRatState.Idle);
                return;
            }

            // Calculate direction away from player
            Vector3 directionAwayFromPlayer = CalculateDirectionAwayFromPlayer();

            if (directionAwayFromPlayer == Vector3.zero)
            {
                // No player detected, just move forward
                directionAwayFromPlayer = transform.forward;
            }

            // Calculate target position
            Vector3 targetPosition = transform.position + directionAwayFromPlayer * carryDistance;

            // Check distance to player
            float distanceToPlayer = playerTransform != null
                ? Vector3.Distance(transform.position, playerTransform.position)
                : float.MaxValue;

            // Check if we should drop the king rat
            if (distanceToPlayer >= dropDistance)
            {
                ChangeState(GrabKingRatState.Dropping);
                dropTimer = 0f;
                dropTargetPosition = transform.position;
                OnDroppingStarted?.Invoke(kingRat);

                if (debugMode)
                {
                    Debug.Log("[GrabKingRatAI] Starting to drop king rat");
                }
            }
            else
            {
                // Only move if target position is valid (prevents NavAgentHoppingController warnings)
                if (IsTargetPositionValid(targetPosition))
                {
                    // Move away from player
                    enemyAI.MoveTowards(targetPosition, Time.deltaTime);
                }
                enemyAI.FaceTarget(targetPosition, Time.deltaTime);
            }
        }

        /// <summary>
        /// Updates the dropping state - performs drop action.
        /// </summary>
        private void UpdateDroppingState()
        {
            if (kingRat == null)
            {
                ChangeState(GrabKingRatState.Idle);
                return;
            }

            dropTimer += Time.deltaTime;

            // Check if drop is complete
            if (dropTimer >= dropDuration)
            {
                Vector3 dropPosition = kingRat.transform.position;
                DropKingRat();
                OnKingRatDropped?.Invoke(kingRat, dropPosition);

                if (debugMode)
                {
                    Debug.Log($"[GrabKingRatAI] Dropped king rat at {dropPosition}");
                }

                // Set cooldown and return to idle
                grabCooldownTimer = grabCooldown;

                if (returnToIdleAfterDrop)
                {
                    ChangeState(GrabKingRatState.Idle);

                    // Resume patrolling if configured
                    if (resumePatrollingAfterDrop && patrollingAI != null && !patrollingAI.IsPatrolling)
                    {
                        patrollingAI.StartPatrolling();

                        if (debugMode)
                        {
                            Debug.Log("[GrabKingRatAI] Resumed patrolling after dropping king rat");
                        }
                    }
                }
                else
                {
                    // Stay in dropping state (can be used for custom behavior)
                }
            }
        }

        #endregion

        #region Grab and Carry Methods

        /// <summary>
        /// Performs a grab action on the king rat.
        /// </summary>
        /// <returns>True if grab was successful, false otherwise.</returns>
        private bool PerformGrab()
        {
            if (kingRat == null)
            {
                return false;
            }

            // Store original kinematic state
            if (kingRatRigidbody != null)
            {
                kingRatWasKinematic = kingRatRigidbody.isKinematic;
                kingRatRigidbody.isKinematic = true;
            }

            isCarryingKingRat = true;

            // Apply carry speed multiplier
            float originalSpeed = enemyAI.MoveSpeed;
            enemyAI.MoveSpeed = originalSpeed * carrySpeedMultiplier;

            // Increment pickup count
            pickupCount++;

            if (debugMode)
            {
                Debug.Log($"[GrabKingRatAI] King rat grabbed! Pickup count: {pickupCount}/{maxPickupCount}");
            }

            OnKingRatGrabbed?.Invoke(kingRat);

            return true;
        }

        /// <summary>
        /// Updates the king rat's position when being carried.
        /// </summary>
        private void UpdateCarryPosition()
        {
            if (!isCarryingKingRat || kingRat == null)
            {
                return;
            }

            Vector3 targetPosition = transform.position + carryOffset;
            kingRat.transform.position = Vector3.Lerp(
                kingRat.transform.position,
                targetPosition,
                carrySmoothSpeed * Time.fixedDeltaTime
            );
        }

        /// <summary>
        /// Drops the king rat with a throw distance.
        /// </summary>
        private void DropKingRat()
        {
            if (!isCarryingKingRat || kingRat == null)
            {
                return;
            }

            // Restore original kinematic state
            if (kingRatRigidbody != null)
            {
                kingRatRigidbody.isKinematic = false;
            }

            // Use IThrowable if available
            if (kingRatThrowable != null)
            {
                kingRatThrowable.Drop();
            }

            // Throw the king rat with calculated velocity
            if (kingRatRigidbody != null && throwDistance > 0f)
            {
                Vector3 throwDirection = CalculateThrowDirection();
                Vector3 throwVelocity = CalculateThrowVelocity(throwDirection, throwDistance);
                kingRatRigidbody.linearVelocity = throwVelocity;

                if (debugMode)
                {
                    Debug.Log($"[GrabKingRatAI] Threw king rat with velocity: {throwVelocity} (distance: {throwDistance}m)");
                }
            }

            // Reset speed multiplier
            float currentSpeed = enemyAI.MoveSpeed;
            enemyAI.MoveSpeed = currentSpeed / carrySpeedMultiplier;

            isCarryingKingRat = false;

            // Clear references
            kingRat = null;
            kingRatThrowable = null;
            kingRatRigidbody = null;
        }

        #endregion

        #region Detection Methods

        /// <summary>
        /// Checks if there is an available king rat that is reachable via navigation.
        /// </summary>
        /// <returns>True if an available and reachable king rat exists, false otherwise.</returns>
        private bool IsAvailableKingRatReachable()
        {
            GameObject availableKingRat = FindAvailableKingRat();
            
            if (availableKingRat == null)
            {
                return false;
            }
            
            // Check if the available king rat is reachable via navigation
            return IsTargetPositionValid(availableKingRat.transform.position);
        }

        /// <summary>
        /// Finds an available king rat that is not being carried.
        /// </summary>
        /// <returns>The available king rat GameObject, or null if none found.</returns>
        private GameObject FindAvailableKingRat()
        {
            // Find all king rats in detection range
            Collider[] colliders = Physics.OverlapSphere(
                transform.position,
                kingRatDetectionRange,
                kingRatLayerMask,
                QueryTriggerInteraction.Ignore
            );

            GameObject bestCandidate = null;
            float closestDistance = float.MaxValue;

            foreach (Collider collider in colliders)
            {
                if (collider == null || collider.transform == transform)
                {
                    continue;
                }

                // Check if it's a king rat
                if (!string.IsNullOrEmpty(kingRatTag) && !collider.gameObject.CompareTag(kingRatTag))
                {
                    continue;
                }

                // Check if king rat is available (not being carried)
                if (!IsKingRatAvailable(collider.gameObject))
                {
                    continue;
                }

                // Find the closest available king rat
                float distance = Vector3.Distance(transform.position, collider.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    bestCandidate = collider.gameObject;
                }
            }

            return bestCandidate;
        }

        /// <summary>
        /// Checks if a king rat is available for grabbing (not being carried by player).
        /// </summary>
        /// <param name="kingRatObject">The king rat GameObject to check.</param>
        /// <returns>True if available, false otherwise.</returns>
        private bool IsKingRatAvailable(GameObject kingRatObject)
        {
            if (kingRatObject == null)
            {
                return false;
            }

            // Check if the king rat has a parent (might be being carried)
            if (kingRatObject.transform.parent != null)
            {
                // Check if parent is the player or a player component
                Transform parent = kingRatObject.transform.parent;
                while (parent != null)
                {
                    if (!string.IsNullOrEmpty(playerTag) && parent.CompareTag(playerTag))
                    {
                        return false;
                    }

                    // Check for KingRatHandler component (player's grab handler)
                    if (parent.GetComponent<KingRatHandler>() != null)
                    {
                        return false;
                    }

                    parent = parent.parent;
                }
            }

            return true;
        }

        /// <summary>
        /// Finds the player transform.
        /// </summary>
        private void FindPlayer()
        {
            if (string.IsNullOrEmpty(playerTag))
            {
                return;
            }

            GameObject player = GameObject.FindGameObjectWithTag(playerTag);
            if (player != null)
            {
                playerTransform = player.transform;

                if (debugMode)
                {
                    Debug.Log($"[GrabKingRatAI] Found player: {player.name}");
                }
            }
            else if (debugMode)
            {
                Debug.LogWarning("[GrabKingRatAI] Player not found with tag: " + playerTag);
            }
        }

        /// <summary>
        /// Calculates the direction to move away from the player.
        /// </summary>
        /// <returns>The direction vector away from the player.</returns>
        private Vector3 CalculateDirectionAwayFromPlayer()
        {
            if (playerTransform == null)
            {
                return Vector3.zero;
            }

            Vector3 directionFromPlayer = transform.position - playerTransform.position;
            directionFromPlayer.y = 0f; // Keep on horizontal plane

            if (directionFromPlayer == Vector3.zero)
            {
                // We're at the same position as player, move in a random direction
                return Random.insideUnitSphere.normalized;
            }

            return directionFromPlayer.normalized;
        }

        /// <summary>
        /// Calculates the direction to throw the king rat.
        /// </summary>
        /// <returns>The throw direction vector.</returns>
        private Vector3 CalculateThrowDirection()
        {
            // Throw in the direction away from the player
            Vector3 directionAwayFromPlayer = CalculateDirectionAwayFromPlayer();

            if (directionAwayFromPlayer == Vector3.zero)
            {
                // No player detected, throw in a random forward direction
                Vector3 randomDirection = Random.insideUnitSphere.normalized;
                randomDirection.y = 0f;
                return randomDirection;
            }

            // Add some upward arc to the throw
            return directionAwayFromPlayer + Vector3.up * 0.5f;
        }

        /// <summary>
        /// Calculates the throw velocity to achieve the desired throw distance.
        /// </summary>
        /// <param name="direction">The direction to throw in.</param>
        /// <param name="distance">The desired throw distance.</param>
        /// <returns>The velocity vector for the throw.</returns>
        private Vector3 CalculateThrowVelocity(Vector3 direction, float distance)
        {
            // Normalize the direction
            Vector3 normalizedDirection = direction.normalized;

            // Calculate horizontal and vertical components
            Vector3 horizontalDirection = normalizedDirection;
            horizontalDirection.y = 0f;
            horizontalDirection = horizontalDirection.normalized;

            float verticalComponent = normalizedDirection.y;

            // Calculate required velocity using projectile motion formula
            // For a given distance and angle, we can calculate the required velocity
            // Using a simplified approach: velocity = sqrt(distance * gravity)
            float gravity = Physics.gravity.magnitude;
            float horizontalVelocity = Mathf.Sqrt(distance * gravity);

            // Apply a multiplier to account for air resistance and other factors
            horizontalVelocity *= 1.2f;

            // Calculate vertical velocity for an arc
            float verticalVelocity = horizontalVelocity * 0.5f;

            // Combine horizontal and vertical components
            Vector3 throwVelocity = (horizontalDirection * horizontalVelocity) + (Vector3.up * verticalVelocity);

            return throwVelocity;
        }

        /// <summary>
        /// Checks if a target position is valid for navigation.
        /// </summary>
        /// <param name="targetPosition">The target position to validate.</param>
        /// <returns>True if the position is valid, false otherwise.</returns>
        private bool IsTargetPositionValid(Vector3 targetPosition)
        {
            // If using NavMeshAgent, check if path is valid
            if (enemyAI.UseNavMeshAgent && enemyAI.IsNavMeshAgentAvailable)
            {
                UnityEngine.AI.NavMeshPath path = new UnityEngine.AI.NavMeshPath();
                if (enemyAI.NavMeshAgent.CalculatePath(targetPosition, path))
                {
                    return path.status == UnityEngine.AI.NavMeshPathStatus.PathComplete;
                }
                return false;
            }
            // If using NavAgentHoppingController, check if destination is reachable
            else if (enemyAI.UseNavAgentHopping && enemyAI.IsNavAgentHoppingAvailable)
            {
                // First check if the target position is on the NavMesh
                UnityEngine.AI.NavMeshHit navMeshHit;
                if (UnityEngine.AI.NavMesh.SamplePosition(targetPosition, out navMeshHit, 1.0f, UnityEngine.AI.NavMesh.AllAreas))
                {
                    // Position is on NavMesh, try to calculate a path
                    // Temporarily set destination to check path validity
                    Vector3 originalDestination = enemyAI.NavAgentHoppingController.Destination;
                    enemyAI.NavAgentHoppingController.SetDestination(targetPosition);
                    bool pathValid = enemyAI.NavAgentHoppingController.RecalculatePath();
                    
                    // Restore original destination if path is invalid
                    if (!pathValid)
                    {
                        enemyAI.NavAgentHoppingController.SetDestination(originalDestination);
                    }
                    
                    return pathValid;
                }
                return false;
            }

            // For direct movement, assume position is valid
            return true;
        }

        /// <summary>
        /// Updates the pickup count reset timer.
        /// </summary>
        private void UpdatePickupCountResetTimer()
        {
            if (pickupCountResetTimer > 0f)
            {
                pickupCountResetTimer -= Time.deltaTime;
            }
            else
            {
                // Reset pickup count
                pickupCount = 0;
                pickupCountResetTimer = pickupCountResetTime;

                if (debugMode)
                {
                    Debug.Log($"[GrabKingRatAI] Pickup count reset to 0. Next reset in {pickupCountResetTime:F1} seconds.");
                }
            }
        }

        /// <summary>
        /// Updates the grab cooldown timer.
        /// </summary>
        private void UpdateGrabCooldown()
        {
            if (grabCooldownTimer > 0f)
            {
                grabCooldownTimer -= Time.deltaTime;
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Changes the current state.
        /// </summary>
        /// <param name="newState">The new state to transition to.</param>
        private void ChangeState(GrabKingRatState newState)
        {
            // Invoke OnActivated when transitioning from Idle to any active state
            if (currentState == GrabKingRatState.Idle && newState != GrabKingRatState.Idle)
            {
                OnActivated?.Invoke();
            }
            
            // Invoke OnDeactivated when transitioning to Idle from an active state
            if (currentState != GrabKingRatState.Idle && newState == GrabKingRatState.Idle)
            {
                OnDeactivated?.Invoke();
            }
            
            currentState = newState;
        }

        #endregion

        #region Public API

        /// <summary>
        /// Forces the AI to start seeking the king rat immediately.
        /// </summary>
        /// <param name="kingRatObject">The king rat to seek.</param>
        public void StartSeeking(GameObject kingRatObject)
        {
            if (kingRatObject == null)
            {
                Debug.LogWarning("[GrabKingRatAI] Cannot seek null king rat!");
                return;
            }

            kingRat = kingRatObject;
            kingRatThrowable = kingRat.GetComponent<IThrowable>();
            kingRatRigidbody = kingRat.GetComponent<Rigidbody>();

            ChangeState(GrabKingRatState.Seeking);
            OnSeekingStarted?.Invoke();

            if (debugMode)
            {
                Debug.Log($"[GrabKingRatAI] Forced to start seeking: {kingRat.name}");
            }
        }

        /// <summary>
        /// Forces the AI to drop the king rat immediately.
        /// </summary>
        public void ForceDrop()
        {
            if (!isCarryingKingRat)
            {
                return;
            }

            Vector3 dropPosition = kingRat != null ? kingRat.transform.position : transform.position;
            DropKingRat();
            OnKingRatDropped?.Invoke(kingRat, dropPosition);

            grabCooldownTimer = grabCooldown;
            ChangeState(GrabKingRatState.Idle);

            if (debugMode)
            {
                Debug.Log("[GrabKingRatAI] Forced to drop king rat");
            }
        }

        /// <summary>
        /// Resets the grab cooldown.
        /// </summary>
        public void ResetGrabCooldown()
        {
            grabCooldownTimer = 0f;

            if (debugMode)
            {
                Debug.Log("[GrabKingRatAI] Grab cooldown reset");
            }
        }

        /// <summary>
        /// Sets the grab cooldown time.
        /// </summary>
        /// <param name="cooldown">The cooldown time in seconds.</param>
        public void SetGrabCooldown(float cooldown)
        {
            grabCooldown = Mathf.Max(0f, cooldown);
        }

        /// <summary>
        /// Sets the carry distance.
        /// </summary>
        /// <param name="distance">The carry distance.</param>
        public void SetCarryDistance(float distance)
        {
            carryDistance = Mathf.Max(1f, distance);
        }

        /// <summary>
        /// Sets the drop distance.
        /// </summary>
        /// <param name="distance">The drop distance.</param>
        public void SetDropDistance(float distance)
        {
            dropDistance = Mathf.Max(1f, distance);
        }

        /// <summary>
        /// Sets the throw distance.
        /// </summary>
        /// <param name="distance">The throw distance.</param>
        public void SetThrowDistance(float distance)
        {
            throwDistance = Mathf.Max(0f, distance);
        }

        /// <summary>
        /// Resets the pickup count immediately.
        /// </summary>
        public void ResetPickupCount()
        {
            pickupCount = 0;
            pickupCountResetTimer = pickupCountResetTime;

            if (debugMode)
            {
                Debug.Log("[GrabKingRatAI] Pickup count reset to 0.");
            }
        }

        #endregion

        #region Editor

        private void OnValidate()
        {
            kingRatDetectionRange = Mathf.Max(1f, kingRatDetectionRange);
            grabRange = Mathf.Max(0.1f, grabRange);
            grabDuration = Mathf.Max(0.1f, grabDuration);
            carrySmoothSpeed = Mathf.Max(0.1f, carrySmoothSpeed);
            carryDistance = Mathf.Max(1f, carryDistance);
            minDistanceFromPlayer = Mathf.Max(0f, minDistanceFromPlayer);
            carrySpeedMultiplier = Mathf.Max(0.1f, carrySpeedMultiplier);
            dropDistance = Mathf.Max(1f, dropDistance);
            dropDuration = Mathf.Max(0.1f, dropDuration);
            throwDistance = Mathf.Max(0f, throwDistance);
            grabCooldown = Mathf.Max(0f, grabCooldown);
            playerDetectionRange = Mathf.Max(1f, playerDetectionRange);
            maxPickupCount = Mathf.Max(0, maxPickupCount);
            pickupCountResetTime = Mathf.Max(0f, pickupCountResetTime);
        }

        #endregion

        #region Editor

        private void OnDrawGizmos()
        {
            if (!showGizmos)
            {
                return;
            }

            // Draw king rat detection range
            Gizmos.color = currentState == GrabKingRatState.Seeking ? Color.yellow : Color.cyan;
            Gizmos.DrawWireSphere(transform.position, kingRatDetectionRange);

            // Draw grab range
            Gizmos.color = currentState == GrabKingRatState.Grabbing ? Color.orange : Color.green;
            Gizmos.DrawWireSphere(transform.position, grabRange);

            // Draw carry position
            if (isCarryingKingRat)
            {
                Gizmos.color = Color.magenta;
                Vector3 carryPosition = transform.position + carryOffset;
                Gizmos.DrawWireSphere(carryPosition, 0.3f);
                Gizmos.DrawLine(transform.position, carryPosition);

                if (kingRat != null)
                {
                    Gizmos.DrawLine(carryPosition, kingRat.transform.position);
                }
            }

            // Draw drop distance circle
            if (playerTransform != null)
            {
                Gizmos.color = currentState == GrabKingRatState.Carrying ? Color.red : Color.blue;
                float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
                Gizmos.DrawWireSphere(transform.position, dropDistance);

                // Draw line to player
                Gizmos.color = Color.gray;
                Gizmos.DrawLine(transform.position, playerTransform.position);
            }

            // Draw direction away from player
            if (currentState == GrabKingRatState.Carrying && playerTransform != null)
            {
                Gizmos.color = Color.red;
                Vector3 directionAway = CalculateDirectionAwayFromPlayer();
                Gizmos.DrawRay(transform.position, directionAway * 5f);
            }

            // Draw state indicator
            Gizmos.color = GetStateColor(currentState);
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 2f, 0.5f);
        }

        private Color GetStateColor(GrabKingRatState state)
        {
            switch (state)
            {
                case GrabKingRatState.Idle:
                    return Color.gray;
                case GrabKingRatState.Seeking:
                    return Color.yellow;
                case GrabKingRatState.Grabbing:
                    return Color.orange;
                case GrabKingRatState.Carrying:
                    return Color.magenta;
                case GrabKingRatState.Dropping:
                    return Color.red;
                default:
                    return Color.white;
            }
        }

        #endregion
    }
}
