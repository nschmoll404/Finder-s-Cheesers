using UnityEngine;

namespace FindersCheesers
{
    /// <summary>
    /// Abstract base class for projectiles.
    /// Provides common functionality for all projectile types.
    /// </summary>
    public abstract class BaseProjectile : MonoBehaviour, IProjectile
    {
        #region Settings

        [Header("Projectile Settings")]
        [Tooltip("Layers that this projectile can hit")]
        [SerializeField]
        protected LayerMask hitLayers = -1;

        [Tooltip("Whether to destroy the projectile on impact")]
        [SerializeField]
        protected bool destroyOnImpact = true;

        [Tooltip("Delay before destroying the projectile after impact (in seconds)")]
        [SerializeField]
        protected float destroyDelay = 0f;

        [Tooltip("Whether to show debug information in the console")]
        [SerializeField]
        protected bool debugMode = false;

        #endregion

        #region Events

        /// <summary>
        /// Event fired when the projectile hits something.
        /// </summary>
        public event System.Action<Collider> OnHit;

        /// <summary>
        /// Event fired when the projectile is destroyed.
        /// </summary>
        public event System.Action OnDestroyed;

        #endregion

        #region Properties

        /// <summary>
        /// Gets whether the projectile is currently active/flying.
        /// </summary>
        public bool IsActive { get; protected set; }

        /// <summary>
        /// Gets the damage dealt by this projectile.
        /// </summary>
        public float Damage { get; protected set; }

        /// <summary>
        /// Gets the owner/creator of this projectile (who fired it).
        /// </summary>
        public GameObject Owner { get; protected set; }

        #endregion

        #region Protected Fields

        protected Vector3 velocity;
        protected float speed;
        protected bool isInitialized = false;

        #endregion

        #region Unity Lifecycle

        protected virtual void Awake()
        {
            IsActive = false;
        }

        protected virtual void OnDestroy()
        {
            OnDestroyed?.Invoke();
        }

        #endregion

        #region IProjectile Implementation

        /// <summary>
        /// Initializes and fires the projectile.
        /// </summary>
        public virtual void Fire(Vector3 origin, Vector3 direction, float speed, float damage, GameObject owner)
        {
            if (debugMode)
            {
                Debug.Log($"[BaseProjectile] Firing projectile from {origin} in direction {direction} with speed {speed}");
            }

            // Set properties
            Damage = damage;
            Owner = owner;
            this.speed = speed;
            velocity = direction * speed;

            // Set position and rotation
            transform.position = origin;
            transform.rotation = Quaternion.LookRotation(direction);

            // Mark as active and initialized
            IsActive = true;
            isInitialized = true;

            // Enable the projectile
            gameObject.SetActive(true);

            // Call subclass-specific fire logic
            OnFired();
        }

        /// <summary>
        /// Deactivates/destroys the projectile.
        /// </summary>
        public virtual void Deactivate()
        {
            if (debugMode)
            {
                Debug.Log("[BaseProjectile] Deactivating projectile");
            }

            IsActive = false;
            isInitialized = false;

            if (destroyOnImpact)
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
            else
            {
                gameObject.SetActive(false);
            }
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Called when the projectile is fired. Override in subclasses for specific behavior.
        /// </summary>
        protected virtual void OnFired()
        {
            // Override in subclasses
        }

        /// <summary>
        /// Called when the projectile hits something.
        /// </summary>
        /// <param name="hitCollider">The collider that was hit.</param>
        protected virtual void OnHitObject(Collider hitCollider)
        {
            if (debugMode)
            {
                Debug.Log($"[BaseProjectile] Hit {hitCollider.name}");
            }

            // Deal damage if the hit object has a Health component
            DealDamage(hitCollider);

            // Fire the hit event
            OnHit?.Invoke(hitCollider);

            // Deactivate the projectile
            Deactivate();
        }

        /// <summary>
        /// Deals damage to the hit object if it has a Health component.
        /// </summary>
        /// <param name="hitCollider">The collider that was hit.</param>
        protected virtual void DealDamage(Collider hitCollider)
        {
            // Don't damage the owner
            if (Owner != null && hitCollider.gameObject == Owner)
            {
                return;
            }

            // Try to get Health component
            Health health = hitCollider.GetComponent<Health>();
            
            if (health != null)
            {
                health.TakeDamage(Damage);

                if (debugMode)
                {
                    Debug.Log($"[BaseProjectile] Dealt {Damage} damage to {hitCollider.name}");
                }
            }
            else if (debugMode)
            {
                Debug.LogWarning($"[BaseProjectile] Hit object {hitCollider.name} has no Health component");
            }
        }

        /// <summary>
        /// Checks if a collider is on a valid hit layer.
        /// </summary>
        /// <param name="collider">The collider to check.</param>
        /// <returns>True if the collider is on a valid hit layer.</returns>
        protected bool IsValidHitLayer(Collider collider)
        {
            return (hitLayers.value & (1 << collider.gameObject.layer)) != 0;
        }

        #endregion
    }
}
