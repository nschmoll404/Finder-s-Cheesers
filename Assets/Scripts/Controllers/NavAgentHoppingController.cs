using UnityEngine;
using UnityEngine.AI;

namespace FindersCheesers
{
    /// <summary>
    /// A controller that uses navmesh pathfinding to navigate, but uses rigidbody physics to hop for movement.
    /// Combines NavMeshAgent path calculation with Rigidbody-based hopping physics.
    /// </summary>
    [AddComponentMenu("Finders Cheesers/NavAgent Hopping Controller")]
    [RequireComponent(typeof(Rigidbody))]
    public class NavAgentHoppingController : MonoBehaviour
    {
        #region Settings

        [Header("NavMesh Settings")]
        [Tooltip("The NavMesh area mask for pathfinding")]
        [SerializeField]
        private int navMeshAreaMask = NavMesh.AllAreas;

        [Tooltip("How often to recalculate path (in seconds)")]
        [SerializeField]
        private float pathRecalculationInterval = 0.5f;

        [Tooltip("The distance threshold to consider a path point reached")]
        [SerializeField]
        private float pathPointThreshold = 0.5f;

        [Header("Hopping Physics")]
        [Tooltip("The upward force applied when hopping")]
        [SerializeField]
        private float hopForce = 8f;

        [Tooltip("The horizontal force applied when hopping towards target")]
        [SerializeField]
        private float hopHorizontalForce = 5f;

        [Tooltip("Maximum horizontal speed")]
        [SerializeField]
        private float maxHorizontalSpeed = 5f;

        [Tooltip("How quickly to rotate to face hop direction")]
        [SerializeField]
        private float rotationSpeed = 10f;

        [Tooltip("Ground check distance to determine if grounded")]
        [SerializeField]
        private float groundCheckDistance = 0.1f;

        [Tooltip("Layer mask for ground detection")]
        [SerializeField]
        private LayerMask groundLayerMask = 1;

        [Tooltip("Whether to use gravity")]
        [SerializeField]
        private bool useGravity = true;

        [Tooltip("Drag applied when in the air")]
        [SerializeField]
        private float airDrag = 0.1f;

        [Tooltip("Drag applied when grounded")]
        [SerializeField]
        private float groundDrag = 5f;

        [Tooltip("Minimum time between hops (in seconds). Set to 0 to allow continuous hopping.")]
        [SerializeField]
        private float hopDelay = 0.2f;

        [Tooltip("When enabled, calculates exact jump velocity to land on target when close to destination")]
        [SerializeField]
        private bool usePreciseJumping = true;

        [Tooltip("Distance threshold for using precise jumping (in meters)")]
        [SerializeField]
        private float preciseJumpDistance = 3f;

        [Header("Movement Settings")]
        [Tooltip("Whether to start moving immediately")]
        [SerializeField]
        private bool autoStart = false;

        [Tooltip("Whether to stop when destination is reached")]
        [SerializeField]
        private bool stopAtDestination = true;

        [Tooltip("The stopping distance from the destination")]
        [SerializeField]
        private float stoppingDistance = 0.5f;

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
        /// Event fired when movement starts.
        /// </summary>
        public event System.Action OnMovementStarted;

        /// <summary>
        /// Event fired when movement stops.
        /// </summary>
        public event System.Action OnMovementStopped;

        /// <summary>
        /// Event fired when destination is reached.
        /// </summary>
        public event System.Action OnDestinationReached;

        /// <summary>
        /// Event fired when a hop is performed.
        /// </summary>
        public event System.Action<Vector3> OnHopPerformed;

        /// <summary>
        /// Event fired when the path is recalculated.
        /// </summary>
        public event System.Action OnPathRecalculated;

        #endregion

        #region Component References

        private Rigidbody _rigidbody;

        #endregion

        #region Properties

        /// <summary>
        /// Gets whether the controller is currently moving.
        /// </summary>
        public bool IsMoving { get; private set; }

        /// <summary>
        /// Gets whether the controller is currently grounded.
        /// </summary>
        public bool IsGrounded { get; private set; }

        /// <summary>
        /// Gets whether the controller is currently in the air (hopping).
        /// </summary>
        public bool IsHopping => !IsGrounded;

        /// <summary>
        /// Gets the current destination.
        /// </summary>
        public Vector3 Destination { get; private set; }

