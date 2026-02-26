using UnityEngine;

namespace FindersCheesers
{
    /// <summary>
    /// A component that makes a rat automatically find and navigate to the closest RatInteractable
    /// within a certain distance and deposit itself.
    /// </summary>
    [AddComponentMenu("Finders Cheesers/Rat Auto Depositer")]
    public class RatAutoDepositer : MonoBehaviour
    {
        [Header("Detection Settings")]
        [Tooltip("Radius to search for RatInteractables")]
        [SerializeField]
        private float detectionRadius = 10f;

        [Tooltip("Layer mask for finding RatInteractables")]
        [SerializeField]
        private LayerMask interactableLayerMask = 1;

        [Tooltip("Tag that identifies RatInteractable GameObjects")]
        [SerializeField]
        private string interactableTag = "Interactable";

        [Header("Movement Settings")]
        [Tooltip("Movement speed when navigating to interactable")]
        [SerializeField]
        private float movementSpeed = 3f;

        [Tooltip("Distance at which the rat is considered to have reached the interactable")]
        [SerializeField]
        private float arrivalDistance = 0.5f;

        [Tooltip("Whether to use NavMeshAgent for navigation (if available)")]
        [SerializeField]
        private bool useNavMeshAgent = true;

        [Header("Deposit Settings")]
        [Tooltip("Whether to automatically deposit when reaching the interactable")]
        [SerializeField]
        private bool autoDeposit = true;

        [Tooltip("Delay in seconds before depositing after arrival")]
        [SerializeField]
        private float depositDelay = 0.5f;

        [Header("Debug")]
        [Tooltip("Show debug information in the console")]
        [SerializeField]
        private bool debugMode = false;

        [Tooltip("Visualize detection radius in Scene view")]
        [SerializeField]
        private bool visualizeDetectionRadius = true;

        // Component references
        private Rat rat;
        private UnityEngine.AI.NavMeshAgent navMeshAgent;
        private RatInteractable targetInteractable;

        // State variables
        private bool isMovingToInteractable = false;
        private float arrivalTime;

        /// <summary>
        /// Gets whether this rat is currently moving to an interactable.
        /// </summary>
        public bool IsMovingToInteractable => isMovingToInteractable;

        /// <summary>
        /// Gets the current target interactable.
        /// </summary>
        public RatInteractable TargetInteractable => targetInteractable;

        private void Awake()
        {
            // Get Rat component
            rat = GetComponent<Rat>();
            if (rat == null)
            {
                Debug.LogError("[RatAutoDepositer] Rat component not found on this GameObject!");
                enabled = false;
                return;
            }

            // Get NavMeshAgent component if it exists
            navMeshAgent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        }

        private void Start()
        {
            // Disable NavMeshAgent if not using it
            if (!useNavMeshAgent && navMeshAgent != null)
            {
                navMeshAgent.enabled = false;
            }
        }

        private void Update()
        {
            // Skip if rat is already supporting king or deposited
            if (rat.IsSupportingKing || rat.IsDeposited)
            {
                if (isMovingToInteractable)
                {
                    StopMovingToInteractable();
                }
                return;
            }

            // Skip if rat is running away or moving to inventory
            if (rat.IsRunningAway || rat.IsMovingToInventory)
            {
                if (isMovingToInteractable)
                {
                    StopMovingToInteractable();
                }
                return;
            }

            // If not moving to interactable, try to find one
            if (!isMovingToInteractable)
            {
                FindAndMoveToInteractable();
            }
            // If moving to interactable, update movement
            else
            {
                UpdateMovementToInteractable();
            }
        }

        /// <summary>
        /// Finds the closest RatInteractable and starts moving toward it.
        /// </summary>
        private void FindAndMoveToInteractable()
        {
            // Find all colliders within detection radius
            Collider[] colliders = Physics.OverlapSphere(
                transform.position,
                detectionRadius,
                interactableLayerMask,
                QueryTriggerInteraction.Ignore
            );

            RatInteractable closestInteractable = null;
            float closestDistance = float.MaxValue;

            // Process each collider to find RatInteractables
            foreach (Collider collider in colliders)
            {
                if (collider == null)
                {
                    continue;
                }

                // Check if collider has a RatInteractable component
                RatInteractable interactable = collider.GetComponent<RatInteractable>();
                if (interactable == null)
                {
                    // Check parent object
                    interactable = collider.GetComponentInParent<RatInteractable>();
                }

                if (interactable == null)
                {
                    continue;
                }

                // Check if interactable can accept rats
                if (!interactable.CanAcceptRats)
                {
                    continue;
                }

                // Check if interactable matches tag (if specified)
                if (!string.IsNullOrEmpty(interactableTag) && !collider.gameObject.CompareTag(interactableTag))
                {
                    continue;
                }

                // Calculate distance to this interactable
                float distance = Vector3.Distance(transform.position, interactable.Transform.position);

                // Check if this is the closest interactable
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestInteractable = interactable;
                }
            }

            // If we found a valid interactable, start moving toward it
            if (closestInteractable != null)
            {
                StartMovingToInteractable(closestInteractable);

                if (debugMode)
                {
                    Debug.Log($"[RatAutoDepositer] Found interactable: {closestInteractable.InteractionDescription}, Distance: {closestDistance:F2}");
                }
            }
        }

