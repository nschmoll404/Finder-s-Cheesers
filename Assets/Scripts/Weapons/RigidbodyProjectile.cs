using UnityEngine;

namespace FindersCheesers
{
    /// <summary>
    /// A physics-based projectile that uses Unity's Rigidbody component.
    /// Affected by gravity and physics collisions.
    /// </summary>
    [AddComponentMenu("Finders Cheesers/Weapons/RigidbodyProjectile")]
    [RequireComponent(typeof(Rigidbody))]
    public class RigidbodyProjectile : BaseProjectile
    {
        #region Settings

        [Header("Rigidbody Settings")]
        [Tooltip("Whether to use gravity")]
        [SerializeField]
        private bool useGravity = true;

        [Tooltip("Mass of the projectile")]
        [SerializeField]
        private float mass = 1f;

        [Tooltip("Drag value for the projectile")]
        [SerializeField]
        private float drag = 0f;

        [Tooltip("Angular drag value for the projectile")]
        [SerializeField]
        private float angularDrag = 0.05f;

        [Tooltip("Collision detection mode")]
        [SerializeField]
        private CollisionDetectionMode collisionDetection = CollisionDetectionMode.ContinuousDynamic;

        [Tooltip("Whether to rotate the projectile to face its velocity direction")]
        [SerializeField]
        private bool rotateToVelocity = true;

        [Tooltip("Maximum lifetime of the projectile (in seconds)")]
        [SerializeField]
        private float maxLifetime = 10f;

        #endregion

        #region Component References

        private Rigidbody rb;

        #endregion

        #region State Variables

        private float lifetimeTimer;

        #endregion

        #region Unity Lifecycle

        protected override void Awake()
        {
            base.Awake();
            rb = GetComponent<Rigidbody>();

            if (rb == null)
            {
                Debug.LogError("[RigidbodyProjectile] Rigidbody component not found!");
                return;
            }

            // Configure rigidbody
            rb.useGravity = useGravity;
            rb.mass = mass;
            rb.linearDamping = drag;
            rb.angularDamping = angularDrag;
            rb.collisionDetectionMode = collisionDetection;
            rb.isKinematic = true; // Start as kinematic, will enable on fire
        }

        protected override void OnFired()
        {
            base.OnFired();

            // Enable physics
            rb.isKinematic = false;
            rb.linearVelocity = velocity;

            // Reset lifetime timer
            lifetimeTimer = 0f;
        }

        private void Update()
        {
            if (!IsActive || !isInitialized)
            {
                return;
            }

            // Update lifetime
            lifetimeTimer += Time.deltaTime;
            if (lifetimeTimer >= maxLifetime)
            {
                if (debugMode)
                {
                    Debug.Log("[RigidbodyProjectile] Max lifetime reached, deactivating");
                }
                Deactivate();
                return;
            }

            // Rotate to face velocity if enabled
            if (rotateToVelocity && rb.linearVelocity.sqrMagnitude > 0.1f)
            {
                transform.rotation = Quaternion.LookRotation(rb.linearVelocity);
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }

        #endregion

        #region Collision Handling

        private void OnCollisionEnter(Collision collision)
        {
            if (!IsActive || !isInitialized)
            {
                return;
            }

            // Check if the hit collider is on a valid layer
            if (!IsValidHitLayer(collision.collider))
            {
                return;
            }

            // Handle the hit
            OnHitObject(collision.collider);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsActive || !isInitialized)
            {
                return;
            }

            // Check if the hit collider is on a valid layer
            if (!IsValidHitLayer(other))
            {
                return;
            }

            // Handle the hit
            OnHitObject(other);
        }

        #endregion

        #region Public API

        /// <summary>
        /// Sets the velocity of the projectile directly.
        /// </summary>
        /// <param name="newVelocity">The new velocity vector.</param>
        public void SetVelocity(Vector3 newVelocity)
        {
            if (rb != null)
            {
                rb.linearVelocity = newVelocity;
            }
        }

        /// <summary>
        /// Applies an impulse force to the projectile.
        /// </summary>
        /// <param name="impulse">The impulse vector.</param>
        public void ApplyImpulse(Vector3 impulse)
        {
            if (rb != null && !rb.isKinematic)
            {
                rb.AddForce(impulse, ForceMode.Impulse);
            }
        }

        #endregion

        #region Public Methods

        public override void Deactivate()
        {
            // Disable physics when deactivated
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            base.Deactivate();
        }

        #endregion
    }
}
