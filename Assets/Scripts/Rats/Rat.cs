using UnityEngine;

namespace FindersCheesers
{
    /// <summary>
    /// Represents a rat that can support to Rat Pack.
    /// Rats can be registered with to Rat Pack to help carry it.
    /// </summary>
    [AddComponentMenu("Finders Cheesers/Rat")]
    public class Rat : MonoBehaviour
    {
        [Header("Rat Settings")]
        // The unique ID of this rat (not serialized, generated on Start)
        private string ratId;

        [Tooltip("Is this rat currently supporting to Rat Pack?")]
        [SerializeField]
        private bool isSupportingKing = false;

        [Tooltip("Is this rat currently deposited to a RatInteractable?")]
        [SerializeField]
        private bool isDeposited = false;

        [Tooltip("The RatInventory this rat is currently supporting")]
        [SerializeField]
        private RatInventory currentRatInventory;

        [Tooltip("The strength of this rat for supporting to Rat Pack")]
        [SerializeField]
        private float supportStrength = 1f;

        [Tooltip("Movement speed when gathering/dispersing")]
        [SerializeField]
        private float movementSpeed = 3f;

        [Header("NavMesh Agent Settings")]
        [Tooltip("If enabled, disables the NavMeshAgent when picked up by RatInventory")]
        [SerializeField]
        private bool disableNavAgentOnPickup = true;

        [Tooltip("If enabled, re-enables the NavMeshAgent when dispersed or removed from inventory")]
        [SerializeField]
        private bool enableNavAgentOnDisperse = true;

        [Header("Debug")]
        [Tooltip("Show debug information in the console")]
        [SerializeField]
        private bool debugMode = false;

        // Component references
        private UnityEngine.AI.NavMeshAgent navMeshAgent;

        // State variables
        private bool isMovingToInventory = false;
        private bool isRunningAway = false;
        private Vector3 targetPosition;

        /// <summary>
        /// Gets the movement speed of this rat.
        /// </summary>
        public float MovementSpeed => movementSpeed;

        /// <summary>
        /// Gets whether this rat is currently moving to inventory.
        /// </summary>
        public bool IsMovingToInventory => isMovingToInventory;

        /// <summary>
        /// Gets whether this rat is currently running away.
        /// </summary>
        public bool IsRunningAway => isRunningAway;

        /// <summary>
        /// Gets the current target position for movement.
        /// </summary>
        public Vector3 TargetPosition => targetPosition;

        /// <summary>
        /// Gets unique ID of this rat.
        /// </summary>
        public string RatId
        {
            get
            {
                if (string.IsNullOrEmpty(ratId))
                {
                    ratId = System.Guid.NewGuid().ToString();
                }
                return ratId;
            }
        }

        /// <summary>
        /// Gets or sets whether this rat is currently supporting to Rat Pack.
        /// </summary>
        public bool IsSupportingKing
        {
            get => isSupportingKing;
            set => isSupportingKing = value;
        }

        /// <summary>
        /// Gets or sets whether this rat is currently deposited to a RatInteractable.
        /// </summary>
        public bool IsDeposited
        {
            get => isDeposited;
            set => isDeposited = value;
        }

        /// <summary>
        /// Gets or sets the RatInventory this rat is currently supporting.
        /// </summary>
        public RatInventory CurrentRatInventory
        {
            get => currentRatInventory;
            set => currentRatInventory = value;
        }

        /// <summary>
        /// Gets the support strength of this rat.
        /// </summary>
        public float SupportStrength => supportStrength;

        /// <summary>
        /// Gets the position of this rat in world space.
        /// </summary>
        public Vector3 Position => transform.position;

        private void Awake()
        {
            // Get NavMeshAgent component if it exists
            navMeshAgent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        }

        /// <summary>
        /// Disables the NavMeshAgent component if the setting is enabled.
        /// Called when the rat is picked up by RatInventory.
        /// </summary>
        public void DisableNavAgent()
        {
            if (!disableNavAgentOnPickup)
            {
                return;
            }

            if (navMeshAgent != null && navMeshAgent.enabled)
            {
                navMeshAgent.enabled = false;

                if (debugMode)
                {
                    Debug.Log($"[Rat] NavMeshAgent disabled. Rat ID: {RatId}");
                }
            }
        }

