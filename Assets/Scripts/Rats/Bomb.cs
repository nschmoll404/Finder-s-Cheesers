using UnityEngine;

namespace FindersCheesers
{
    /// <summary>
    /// A throwable bomb component that explodes after a fuse countdown.
    /// When picked up, the fuse is lit and the countdown starts.
    /// On explosion, damages all Health components within the explosion radius.
    /// </summary>
    [AddComponentMenu("Finders Cheesers/Bomb")]
    [RequireComponent(typeof(ThrowableObject))]
    public class Bomb : MonoBehaviour
    {
        #region Settings

        [Header("Fuse Settings")]
        [Tooltip("Duration of the fuse countdown (in seconds)")]
        [SerializeField]
        private float fuseDuration = 3f;

        [Header("Explosion Settings")]
        [Tooltip("Radius of the explosion")]
        [SerializeField]
        private float explosionRadius = 5f;

        [Tooltip("Damage amount to deal to Health components within explosion radius")]
        [SerializeField]
        private float explosionDamage = 50f;

        [Tooltip("Layer mask for objects that can be damaged by the explosion")]
        [SerializeField]
        private LayerMask damageableLayers = -1;

        [Tooltip("Whether to destroy the bomb GameObject after explosion")]
        [SerializeField]
        private bool destroyAfterExplosion = true;

        [Tooltip("Delay before destroying the bomb GameObject after explosion (in seconds)")]
        [SerializeField]
        private float destroyDelay = 0.1f;

        [Header("Debug")]
        [Tooltip("Show debug information in the console")]
        [SerializeField]
        private bool debugMode = false;

        [Tooltip("Draw explosion radius in scene view")]
        [SerializeField]
        private bool drawExplosionRadius = true;

        #endregion

        #region Events

        /// <summary>
        /// Event fired when the fuse is lit (bomb is picked up).
        /// </summary>
        public event System.Action OnFuseLit;

        /// <summary>
        /// Event fired when the bomb explodes.
        /// </summary>
        public event System.Action<Vector3> OnExploded;

        #endregion

        #region Properties

        /// <summary>
        /// Gets whether the fuse is currently lit.
        /// </summary>
        public bool IsFuseLit { get; private set; }

        /// <summary>
        /// Gets whether the bomb has exploded.
        /// </summary>
        public bool HasExploded { get; private set; }

        /// <summary>
        /// Gets the remaining fuse time (0 if fuse is not lit or bomb has exploded).
        /// </summary>
        public float RemainingFuseTime { get; private set; }

        #endregion

        #region Private Fields

        // Component references
        private ThrowableObject throwableObject;

        // Fuse state
        private float fuseTimer;
        private bool isCountingDown;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            throwableObject = GetComponent<ThrowableObject>();

            if (throwableObject == null)
            {
                Debug.LogError("[Bomb] ThrowableObject component not found!");
            }
        }