        /// <summary>
        /// Gets whether a path is currently available.
        /// </summary>
        public bool HasPath { get; private set; }

        /// <summary>
        /// Gets the remaining distance to the destination.
        /// </summary>
        public float RemainingDistance { get; private set; }

        /// <summary>
        /// Gets the current velocity.
        /// </summary>
        public Vector3 Velocity => _rigidbody != null ? _rigidbody.linearVelocity : Vector3.zero;

        #endregion

        #region State Variables

        private NavMeshPath navMeshPath;
        private int currentPathIndex;
        private float pathRecalculationTimer;
        private float hopCooldownTimer;
        private bool hasDestination;
        private bool destinationReached;
        private bool needsPathRecalculation = false;
        private Vector3 targetDirection;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Cache Rigidbody component
            _rigidbody = GetComponent<Rigidbody>();

            if (_rigidbody == null)
            {
                Debug.LogError("[NavAgentHoppingController] Rigidbody component not found on this GameObject!");
                return;
            }

            // Initialize NavMeshPath
            navMeshPath = new NavMeshPath();
        }

        private void Start()
        {
            // Configure Rigidbody
            if (_rigidbody != null)
            {
                _rigidbody.useGravity = useGravity;
                _rigidbody.linearDamping = groundDrag;
                _rigidbody.angularDamping = 10f;
                _rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            }

            // Auto-start if enabled
            if (autoStart && hasDestination)
            {
                StartMoving();
            }
        }

        private void Update()
        {
            // Check if grounded
            CheckGrounded();

            // Handle path recalculation - only when grounded to avoid navmesh warnings
            if (IsMoving && hasDestination)
            {
                if (IsGrounded)
                {
                    pathRecalculationTimer += Time.deltaTime;

                    if (pathRecalculationTimer >= pathRecalculationInterval)
                    {
                        pathRecalculationTimer = 0f;
                        RecalculatePath();
                    }
                }
                else if (needsPathRecalculation)
                {
                    // We became grounded and need to recalculate path
                    needsPathRecalculation = false;
                    RecalculatePath();
                }
            }
        }