        /// <summary>
        /// Enables the NavMeshAgent component if the setting is enabled.
        /// Called when the rat is dispersed or removed from inventory.
        /// </summary>
        public void EnableNavAgent()
        {
            if (!enableNavAgentOnDisperse)
            {
                return;
            }

            if (navMeshAgent != null && !navMeshAgent.enabled)
            {
                navMeshAgent.enabled = true;

                if (debugMode)
                {
                    Debug.Log($"[Rat] NavMeshAgent enabled. Rat ID: {RatId}");
                }
            }
        }

        private void Start()
        {
            // Generate a unique ID if none is set
            if (string.IsNullOrEmpty(ratId))
            {
                ratId = System.Guid.NewGuid().ToString();
            }
        }

        private void Update()
        {
            // Handle movement to inventory
            if (isMovingToInventory)
            {
                UpdateMovementToInventory();
            }
            // Handle running away
            else if (isRunningAway)
            {
                UpdateRunningAway();
            }
        }

        /// <summary>
        /// Updates movement toward the inventory.
        /// </summary>
        private void UpdateMovementToInventory()
        {
            if (currentRatInventory == null)
            {
                isMovingToInventory = false;
                return;
            }

            Vector3 inventoryPosition = currentRatInventory.transform.position;
            float distance = Vector3.Distance(transform.position, inventoryPosition);

            // Check if we've reached the inventory
            if (distance < 0.5f)
            {
                isMovingToInventory = false;
                // Register with the inventory
                RegisterWithRatInventory(currentRatInventory);

                if (debugMode)
                {
                    Debug.Log($"[Rat] Reached inventory. Rat ID: {RatId}");
                }
                return;
            }

            // Move toward inventory
            if (navMeshAgent != null && navMeshAgent.enabled)
            {
                // Use NavMeshAgent for navigation
                if (navMeshAgent.destination != inventoryPosition || !navMeshAgent.hasPath)
                {
                    navMeshAgent.SetDestination(inventoryPosition);
                }
            }
            else
            {
                // Use direct movement
                Vector3 direction = (inventoryPosition - transform.position).normalized;
                transform.position += direction * movementSpeed * Time.deltaTime;
                transform.LookAt(inventoryPosition);
            }
        }

        /// <summary>
        /// Updates running away behavior.
        /// </summary>
        private void UpdateRunningAway()
        {
            float distance = Vector3.Distance(transform.position, targetPosition);

            // Check if we've reached the target position
            if (distance < 0.5f)
            {
                isRunningAway = false;

                if (debugMode)
                {
                    Debug.Log($"[Rat] Reached target position. Rat ID: {RatId}");
                }
                return;
            }

            // Move toward target position
            if (navMeshAgent != null && navMeshAgent.enabled)
            {
                // Use NavMeshAgent for navigation
                if (navMeshAgent.destination != targetPosition || !navMeshAgent.hasPath)
                {
                    navMeshAgent.SetDestination(targetPosition);
                }
            }
            else
            {
                // Use direct movement
                Vector3 direction = (targetPosition - transform.position).normalized;
                transform.position += direction * movementSpeed * Time.deltaTime;
                transform.LookAt(targetPosition);
            }
        }

        /// <summary>
        /// Makes the rat move toward the specified inventory.
        /// </summary>
        /// <param name="inventory">The inventory to move toward.</param>
        public void MoveToInventory(RatInventory inventory)
        {
            if (inventory == null)
            {
                Debug.LogWarning("[Rat] Cannot move to null inventory.");
                return;
            }

            // Stop any current running away
            if (isRunningAway)
            {
                isRunningAway = false;
                if (navMeshAgent != null)
                {
                    navMeshAgent.ResetPath();
                }
            }

            currentRatInventory = inventory;
            isMovingToInventory = true;

            if (debugMode)
            {
                Debug.Log($"[Rat] Moving to inventory. Rat ID: {RatId}");
            }
        }

