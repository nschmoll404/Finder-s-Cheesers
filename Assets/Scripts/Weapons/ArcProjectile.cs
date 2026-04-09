using UnityEngine;

namespace FindersCheesers
{
    [AddComponentMenu("Finders Cheesers/Weapons/ArcProjectile")]
    public class ArcProjectile : BaseProjectile
    {
        #region Settings

        [Header("Arc Settings")]
        [Tooltip("Gravity multiplier applied to the arc")]
        [SerializeField]
        private float gravityMultiplier = 1f;

        [Tooltip("Whether to rotate the projectile to face its velocity direction")]
        [SerializeField]
        private bool rotateToVelocity = true;

        [Tooltip("Extra full rotations per second around the forward axis while in flight")]
        [SerializeField]
        private float spinRotationsPerSecond = 0f;

        private float spinAngle;

        [Tooltip("Maximum lifetime of the projectile (in seconds)")]
        [SerializeField]
        private float maxLifetime = 10f;

        [Tooltip("Minimum distance to travel before checking for hits (prevents self-collision)")]
        [SerializeField]
        private float minTravelDistance = 0.5f;

        [Tooltip("Radius for spherecast collision detection")]
        [SerializeField]
        private float collisionRadius = 0.2f;

        [Header("Visual Settings")]
        [Tooltip("Whether to show a trail effect")]
        [SerializeField]
        private bool showTrail = true;

        [Tooltip("Trail renderer for visual effect")]
        [SerializeField]
        private TrailRenderer trailRenderer = null;

        #endregion

        #region State Variables

        private Vector3 currentVelocity;
        private float distanceTraveled;
        private Vector3 previousPosition;
        private float lifetimeTimer;
        private bool isProcessingHit = false;

        #endregion

        #region Unity Lifecycle

        protected override void Awake()
        {
            base.Awake();

            if (trailRenderer == null)
            {
                trailRenderer = GetComponent<TrailRenderer>();
            }

            if (trailRenderer != null)
            {
                trailRenderer.enabled = false;
            }
        }

        protected override void OnFired()
        {
            base.OnFired();

            float gravity = Mathf.Abs(Physics.gravity.y) * gravityMultiplier;
            currentVelocity = velocity;
            previousPosition = transform.position;
            distanceTraveled = 0f;
            lifetimeTimer = 0f;
            isProcessingHit = false;
            spinAngle = 0f;

            if (showTrail && trailRenderer != null)
            {
                trailRenderer.enabled = true;
                trailRenderer.Clear();
            }
        }

        private void FixedUpdate()
        {
            if (!IsActive || !isInitialized || isProcessingHit)
            {
                return;
            }

            lifetimeTimer += Time.fixedDeltaTime;

            if (lifetimeTimer >= maxLifetime)
            {
                if (debugMode)
                {
                    Debug.Log("[ArcProjectile] Max lifetime reached");
                }
                Deactivate();
                return;
            }

            MoveArc();
            CheckForCollision();
        }

        #endregion

        #region Private Methods

        private void MoveArc()
        {
            previousPosition = transform.position;

            Vector3 gravity = Physics.gravity * gravityMultiplier;
            currentVelocity += gravity * Time.fixedDeltaTime;

            Vector3 movement = currentVelocity * Time.fixedDeltaTime;
            transform.position += movement;

            distanceTraveled += movement.magnitude;

            if (rotateToVelocity && currentVelocity.sqrMagnitude > 0.1f)
            {
                transform.rotation = Quaternion.LookRotation(currentVelocity);
            }

            if (spinRotationsPerSecond > 0f)
            {
                spinAngle += spinRotationsPerSecond * 360f * Time.fixedDeltaTime;
                transform.Rotate(Vector3.right, spinRotationsPerSecond * 360f * Time.fixedDeltaTime, Space.Self);
            }
        }

        private void CheckForCollision()
        {
            if (distanceTraveled < minTravelDistance)
            {
                return;
            }

            Vector3 movement = transform.position - previousPosition;

            if (movement.sqrMagnitude <= 0f)
            {
                return;
            }

            Vector3 direction = movement.normalized;
            float moveDistance = movement.magnitude;

            RaycastHit hit;
            if (Physics.SphereCast(previousPosition, collisionRadius, direction, out hit, moveDistance, hitLayers))
            {
                if (IsValidHitLayer(hit.collider))
                {
                    transform.position = hit.point - direction * collisionRadius;
                    OnHitObject(hit.collider);
                }
            }
        }

        #endregion

        #region Protected Methods

        protected override void OnHitObject(Collider hitCollider)
        {
            isProcessingHit = true;
            base.OnHitObject(hitCollider);
        }

        public override void Deactivate()
        {
            if (trailRenderer != null)
            {
                trailRenderer.enabled = false;
                trailRenderer.Clear();
            }

            base.Deactivate();
        }

        #endregion

        #region Public API

        public Vector3 CurrentVelocity => currentVelocity;

        public void SetVelocity(Vector3 newVelocity)
        {
            currentVelocity = newVelocity;
            velocity = newVelocity;
        }

        #endregion
    }
}