        /// <summary>
        /// Starts moving toward the specified interactable.
        /// </summary>
        /// <param name="interactable">The interactable to move toward.</param>
        private void StartMovingToInteractable(RatInteractable interactable)
        {
            if (interactable == null)
            {
                Debug.LogWarning("[RatAutoDepositer] Cannot move to null interactable.");
                return;
            }

            targetInteractable = interactable;
            isMovingToInteractable = true;

            if (debugMode)
            {
                Debug.Log($"[RatAutoDepositer] Starting to move to interactable: {interactable.InteractionDescription}");
            }
        }

        /// <summary>
        /// Updates movement toward the target interactable.
        /// </summary>
        private void UpdateMovementToInteractable()
        {
            if (targetInteractable == null || !targetInteractable.CanAcceptRats)
            {
                StopMovingToInteractable();
                return;
            }

            Vector3 targetPosition = targetInteractable.Transform.position;
            float distance = Vector3.Distance(transform.position, targetPosition);

            // Check if we've arrived at the interactable
            if (distance < arrivalDistance)
            {
                HandleArrivalAtInteractable();
                return;
            }

            // Move toward interactable
            if (navMeshAgent != null && navMeshAgent.enabled && useNavMeshAgent)
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
        /// Handles arrival at the target interactable.
        /// </summary>
        private void HandleArrivalAtInteractable()
        {
            // Check if we've waited long enough to deposit
            if (arrivalTime == 0f)
            {
                arrivalTime = Time.time;
                return;
            }

            if (Time.time - arrivalTime < depositDelay)
            {
                return;
            }

            // Deposit the rat if auto-deposit is enabled
            if (autoDeposit && targetInteractable != null)
            {
                DepositToInteractable();
            }
            else
            {
                // Just stop moving
                StopMovingToInteractable();
            }
        }

        /// <summary>
        /// Deposits this rat to the target interactable.
        /// </summary>
        private void DepositToInteractable()
        {
            if (targetInteractable == null)
            {
                Debug.LogWarning("[RatAutoDepositer] Cannot deposit to null interactable.");
                return;
            }

            // Check if rat can be deposited (not in cooldown, not supporting king, etc.)
            if (rat.IsSupportingKing)
            {
                if (debugMode)
                {
                    Debug.LogWarning("[RatAutoDepositer] Cannot deposit - rat is supporting king.");
                }
                StopMovingToInteractable();
                return;
            }

            // Create a list with this rat
            System.Collections.Generic.List<Rat> ratsToDeposit = new System.Collections.Generic.List<Rat> { rat };

            // Try to deposit the rat
            bool success = targetInteractable.DepositRats(ratsToDeposit);

            if (success)
            {
                if (debugMode)
                {
                    Debug.Log($"[RatAutoDepositer] Successfully deposited rat to: {targetInteractable.InteractionDescription}");
                }
            }
            else
            {
                if (debugMode)
                {
                    Debug.LogWarning("[RatAutoDepositer] Failed to deposit rat.");
                }
            }

            // Stop moving regardless of success
            StopMovingToInteractable();
        }

        /// <summary>
        /// Stops moving to the interactable.
        /// </summary>
        private void StopMovingToInteractable()
        {
            isMovingToInteractable = false;
            targetInteractable = null;
            arrivalTime = 0f;

            // Stop NavMeshAgent if it's active
            if (navMeshAgent != null && navMeshAgent.enabled)
            {
                navMeshAgent.ResetPath();
            }

            if (debugMode)
            {
                Debug.Log("[RatAutoDepositer] Stopped moving to interactable.");
            }
        }

        /// <summary>
        /// Sets the detection radius.
        /// </summary>
        /// <param name="radius">The new detection radius.</param>
        public void SetDetectionRadius(float radius)
        {
            detectionRadius = Mathf.Max(0.1f, radius);

            if (debugMode)
            {
                Debug.Log($"[RatAutoDepositer] Detection radius set to: {detectionRadius}");
            }
        }

        /// <summary>
        /// Sets the movement speed.
        /// </summary>
        /// <param name="speed">The new movement speed.</param>
        public void SetMovementSpeed(float speed)
        {
            movementSpeed = Mathf.Max(0.1f, speed);

            if (debugMode)
            {
                Debug.Log($"[RatAutoDepositer] Movement speed set to: {movementSpeed}");
            }
        }

        /// <summary>
        /// Sets the arrival distance.
        /// </summary>
        /// <param name="distance">The new arrival distance.</param>
        public void SetArrivalDistance(float distance)
        {
            arrivalDistance = Mathf.Max(0.1f, distance);

            if (debugMode)
            {
                Debug.Log($"[RatAutoDepositer] Arrival distance set to: {arrivalDistance}");
            }
        }

        private void OnDrawGizmos()
        {
            // Draw detection radius
            if (visualizeDetectionRadius)
            {
                Gizmos.color = isMovingToInteractable ? Color.yellow : Color.green;
                Gizmos.DrawWireSphere(transform.position, detectionRadius);
            }

            // Draw line to target interactable
            if (Application.isPlaying && isMovingToInteractable && targetInteractable != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(transform.position, targetInteractable.Transform.position);

                // Draw target position
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(targetInteractable.Transform.position, arrivalDistance);
            }
        }

        private void Reset()
        {
            detectionRadius = 10f;
            movementSpeed = 3f;
            arrivalDistance = 0.5f;
            useNavMeshAgent = true;
            autoDeposit = true;
            depositDelay = 0.5f;
        }
    }
}