        /// <summary>
        /// Makes the rat run away from the current inventory.
        /// </summary>
        /// <param name="runDistance">The distance to run away.</param>
        public void RunAway(float runDistance = 10f)
        {
            // Stop any current movement to inventory
            if (isMovingToInventory)
            {
                isMovingToInventory = false;
                if (navMeshAgent != null)
                {
                    navMeshAgent.ResetPath();
                }
            }

            // Calculate a random direction away from inventory
            Vector3 awayDirection = Vector3.zero;
            if (currentRatInventory != null)
            {
                awayDirection = (transform.position - currentRatInventory.transform.position).normalized;
            }
            else
            {
                // Random direction if no inventory
                awayDirection = Random.insideUnitSphere;
                awayDirection.y = 0f;
                awayDirection = awayDirection.normalized;
            }

            // Set target position
            targetPosition = transform.position + awayDirection * runDistance;

            // Unregister from inventory (this will re-enable NavMeshAgent if enabled)
            if (currentRatInventory != null)
            {
                UnregisterFromRatInventory();
            }

            isRunningAway = true;

            if (debugMode)
            {
                Debug.Log($"[Rat] Running away. Rat ID: {RatId}, Distance: {runDistance}");
            }
        }

        /// <summary>
        /// Registers this rat with a RatInventory.
        /// </summary>
        /// <param name="ratInventory">The RatInventory to register with.</param>
        /// <returns>True if registration was successful, false otherwise.</returns>
        public bool RegisterWithRatInventory(RatInventory ratInventory)
        {
            if (ratInventory == null)
            {
                Debug.LogWarning("[Rat] Cannot register with null RatInventory.");
                return false;
            }

            if (currentRatInventory != null && currentRatInventory != ratInventory)
            {
                Debug.LogWarning($"[Rat] Already registered with another RatInventory. Unregister first.");
                return false;
            }

            bool success = ratInventory.AddRat(this);

            if (success)
            {
                currentRatInventory = ratInventory;
                isSupportingKing = true;

                // Disable NavMeshAgent when picked up
                DisableNavAgent();

                if (debugMode)
                {
                    Debug.Log($"[Rat] Registered with RatInventory. Rat ID: {RatId}");
                }
            }

            return success;
        }

        /// <summary>
        /// Unregisters this rat from its current RatInventory.
        /// </summary>
        /// <returns>True if unregistration was successful, false otherwise.</returns>
        public bool UnregisterFromRatInventory()
        {
            if (currentRatInventory == null)
            {
                Debug.LogWarning("[Rat] Not registered with any RatInventory.");
                return false;
            }

            bool success = currentRatInventory.RemoveRat(this);

            if (success)
            {
                currentRatInventory = null;
                isSupportingKing = false;

                // Re-enable NavMeshAgent when removed from inventory
                EnableNavAgent();

                if (debugMode)
                {
                    Debug.Log($"[Rat] Unregistered from RatInventory. Rat ID: {RatId}");
                }
            }

            return success;
        }

        /// <summary>
        /// Sets the support strength of this rat.
        /// </summary>
        /// <param name="strength">The new support strength.</param>
        public void SetSupportStrength(float strength)
        {
            supportStrength = Mathf.Max(0f, strength);
        }

        /// <summary>
        /// Sets the movement speed of this rat.
        /// </summary>
        /// <param name="speed">The new movement speed.</param>
        public void SetMovementSpeed(float speed)
        {
            movementSpeed = Mathf.Max(0.1f, speed);
        }

        private void OnDestroy()
        {
            // Unregister from RatInventory when destroyed
            if (isSupportingKing && currentRatInventory != null)
            {
                currentRatInventory.RemoveRat(this);
            }
        }

        private void OnDrawGizmos()
        {
            // Draw a visual indicator for this rat
            Gizmos.color = isMovingToInventory ? Color.blue : (isRunningAway ? Color.red : (isSupportingKing ? Color.green : Color.gray));
            Gizmos.DrawWireSphere(transform.position, 0.3f);

            // Draw target position if moving
            if (isMovingToInventory && currentRatInventory != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(transform.position, currentRatInventory.transform.position);
            }
            else if (isRunningAway)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, targetPosition);
            }
            else if (isSupportingKing && currentRatInventory != null)
            {
                // Draw line to RatInventory owner
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, currentRatInventory.transform.position);
            }
        }
    }
}
