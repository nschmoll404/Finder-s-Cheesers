using UnityEngine;

namespace FindersCheesers
{
    /// <summary>
    /// A component that spawns throwable objects when interacted with.
    /// Similar to taking something out of a box - when the KingRatHandler interacts
    /// with this producer, it spawns a throwable that the handler can pick up.
    /// </summary>
    [AddComponentMenu("Finders Cheesers/Throwable Producer")]
    public class ThrowableProducer : MonoBehaviour
    {
        [Header("Throwable Prefab")]
        [Tooltip("The prefab to spawn when this producer is interacted with")]
        [SerializeField]
        private GameObject throwablePrefab;

        [Header("Spawn Settings")]
        [Tooltip("Offset from the producer's position where the throwable will spawn")]
        [SerializeField]
        private Vector3 spawnOffset = new Vector3(0f, 0.5f, 0.5f);

        [Tooltip("Whether to use a specific spawn point transform instead of offset")]
        [SerializeField]
        private bool useSpawnPoint = false;

        [Tooltip("Transform defining where the throwable should spawn (used if useSpawnPoint is true)")]
        [SerializeField]
        private Transform spawnPoint;

        [Header("Producer Settings")]
        [Tooltip("Whether this producer can be used multiple times")]
        [SerializeField]
        private bool reusable = true;

        [Tooltip("Cooldown time between spawns (in seconds)")]
        [SerializeField]
        private float spawnCooldown = 0.5f;

        [Tooltip("Maximum number of throwables this producer can spawn (0 = unlimited)")]
        [SerializeField]
        private int maxSpawns = 0;

        [Header("Visual Feedback")]
        [Tooltip("Show debug information in the console")]
        [SerializeField]
        private bool debugMode = false;

        [Tooltip("Visualize spawn point in Scene view")]
        [SerializeField]
        private bool visualizeSpawnPoint = true;

        // State
        private float lastSpawnTime;
        private int spawnCount;
        private Collider producerCollider;

        /// <summary>
        /// Event fired when a throwable is produced.
        /// </summary>
        public event System.Action<GameObject> OnThrowableProduced;

        /// <summary>
        /// Event fired when this producer is depleted (if maxSpawns > 0).
        /// </summary>
        public event System.Action OnProducerDepleted;

        /// <summary>
        /// Gets whether this producer can currently spawn a throwable.
        /// </summary>
        public bool CanSpawn => (reusable || spawnCount < maxSpawns || maxSpawns == 0) &&
                                Time.time >= lastSpawnTime + spawnCooldown;

        /// <summary>
        /// Gets the number of throwables spawned so far.
        /// </summary>
        public int SpawnCount => spawnCount;

        /// <summary>
        /// Gets whether this producer is depleted (has reached max spawns).
        /// </summary>
        public bool IsDepleted => maxSpawns > 0 && spawnCount >= maxSpawns;

        private void Awake()
        {
            producerCollider = GetComponent<Collider>();

            if (producerCollider == null)
            {
                producerCollider = gameObject.AddComponent<BoxCollider>();
                Debug.LogWarning("[ThrowableProducer] No collider found, added BoxCollider.");
            }
        }

        private void OnDrawGizmos()
        {
            if (visualizeSpawnPoint)
            {
                Vector3 spawnPosition = GetSpawnPosition();
                Gizmos.color = CanSpawn ? Color.green : Color.red;
                Gizmos.DrawWireSphere(spawnPosition, 0.2f);
                Gizmos.DrawLine(transform.position, spawnPosition);

                // Draw arrow pointing to spawn position
                Gizmos.color = Color.yellow;
                Vector3 direction = (spawnPosition - transform.position).normalized;
                Gizmos.DrawRay(spawnPosition, direction * 0.3f);
            }
        }

        /// <summary>
        /// Gets the spawn position for the throwable.
        /// </summary>
        public Vector3 GetSpawnPosition()
        {
            if (useSpawnPoint && spawnPoint != null)
            {
                return spawnPoint.position;
            }
            return transform.position + transform.TransformDirection(spawnOffset);
        }

        /// <summary>
        /// Spawns a throwable at the configured spawn position.
        /// </summary>
        /// <returns>The spawned throwable GameObject, or null if spawning failed.</returns>
        public GameObject SpawnThrowable()
        {
            if (throwablePrefab == null)
            {
                Debug.LogError("[ThrowableProducer] No throwable prefab assigned!");
                return null;
            }

            if (!CanSpawn)
            {
                if (debugMode)
                {
                    Debug.LogWarning("[ThrowableProducer] Cannot spawn throwable (cooldown or max spawns reached)!");
                }
                return null;
            }

            // Update spawn time and count
            lastSpawnTime = Time.time;
            spawnCount++;

            // Spawn the throwable
            Vector3 spawnPosition = GetSpawnPosition();
            GameObject spawnedThrowable = Instantiate(throwablePrefab, spawnPosition, Quaternion.identity);

            if (debugMode)
            {
                Debug.Log($"[ThrowableProducer] Spawned throwable at {spawnPosition} (Spawn #{spawnCount})");
            }

            // Fire event
            OnThrowableProduced?.Invoke(spawnedThrowable);

            // Check if producer is depleted
            if (IsDepleted)
            {
                OnProducerDepleted?.Invoke();

                if (debugMode)
                {
                    Debug.Log("[ThrowableProducer] Producer depleted!");
                }
            }

            return spawnedThrowable;
        }

        /// <summary>
        /// Resets the producer's spawn count and cooldown.
        /// Useful for respawning or resetting the producer.
        /// </summary>
        public void ResetProducer()
        {
            spawnCount = 0;
            lastSpawnTime = 0f;

            if (debugMode)
            {
                Debug.Log("[ThrowableProducer] Producer reset!");
            }
        }

        /// <summary>
        /// Sets the throwable prefab to spawn.
        /// </summary>
        public void SetThrowablePrefab(GameObject prefab)
        {
            throwablePrefab = prefab;
        }

        /// <summary>
        /// Sets the spawn offset.
        /// </summary>
        public void SetSpawnOffset(Vector3 offset)
        {
            spawnOffset = offset;
            useSpawnPoint = false;
        }

        /// <summary>
        /// Sets the spawn point transform.
        /// </summary>
        public void SetSpawnPoint(Transform point)
        {
            spawnPoint = point;
            useSpawnPoint = true;
        }

        /// <summary>
        /// Sets whether this producer is reusable.
        /// </summary>
        public void SetReusable(bool reusable)
        {
            this.reusable = reusable;
        }

        /// <summary>
        /// Sets the spawn cooldown time.
        /// </summary>
        public void SetSpawnCooldown(float cooldown)
        {
            spawnCooldown = Mathf.Max(0f, cooldown);
        }

        /// <summary>
        /// Sets the maximum number of spawns (0 = unlimited).
        /// </summary>
        public void SetMaxSpawns(int max)
        {
            maxSpawns = Mathf.Max(0, max);
        }
    }
}