        private void FixedUpdate()
        {
            if (_rigidbody == null)
            {
                return;
            }

            // Update drag based on grounded state
            _rigidbody.linearDamping = IsGrounded ? groundDrag : airDrag;

            // Update hop cooldown timer
            if (hopCooldownTimer > 0f)
            {
                hopCooldownTimer -= Time.fixedDeltaTime;
            }

            // Rotate towards target direction continuously (smooth rotation even in air)
            if (IsMoving && !destinationReached && targetDirection != Vector3.zero)
            {
                RotateTowards(targetDirection);
            }

            // Handle movement
            if (IsMoving && !destinationReached)
            {
                HandleMovement();
            }
            else if (debugMode && IsMoving && destinationReached)
            {
                // DEBUG: Log why movement is not being handled
                Debug.Log($"[NavAgentHoppingController] FixedUpdate: Skipping HandleMovement() - destinationReached is true");
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Sets the destination and starts moving towards it.
        /// </summary>
        /// <param name="destination">The destination position.</param>
        /// <returns>True if a valid path was found, false otherwise.</returns>
        public bool SetDestination(Vector3 destination)
        {
            // Reset destination reached flag to allow new movement
            Destination = destination;
            hasDestination = true;
            destinationReached = false;

            // Only calculate path if grounded to avoid navmesh warnings
            if (!IsGrounded)
            {
                // Mark that we need to recalculate path when we land
                needsPathRecalculation = true;
                HasPath = false;

                if (debugMode)
                {
                    Debug.Log($"[NavAgentHoppingController] Path calculation deferred - agent in air. Will recalculate when grounded.");
                }

                // Start moving anyway - we'll calculate path when grounded
                if (!IsMoving)
                {
                    StartMoving();
                }
                return true;
            }

            // Calculate path immediately if grounded
            bool pathFound = CalculatePath();

            if (pathFound)
            {
                if (debugMode)
                {
                    Debug.Log($"[NavAgentHoppingController] Path found to destination: {destination}");
                }

                // Only start moving if not already moving
                if (!IsMoving)
                {
                    StartMoving();
                }
                return true;
            }
            else
            {
                Debug.LogWarning("[NavAgentHoppingController] No valid path found to destination!");
                return false;
            }
        }

        /// <summary>
        /// Starts moving towards the current destination.
        /// </summary>
        public void StartMoving()
        {
            if (!hasDestination)
            {
                Debug.LogWarning("[NavAgentHoppingController] No destination set!");
                return;
            }

            if (IsMoving)
            {
                return;
            }

            IsMoving = true;
            destinationReached = false;
            currentPathIndex = 0;
            pathRecalculationTimer = 0f;
            hopCooldownTimer = 0f;

            // Calculate initial path if grounded
            if (IsGrounded)
            {
                CalculatePath();
            }

            OnMovementStarted?.Invoke();

            if (debugMode)
            {
                Debug.Log("[NavAgentHoppingController] Started moving");
            }
        }

        /// <summary>
        /// Stops movement.
        /// </summary>
        public void StopMoving()
        {
            if (!IsMoving)
            {
                return;
            }

            IsMoving = false;

            // Stop horizontal movement
            if (_rigidbody != null)
            {
                Vector3 velocity = _rigidbody.linearVelocity;
                _rigidbody.linearVelocity = new Vector3(0f, velocity.y, 0f);
            }

            OnMovementStopped?.Invoke();

            if (debugMode)
            {
                Debug.Log("[NavAgentHoppingController] Stopped moving");
            }
        }

        /// <summary>
        /// Clears the current destination.
        /// </summary>
        public void ClearDestination()
        {
            StopMoving();
            hasDestination = false;
            Destination = Vector3.zero;
            HasPath = false;
            navMeshPath.ClearCorners();
            needsPathRecalculation = false;

            if (debugMode)
            {
                Debug.Log("[NavAgentHoppingController] Destination cleared");
            }
        }

        /// <summary>
        /// Forces an immediate path recalculation.
        /// </summary>
        /// <returns>True if a valid path was found, false otherwise.</returns>
        public bool RecalculatePath()
        {
            if (!hasDestination)
            {
                return false;
            }

            // DEBUG: Log state before recalculation
            if (debugMode)
            {
                Debug.Log($"[NavAgentHoppingController] RecalculatePath() called - destinationReached: {destinationReached}, IsMoving: {IsMoving}, IsGrounded: {IsGrounded}");
            }

            return CalculatePath();
        }

        /// <summary>
        /// Gets the current path corners.
        /// </summary>
        /// <returns>Array of path corners, or empty array if no path.</returns>
        public Vector3[] GetPathCorners()
        {
            if (navMeshPath == null || navMeshPath.status != NavMeshPathStatus.PathComplete)
            {
                return new Vector3[0];
            }

            return navMeshPath.corners;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Calculates a path to the destination using NavMesh.
        /// </summary>
        /// <returns>True if a valid path was found, false otherwise.</returns>
        private bool CalculatePath()
        {
            if (navMeshPath == null)
            {
                navMeshPath = new NavMeshPath();
            }

            // Only calculate path if grounded to avoid navmesh warnings
            if (!IsGrounded)
            {
                if (debugMode)
                {
                    Debug.Log("[NavAgentHoppingController] Skipping path calculation - not grounded");
                }
                return false;
            }

            // Calculate path from current position to destination
            bool pathFound = NavMesh.CalculatePath(
                transform.position,
                Destination,
                navMeshAreaMask,
                navMeshPath
            );

            HasPath = pathFound && navMeshPath.status == NavMeshPathStatus.PathComplete;

            if (HasPath)
            {
                // Find closest path corner to start from
                currentPathIndex = FindClosestPathCornerIndex();

                // Update remaining distance
                UpdateRemainingDistance();

                OnPathRecalculated?.Invoke();

                if (debugMode)
                {
                    Debug.Log($"[NavAgentHoppingController] Path calculated with {navMeshPath.corners.Length} corners");
                }
            }
            else if (debugMode)
            {
                Debug.LogWarning("[NavAgentHoppingController] Path calculation failed");
            }

            return HasPath;
        }

        /// <summary>
        /// Finds the index of the closest path corner to the current position.
        /// </summary>
        /// <returns>The index of the closest path corner.</returns>
        private int FindClosestPathCornerIndex()
        {
            if (navMeshPath.corners.Length == 0)
            {
                return 0;
            }

            int closestIndex = 0;
            float closestDistance = float.MaxValue;

            for (int i = 0; i < navMeshPath.corners.Length; i++)
            {
                float distance = Vector3.Distance(transform.position, navMeshPath.corners[i]);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestIndex = i;
                }
            }

            return closestIndex;
        }

        /// <summary>
        /// Updates the remaining distance to the destination.
        /// </summary>
        private void UpdateRemainingDistance()
        {
            if (!HasPath || navMeshPath.corners.Length == 0)
            {
                RemainingDistance = Vector3.Distance(transform.position, Destination);
                return;
            }

            // Calculate total remaining distance along path
            float totalDistance = 0f;
            Vector3 currentPosition = transform.position;

            for (int i = currentPathIndex; i < navMeshPath.corners.Length; i++)
            {
                totalDistance += Vector3.Distance(currentPosition, navMeshPath.corners[i]);
                currentPosition = navMeshPath.corners[i];
            }

            RemainingDistance = totalDistance;
        }

        /// <summary>
        /// Handles movement towards the current path point.
        /// </summary>
        private void HandleMovement()
        {
            if (!HasPath || navMeshPath.corners.Length == 0)
            {
                if (debugMode)
                {
                    Debug.Log($"[NavAgentHoppingController] HandleMovement: No path available - HasPath: {HasPath}, corners.Length: {navMeshPath.corners.Length}");
                }
                return;
            }

            // DEBUG: Log current state
            if (debugMode)
            {
                Debug.Log($"[NavAgentHoppingController] HandleMovement: currentPathIndex: {currentPathIndex}, total corners: {navMeshPath.corners.Length}, RemainingDistance: {RemainingDistance:F2}, stoppingDistance: {stoppingDistance:F2}");
            }

            // Check if destination is reached
            if (stopAtDestination && RemainingDistance <= stoppingDistance)
            {
                destinationReached = true;
                StopMoving();
                OnDestinationReached?.Invoke();

                if (debugMode)
                {
                    Debug.Log("[NavAgentHoppingController] Destination reached");
                }
                return;
            }

            // Get current target path point
            Vector3 targetPoint = navMeshPath.corners[currentPathIndex];
            float distanceToTarget = Vector3.Distance(transform.position, targetPoint);

            // Check if we've reached the current path point
            // Only advance to next point if we've actually hopped toward this point
            if (distanceToTarget <= pathPointThreshold)
            {
                // Move to next path point
                currentPathIndex++;

                // Check if we've reached the end of the path
                if (currentPathIndex >= navMeshPath.corners.Length)
                {
                    destinationReached = true;
                    StopMoving();
                    OnDestinationReached?.Invoke();

                    if (debugMode)
                    {
                        Debug.Log("[NavAgentHoppingController] End of path reached");
                    }
                    return;
                }

                targetPoint = navMeshPath.corners[currentPathIndex];
            }

            // Update target direction for smooth rotation
            Vector3 direction = (targetPoint - transform.position).normalized;
            direction.y = 0f; // Keep on horizontal plane
            targetDirection = direction;

            // Hop towards the target point
            HopTowards(targetPoint);
        }

        /// <summary>
        /// Performs a hop towards the target position.
        /// </summary>
        /// <param name="targetPosition">The position to hop towards.</param>
        private void HopTowards(Vector3 targetPosition)
        {
            // Only hop if grounded
            if (!IsGrounded)
            {
                return;
            }

            // Check hop cooldown
            if (hopCooldownTimer > 0f)
            {
                if (debugMode)
                {
                    Debug.Log($"[NavAgentHoppingController] Hop delayed - cooldown remaining: {hopCooldownTimer:F2}s");
                }
                return;
            }

            // Calculate direction to target
            Vector3 direction = (targetPosition - transform.position).normalized;
            direction.y = 0f; // Keep on horizontal plane

            if (direction == Vector3.zero)
            {
                return;
            }

            // Rotate to face the target
            RotateTowards(direction);

            // Calculate hop velocity
            Vector3 hopVelocity;
            float distanceToTarget = Vector3.Distance(transform.position, targetPosition);

            // Use precise jumping if enabled and within threshold
            if (usePreciseJumping && distanceToTarget <= preciseJumpDistance)
            {
                hopVelocity = CalculatePreciseJumpVelocity(targetPosition);
                
                if (hopVelocity == Vector3.zero)
                {
                    // If precise calculation failed, fall back to standard hopping
                    if (debugMode)
                    {
                        Debug.LogWarning("[NavAgentHoppingController] Precise jump calculation failed, falling back to standard hopping");
                    }
                    hopVelocity = CalculateStandardHopVelocity(direction);
                }
                else
                {
                    if (debugMode)
                    {
                        Debug.Log($"[NavAgentHoppingController] Using precise jumping to reach target at {distanceToTarget:F2}m");
                    }
                }
            }
            else
            {
                hopVelocity = CalculateStandardHopVelocity(direction);
            }

            // Apply velocity to Rigidbody
            _rigidbody.linearVelocity = hopVelocity;

            // Reset hop cooldown
            hopCooldownTimer = hopDelay;

            OnHopPerformed?.Invoke(hopVelocity);

            if (debugMode)
            {
                Debug.Log($"[NavAgentHoppingController] Hopping towards {targetPosition} with velocity {hopVelocity}");
            }
        }

        /// <summary>
        /// Calculates the standard hop velocity using fixed hop forces.
        /// </summary>
        /// <param name="direction">The normalized direction to hop towards.</param>
        /// <returns>The hop velocity vector.</returns>
        private Vector3 CalculateStandardHopVelocity(Vector3 direction)
        {
            Vector3 hopVelocity = new Vector3(
                direction.x * hopHorizontalForce,
                hopForce,
                direction.z * hopHorizontalForce
            );

            // Clamp horizontal velocity
            Vector3 horizontalVelocity = new Vector3(hopVelocity.x, 0f, hopVelocity.z);
            if (horizontalVelocity.magnitude > maxHorizontalSpeed)
            {
                horizontalVelocity = horizontalVelocity.normalized * maxHorizontalSpeed;
                hopVelocity = new Vector3(horizontalVelocity.x, hopVelocity.y, horizontalVelocity.z);
            }

            return hopVelocity;
        }

        /// <summary>
        /// Calculates the precise jump velocity needed to land exactly on the target position.
        /// Uses projectile motion physics to determine the required initial velocity.
        /// </summary>
        /// <param name="targetPosition">The target position to land on.</param>
        /// <returns>The calculated velocity vector, or Vector3.zero if calculation fails.</returns>
        private Vector3 CalculatePreciseJumpVelocity(Vector3 targetPosition)
        {
            Vector3 startPos = transform.position;
            Vector3 endPos = targetPosition;

            // Calculate horizontal distance (on XZ plane)
            Vector3 horizontalDiff = new Vector3(endPos.x - startPos.x, 0f, endPos.z - startPos.z);
            float horizontalDistance = horizontalDiff.magnitude;

            // Calculate vertical distance
            float verticalDistance = endPos.y - startPos.y;

            // Get gravity value
            float gravity = useGravity ? Physics.gravity.y : 0f;
            
            // If gravity is disabled or zero, we can't calculate a precise jump
            if (gravity >= 0f)
            {
                if (debugMode)
                {
                    Debug.LogWarning("[NavAgentHoppingController] Cannot calculate precise jump without downward gravity");
                }
                return Vector3.zero;
            }

            // Normalize gravity to positive value for calculations
            float g = Mathf.Abs(gravity);

            // Try to find a feasible time of flight
            // We'll iterate through possible horizontal speeds to find one that works
            float bestHorizontalSpeed = 0f;
            float bestVerticalSpeed = 0f;
            bool foundSolution = false;

            // Try different horizontal speeds (from 50% to 150% of maxHorizontalSpeed)
            float minSpeed = maxHorizontalSpeed * 0.5f;
            float maxSpeed = maxHorizontalSpeed * 1.5f;
            int iterations = 20;

            for (int i = 0; i < iterations; i++)
            {
                float t = minSpeed + (maxSpeed - minSpeed) * (i / (float)(iterations - 1));
                
                // Calculate time of flight
                float timeOfFlight = horizontalDistance / t;

                // Calculate required vertical velocity
                // Using the equation: y = y0 + vy*t - 0.5*g*t^2
                // Solving for vy: vy = (y - y0 + 0.5*g*t^2) / t
                float verticalVelocity = (verticalDistance + 0.5f * g * timeOfFlight * timeOfFlight) / timeOfFlight;

                // Check if this solution is feasible
                // We want a reasonable vertical velocity (not too extreme)
                if (verticalVelocity > 0f && verticalVelocity < hopForce * 3f)
                {
                    // Check if we can actually reach this height
                    float maxHeight = verticalVelocity * verticalVelocity / (2f * g);
                    if (maxHeight < 10f) // Reasonable max height
                    {
                        bestHorizontalSpeed = t;
                        bestVerticalSpeed = verticalVelocity;
                        foundSolution = true;
                        break;
                    }
                }
            }

            if (!foundSolution)
            {
                if (debugMode)
                {
                    Debug.LogWarning("[NavAgentHoppingController] Could not find a feasible precise jump solution");
                }
                return Vector3.zero;
            }

            // Calculate the velocity vector
            Vector3 horizontalDirection = horizontalDiff.normalized;
            Vector3 preciseVelocity = new Vector3(
                horizontalDirection.x * bestHorizontalSpeed,
                bestVerticalSpeed,
                horizontalDirection.z * bestHorizontalSpeed
            );

            // Ensure horizontal speed doesn't exceed max
            Vector3 horizontalVel = new Vector3(preciseVelocity.x, 0f, preciseVelocity.z);
            if (horizontalVel.magnitude > maxHorizontalSpeed)
            {
                horizontalVel = horizontalVel.normalized * maxHorizontalSpeed;
                preciseVelocity = new Vector3(horizontalVel.x, preciseVelocity.y, horizontalVel.z);
            }

            return preciseVelocity;
        }

        /// <summary>
        /// Rotates to face the target direction.
        /// </summary>
        /// <param name="direction">The direction to face.</param>
        private void RotateTowards(Vector3 direction)
        {
            if (direction == Vector3.zero)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
            _rigidbody.rotation = Quaternion.Slerp(
                _rigidbody.rotation,
                targetRotation,
                rotationSpeed * Time.fixedDeltaTime
            );
        }

        /// <summary>
        /// Checks if the controller is grounded.
        /// </summary>
        private void CheckGrounded()
        {
            IsGrounded = Physics.Raycast(
                transform.position,
                Vector3.down,
                groundCheckDistance,
                groundLayerMask,
                QueryTriggerInteraction.Ignore
            );
        }

        #endregion

        #region Editor

        private void OnValidate()
        {
            hopForce = Mathf.Max(0f, hopForce);
            hopHorizontalForce = Mathf.Max(0f, hopHorizontalForce);
            maxHorizontalSpeed = Mathf.Max(0f, maxHorizontalSpeed);
            rotationSpeed = Mathf.Max(0f, rotationSpeed);
            groundCheckDistance = Mathf.Max(0.01f, groundCheckDistance);
            airDrag = Mathf.Max(0f, airDrag);
            groundDrag = Mathf.Max(0f, groundDrag);
            hopDelay = Mathf.Max(0f, hopDelay);
            pathRecalculationInterval = Mathf.Max(0.1f, pathRecalculationInterval);
            pathPointThreshold = Mathf.Max(0.1f, pathPointThreshold);
            stoppingDistance = Mathf.Max(0f, stoppingDistance);
        }

        private void OnDrawGizmos()
        {
            if (!showGizmos)
            {
                return;
            }

            // Draw ground check
            Gizmos.color = IsGrounded ? Color.green : Color.red;
            Gizmos.DrawLine(
                transform.position,
                transform.position + Vector3.down * groundCheckDistance
            );

            // Draw destination
            if (hasDestination)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(Destination, stoppingDistance);

                // Draw line to destination
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, Destination);
            }

            // Draw path
            if (HasPath && navMeshPath != null && navMeshPath.corners.Length > 0)
            {
                Gizmos.color = Color.cyan;

                for (int i = 0; i < navMeshPath.corners.Length - 1; i++)
                {
                    Gizmos.DrawLine(navMeshPath.corners[i], navMeshPath.corners[i + 1]);
                }

                // Draw path corners
                for (int i = 0; i < navMeshPath.corners.Length; i++)
                {
                    Gizmos.color = (i == currentPathIndex) ? Color.green : Color.cyan;
                    Gizmos.DrawWireSphere(navMeshPath.corners[i], 0.2f);
                }

                // Draw line to current target
                if (currentPathIndex < navMeshPath.corners.Length)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawLine(transform.position, navMeshPath.corners[currentPathIndex]);
                }
            }

            // Draw forward direction
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, transform.forward * 2f);
        }

        #endregion
    }
}
