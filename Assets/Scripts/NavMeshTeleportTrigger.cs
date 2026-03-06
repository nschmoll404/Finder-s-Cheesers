using UnityEngine;
using UnityEngine.AI;

namespace FindersCheesers
{
    /// <summary>
    /// A trigger box that teleports any object that enters it to the nearest point on a navmesh.
    /// Works with any collider, not just NavMeshAgents.
    /// </summary>
    [AddComponentMenu("Finders Cheesers/NavMesh Teleport Trigger")]
    [RequireComponent(typeof(Collider))]
    public class NavMeshTeleportTrigger : MonoBehaviour
    {
        #region Settings

        [Header("NavMesh Settings")]
        [Tooltip("The NavMesh area mask to use when finding the nearest point")]
        [SerializeField]
        private int navMeshAreaMask = NavMesh.AllAreas;

        [Tooltip("The maximum distance to search for a valid navmesh point")]
        [SerializeField]
        private float maxSearchDistance = 10f;

        [Header("Teleport Settings")]
        [Tooltip("Offset from the navmesh surface (helps prevent objects from clipping through the ground)")]
        [SerializeField]
        private float verticalOffset = 0.1f;

        [Tooltip("Whether to preserve the object's rotation when teleporting")]
        [SerializeField]
        private bool preserveRotation = true;

        [Header("Filter Settings")]
        [Tooltip("Layer mask to filter which objects can be teleported")]
        [SerializeField]
        private LayerMask teleportableLayers = -1;

        [Tooltip("Tag filter - only objects with this tag will be teleported. Leave empty to ignore tag.")]
        [SerializeField]
        private string requiredTag = string.Empty;

        [Header("Cooldown Settings")]
        [Tooltip("Minimum time between teleports for the same object (prevents rapid re-triggering)")]
        [SerializeField]
        private float teleportCooldown = 0.5f;

        [Tooltip("Whether to show debug information in the console and scene view")]
        [SerializeField]
        private bool debugMode = false;

        #endregion

        #region Private Fields

        private Collider _triggerCollider;
        private System.Collections.Generic.Dictionary<GameObject, float> _lastTeleportTimes;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _triggerCollider = GetComponent<Collider>();
            _lastTeleportTimes = new System.Collections.Generic.Dictionary<GameObject, float>();

