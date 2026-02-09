using UnityEngine;

namespace FindersCheesers
{
    /// <summary>
    /// A trigger zone that instantly kills any GameObject with a Health component.
    /// Attach this to a GameObject with a Collider set to "Is Trigger".
    /// </summary>
    [AddComponentMenu("Finders Cheesers/Triggers/Kill Zone")]
    [RequireComponent(typeof(Collider))]
    public class KillZone : MonoBehaviour
    {
        #region Settings

        [Header("Settings")]
        [Tooltip("Whether to show debug information in the console")]
        [SerializeField]
        private bool debugMode = false;

        [Tooltip("Layer mask to filter which objects can be killed by this zone")]
        [SerializeField]
        private LayerMask targetLayers = -1;

        [Tooltip("Whether to destroy the kill zone after it kills something")]
        [SerializeField]
        private bool destroyOnKill = false;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Ensure the collider is set as a trigger
            Collider col = GetComponent<Collider>();
            if (col != null && !col.isTrigger)
            {
                col.isTrigger = true;
                if (debugMode)
                {
                    Debug.LogWarning($"[KillZone] {gameObject.name} collider was not set as trigger. Auto-enabled.");
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            // Check if the object is in the target layers
            if (!IsInTargetLayers(other.gameObject))
            {
                return;
            }

            // Try to get the Health component
            Health health = other.GetComponent<Health>();
            
            if (health != null)
            {
                // Instantly kill the target by dealing damage equal to its max health
                if (!health.IsDead)
                {
                    health.TakeDamage(health.MaxHealth);
                    
                    if (debugMode)
                    {
                        Debug.Log($"[KillZone] {other.gameObject.name} was killed by {gameObject.name}");
                    }

                    // Destroy this kill zone if configured
                    if (destroyOnKill)
                    {
                        Destroy(gameObject);
                    }
                }
            }
            else if (debugMode)
            {
                Debug.Log($"[KillZone] {other.gameObject.name} entered {gameObject.name} but has no Health component");
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Checks if a GameObject is in the target layers.
        /// </summary>
        private bool IsInTargetLayers(GameObject obj)
        {
            return (targetLayers.value & (1 << obj.layer)) != 0;
        }

        #endregion

        #region Editor

        private void OnDrawGizmos()
        {
            // Draw a red wireframe to visualize the kill zone
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
                
                if (col is BoxCollider boxCol)
                {
                    Gizmos.matrix = transform.localToWorldMatrix;
                    Gizmos.DrawWireCube(boxCol.center, boxCol.size);
                }
                else if (col is SphereCollider sphereCol)
                {
                    Gizmos.matrix = transform.localToWorldMatrix;
                    Gizmos.DrawWireSphere(sphereCol.center, sphereCol.radius);
                }
                else if (col is CapsuleCollider capsuleCol)
                {
                    Gizmos.matrix = transform.localToWorldMatrix;
                    // Draw a simple wireframe for capsule
                    Vector3 center = capsuleCol.center;
                    float height = capsuleCol.height;
                    float radius = capsuleCol.radius;
                    
                    // Draw two spheres at the ends
                    Gizmos.DrawWireSphere(center + Vector3.up * (height / 2 - radius), radius);
                    Gizmos.DrawWireSphere(center - Vector3.up * (height / 2 - radius), radius);
                }
            }
        }

        private void OnValidate()
        {
            // Ensure the collider is set as a trigger in editor
            Collider col = GetComponent<Collider>();
            if (col != null && !col.isTrigger)
            {
                col.isTrigger = true;
            }
        }

        #endregion
    }
}