        private void Update()
        {
            // Update fuse countdown
            if (isCountingDown)
            {
                UpdateFuseCountdown();
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Draw explosion radius
            if (drawExplosionRadius)
            {
                Gizmos.color = HasExploded ? Color.gray : (IsFuseLit ? Color.red : Color.yellow);
                Gizmos.DrawWireSphere(transform.position, explosionRadius);
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Lights the fuse and starts the countdown.
        /// Call this method when the bomb is picked up.
        /// </summary>
        public void StartFuse()
        {
            if (IsFuseLit)
            {
                if (debugMode)
                {
                    Debug.LogWarning("[Bomb] Fuse is already lit!");
                }
                return;
            }

            if (HasExploded)
            {
                if (debugMode)
                {
                    Debug.LogWarning("[Bomb] Bomb has already exploded!");
                }
                return;
            }

            // Start the fuse
            IsFuseLit = true;
            isCountingDown = true;
            fuseTimer = 0f;
            RemainingFuseTime = fuseDuration;

            // Fire event
            OnFuseLit?.Invoke();

            if (debugMode)
            {
                Debug.Log($"[Bomb] Fuse lit! Explosion in {fuseDuration:F2} seconds.");
            }
        }

        /// <summary>
        /// Manually triggers the explosion, regardless of fuse state.
        /// </summary>
        public void Explode()
        {
            if (HasExploded)
            {
                if (debugMode)
                {
                    Debug.LogWarning("[Bomb] Bomb has already exploded!");
                }
                return;
            }

            // Stop countdown
            isCountingDown = false;
            RemainingFuseTime = 0f;

            // Perform explosion
            PerformExplosion();
        }

        /// <summary>
        /// Cancels the fuse countdown without exploding.
        /// </summary>
        public void CancelFuse()
        {
            if (!IsFuseLit)
            {
                if (debugMode)
                {
                    Debug.LogWarning("[Bomb] Fuse is not lit!");
                }
                return;
            }

            isCountingDown = false;
            RemainingFuseTime = 0f;

            if (debugMode)
            {
                Debug.Log("[Bomb] Fuse cancelled.");
            }
        }

        /// <summary>
        /// Resets the bomb to its initial state.
        /// </summary>
        public void ResetBomb()
        {
            IsFuseLit = false;
            HasExploded = false;
            isCountingDown = false;
            fuseTimer = 0f;
            RemainingFuseTime = 0f;

            if (debugMode)
            {
                Debug.Log("[Bomb] Bomb reset.");
            }
        }

        /// <summary>
        /// Sets the fuse duration.
        /// </summary>
        /// <param name="duration">The new fuse duration in seconds.</param>
        public void SetFuseDuration(float duration)
        {
            fuseDuration = Mathf.Max(0.1f, duration);

            if (debugMode)
            {
                Debug.Log($"[Bomb] Fuse duration set to: {fuseDuration:F2} seconds.");
            }
        }

        /// <summary>
        /// Sets the explosion radius.
        /// </summary>
        /// <param name="radius">The new explosion radius.</param>
        public void SetExplosionRadius(float radius)
        {
            explosionRadius = Mathf.Max(0.1f, radius);

            if (debugMode)
            {
                Debug.Log($"[Bomb] Explosion radius set to: {explosionRadius:F2} units.");
            }
        }

        /// <summary>
        /// Sets the explosion damage.
        /// </summary>
        /// <param name="damage">The new explosion damage.</param>
        public void SetExplosionDamage(float damage)
        {
            explosionDamage = Mathf.Max(0f, damage);

            if (debugMode)
            {
                Debug.Log($"[Bomb] Explosion damage set to: {explosionDamage:F2}.");
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Updates the fuse countdown timer.
        /// </summary>
        private void UpdateFuseCountdown()
        {
            fuseTimer += Time.deltaTime;
            RemainingFuseTime = Mathf.Max(0f, fuseDuration - fuseTimer);

            // Check if fuse has burned out
            if (fuseTimer >= fuseDuration)
            {
                isCountingDown = false;
                PerformExplosion();
            }
        }

        /// <summary>
        /// Performs the explosion logic.
        /// </summary>
        private void PerformExplosion()
        {
            HasExploded = true;

            // Find all colliders within explosion radius
            Collider[] hitColliders = Physics.OverlapSphere(
                transform.position,
                explosionRadius,
                damageableLayers,
                QueryTriggerInteraction.Ignore
            );

            // Apply damage to all Health components
            int damagedObjects = 0;
            foreach (Collider collider in hitColliders)
            {
                Health health = collider.GetComponent<Health>();
                if (health != null)
                {
                    health.TakeDamage(explosionDamage);
                    damagedObjects++;

                    if (debugMode)
                    {
                        Debug.Log($"[Bomb] Damaged {collider.gameObject.name} for {explosionDamage:F2} damage.");
                    }
                }
            }

            // Fire event
            OnExploded?.Invoke(transform.position);

            if (debugMode)
            {
                Debug.Log($"[Bomb] EXPLODED! Damaged {damagedObjects} object(s) within {explosionRadius:F2} unit radius.");
            }

            // Destroy the bomb if configured
            if (destroyAfterExplosion)
            {
                if (destroyDelay > 0f)
                {
                    Destroy(gameObject, destroyDelay);
                }
                else
                {
                    Destroy(gameObject);
                }
            }
        }

        #endregion

        #region Editor

        private void OnValidate()
        {
            // Ensure values are valid in editor
            fuseDuration = Mathf.Max(0.1f, fuseDuration);
            explosionRadius = Mathf.Max(0.1f, explosionRadius);
            explosionDamage = Mathf.Max(0f, explosionDamage);
            destroyDelay = Mathf.Max(0f, destroyDelay);
        }

        #endregion
    }
}