            // Ensure the collider is set as a trigger
            if (_triggerCollider != null && !_triggerCollider.isTrigger)
            {
                _triggerCollider.isTrigger = true;

                if (debugMode)
                {
                    Debug.LogWarning("[NavMeshTeleportTrigger] Collider was not set as trigger. Auto-enabled trigger mode.");
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            // Check if the object should be teleported
            if (!ShouldTeleport(other))
            {
                return;
            }

            // Check cooldown for this specific object
            if (IsOnCooldown(other.gameObject))
            {
                if (debugMode)
                {
                    Debug.Log($"[NavMeshTeleportTrigger] {other.gameObject.name} is on cooldown, skipping teleport.");
                }
                return;
            }

            // Find the nearest navmesh point and teleport the object
            Vector3 nearestNavMeshPoint = FindNearestNavMeshPoint(other.transform.position);

            if (nearestNavMeshPoint != Vector3.zero)
            {
                TeleportObject(other.gameObject, nearestNavMeshPoint);
                UpdateLastTeleportTime(other.gameObject);
            }
            else if (debugMode)
            {
                Debug.LogWarning($"[NavMeshTeleportTrigger] Could not find a valid navmesh point near {other.gameObject.name}.");
            }
        }

        private void OnTriggerStay(Collider other)
        {
            // Optional: Handle objects that stay in the trigger
            // This can be useful for objects that might have entered but weren't teleported
            // (e.g., if the navmesh wasn't available at the exact moment of entry)
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Determines if the given collider should be teleported based on layer and tag filters.
        /// </summary>
        /// <param name="other">The collider to check.</param>
        /// <returns>True if the object should be teleported, false otherwise.</returns>
        private bool ShouldTeleport(Collider other)
        {
            // Check layer mask
            if (!IsInLayerMask(other.gameObject.layer, teleportableLayers))
            {
                return false;
            }

            // Check tag filter
            if (!string.IsNullOrEmpty(requiredTag) && !other.gameObject.CompareTag(requiredTag))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Checks if a layer is included in a layer mask.
        /// </summary>
        /// <param name="layer">The layer to check.</param>
        /// <param name="layerMask">The layer mask to check against.</param>
        /// <returns>True if the layer is in the mask, false otherwise.</returns>
        private bool IsInLayerMask(int layer, LayerMask layerMask)
        {
            return layerMask == (layerMask | (1 << layer));
        }

        /// <summary>
        /// Checks if an object is currently on cooldown.
        /// </summary>
        /// <param name="obj">The object to check.</param>
        /// <returns>True if the object is on cooldown, false otherwise.</returns>
        private bool IsOnCooldown(GameObject obj)
        {
            if (teleportCooldown <= 0f)
            {
                return false;
            }

            if (_lastTeleportTimes.TryGetValue(obj, out float lastTeleportTime))
            {
                return Time.time - lastTeleportTime < teleportCooldown;
            }

            return false;
        }

        /// <summary>
        /// Updates the last teleport time for an object.
        /// </summary>
        /// <param name="obj">The object to update.</param>
        private void UpdateLastTeleportTime(GameObject obj)
        {
            _lastTeleportTimes[obj] = Time.time;
        }

        /// <summary>
        /// Finds the nearest valid point on the navmesh to the given position.
        /// </summary>
        /// <param name="position">The position to search from.</param>
        /// <returns>The nearest navmesh point, or Vector3.zero if no point was found.</returns>
        private Vector3 FindNearestNavMeshPoint(Vector3 position)
        {
            // Sample the navmesh at the given position
            NavMeshHit hit;
            bool found = NavMesh.SamplePosition(
                position,
                out hit,
                maxSearchDistance,
                navMeshAreaMask
            );

            if (found)
            {
                if (debugMode)
                {
                    Debug.Log($"[NavMeshTeleportTrigger] Found navmesh point at {hit.position} (distance: {hit.distance:F2})");
                }
                return hit.position;
            }

            if (debugMode)
            {
                Debug.LogWarning($"[NavMeshTeleportTrigger] No navmesh point found within {maxSearchDistance} units of {position}");
            }
            return Vector3.zero;
        }

        /// <summary>
        /// Teleports the given object to the specified position.
        /// </summary>
        /// <param name="obj">The object to teleport.</param>
        /// <param name="targetPosition">The position to teleport to.</param>
        private void TeleportObject(GameObject obj, Vector3 targetPosition)
        {
            // Handle ThrowableObject if present - stop throw animation before teleporting
            ThrowableObject throwable = obj.GetComponent<ThrowableObject>();
            if (throwable != null && throwable.IsThrowing)
            {
                throwable.StopThrowAndResetPhysics();
                
                if (debugMode)
                {
                    Debug.Log($"[NavMeshTeleportTrigger] Stopped throw animation for {obj.name}");
                }
            }

            // Apply vertical offset
            Vector3 finalPosition = targetPosition + Vector3.up * verticalOffset;

            // Preserve rotation if enabled
            Quaternion originalRotation = obj.transform.rotation;

            // Teleport the object
            obj.transform.position = finalPosition;

            if (preserveRotation)
            {
                obj.transform.rotation = originalRotation;
            }

            // Handle Rigidbody if present
            Rigidbody rb = obj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                // Only reset velocity if the Rigidbody is not kinematic
                // (ThrowableObject.StopThrowAndResetPhysics already handles this for throwables)
                if (!rb.isKinematic)
                {
                    // Reset velocity to prevent physics issues after teleport
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;

                    if (debugMode)
                    {
                        Debug.Log($"[NavMeshTeleportTrigger] Reset Rigidbody velocity for {obj.name}");
                    }
                }
                else if (debugMode)
                {
                    Debug.Log($"[NavMeshTeleportTrigger] Skipping velocity reset for {obj.name} (Rigidbody is kinematic)");
                }
            }

            // Handle CharacterController if present
            CharacterController characterController = obj.GetComponent<CharacterController>();
            if (characterController != null)
            {
                characterController.enabled = false;
                obj.transform.position = finalPosition;
                characterController.enabled = true;

                if (debugMode)
                {
                    Debug.Log($"[NavMeshTeleportTrigger] Handled CharacterController for {obj.name}");
                }
            }

            // Handle NavMeshAgent if present
            NavMeshAgent agent = obj.GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                agent.Warp(finalPosition);

                if (debugMode)
                {
                    Debug.Log($"[NavMeshTeleportTrigger] Warped NavMeshAgent for {obj.name}");
                }
            }

            if (debugMode)
            {
                Debug.Log($"[NavMeshTeleportTrigger] Teleported {obj.name} from {obj.transform.position} to {finalPosition}");
            }
        }

        #endregion

        #region Editor

        /// <summary>
        /// Draws debug visualization in the scene view when the GameObject is selected.
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            // Save the current Gizmos matrix
            Matrix4x4 originalMatrix = Gizmos.matrix;

            // Draw the trigger collider bounds
            Collider collider = GetComponent<Collider>();
            if (collider != null)
            {
                Gizmos.color = new Color(0f, 1f, 1f, 0.3f);

                if (collider is BoxCollider boxCollider)
                {
                    Gizmos.matrix = transform.localToWorldMatrix;
                    Gizmos.DrawWireCube(boxCollider.center, boxCollider.size);
                }
                else if (collider is SphereCollider sphereCollider)
                {
                    Gizmos.DrawWireSphere(transform.TransformPoint(sphereCollider.center), sphereCollider.radius);
                }
                else if (collider is CapsuleCollider capsuleCollider)
                {
                    Gizmos.DrawWireSphere(transform.TransformPoint(capsuleCollider.center), capsuleCollider.radius);
                }
            }

            // Restore the matrix before drawing the search distance sphere
            Gizmos.matrix = originalMatrix;

            // Draw the max search distance as a sphere at the object's position
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, maxSearchDistance);

            // Draw a semi-transparent fill for the search distance
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.05f);
            Gizmos.DrawSphere(transform.position, maxSearchDistance);
        }

        /// <summary>
        /// Validates settings in the editor.
        /// </summary>
        private void OnValidate()
        {
            maxSearchDistance = Mathf.Max(0.1f, maxSearchDistance);
            verticalOffset = Mathf.Max(0f, verticalOffset);
            teleportCooldown = Mathf.Max(0f, teleportCooldown);
        }

        #endregion
    }
}
