using UnityEngine;

namespace FindersCheesers
{
    /// <summary>
    /// A bullet projectile that supports two detection modes:
    /// - HitScan: Uses raycasting for instant hit detection
    /// - TrailingScan: Uses spherecasting to detect hits as the bullet travels
    /// </summary>
    [AddComponentMenu("Finders Cheesers/Weapons/BulletProjectile")]
    public class BulletProjectile : BaseProjectile
    {
        #region Enums

        /// <summary>
        /// Detection mode for the bullet projectile.
        /// </summary>
        public enum DetectionMode
        {
            /// <summary>
            /// Uses raycasting to detect hits immediately.
            /// </summary>
            HitScan,

            /// <summary>
            /// Uses collision detection as the bullet moves through space.
            /// </summary>
            TrailingScan
        }

        #endregion

        #region Settings

        [Header("Bullet Settings")]
        [Tooltip("Detection mode: HitScan (raycast) or TrailingScan (collision-based)")]
        [SerializeField]
        private DetectionMode detectionMode = DetectionMode.HitScan;

        [Tooltip("Maximum range of the bullet")]
        [SerializeField]
        private float maxRange = 100f;

        [Tooltip("Minimum distance to travel before checking for hits (prevents self-collision)")]
        [SerializeField]
        private float minTravelDistance = 0.5f;

        [Tooltip("Whether to show a trail effect")]
        [SerializeField]
        private bool showTrail = true;

        [Tooltip("Trail renderer for visual effect")]
        [SerializeField]
        private TrailRenderer trailRenderer = null;

        [Tooltip("Line renderer for instant hit visualization")]
        [SerializeField]
        private LineRenderer lineRenderer = null;

        [Tooltip("Duration to show the hit line (in seconds)")]
        [SerializeField]
        private float hitLineDuration = 0.1f;

        #endregion

        #region State Variables

        private float distanceTraveled;
        private Vector3 startPosition;
        private bool isProcessingHit = false;
        private float hitLineTimer;

        #endregion

        #region Unity Lifecycle

        protected override void Awake()
        {
            base.Awake();

            // Get or create trail renderer
            if (trailRenderer == null)
            {
                trailRenderer = GetComponent<TrailRenderer>();
            }

            // Get or create line renderer
            if (lineRenderer == null)
            {
                lineRenderer = GetComponent<LineRenderer>();
            }

            // Disable by default
            if (trailRenderer != null)
            {
                trailRenderer.enabled = false;
            }

            if (lineRenderer != null)
            {
                lineRenderer.enabled = false;
            }
        }

        protected override void OnFired()
        {
            base.OnFired();

            // Initialize state
            startPosition = transform.position;
            distanceTraveled = 0f;
            isProcessingHit = false;
            hitLineTimer = 0f;

            // Enable trail if configured
            if (showTrail && trailRenderer != null)
            {
                trailRenderer.enabled = true;
                trailRenderer.Clear();
            }
        }

        private void Update()
        {
            if (!IsActive || !isInitialized)
            {
                return;
            }

            // If we're showing a hit line, just update the timer
            if (isProcessingHit && lineRenderer != null && lineRenderer.enabled)
            {
                hitLineTimer += Time.deltaTime;
                if (hitLineTimer >= hitLineDuration)
                {
                    lineRenderer.enabled = false;
                    Deactivate();
                }
                return;
            }

            // Move the bullet visually and check for hits
            if (!isProcessingHit)
            {
                MoveBullet();
                CheckForHit();
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Checks for hits as the bullet travels.
        /// </summary>
        private void CheckForHit()
        {
            // Only check for hits after minimum travel distance (prevents self-collision)
            if (distanceTraveled < minTravelDistance)
            {
                return;
            }

            Vector3 direction = velocity.normalized;
            Vector3 currentPos = transform.position;
            float remainingRange = maxRange - distanceTraveled;

            if (remainingRange <= 0f)
            {
                return;
            }

            RaycastHit hit;
            bool didHit = false;

            // Use different detection methods based on mode
            if (detectionMode == DetectionMode.HitScan)
            {
                // Perform raycast from current position
                didHit = Physics.Raycast(currentPos, direction, out hit, remainingRange, hitLayers);
            }
            else // TrailingScan
            {
                // Perform spherecast for trailing detection
                float moveDistance = speed * Time.deltaTime;
                didHit = Physics.SphereCast(currentPos, 0.1f, direction, out hit, moveDistance, hitLayers);
            }

            if (didHit)
            {
                // Check if hit collider is on a valid layer
                if (IsValidHitLayer(hit.collider))
                {
                    // Handle the hit
                    OnHitObject(hit.collider);

                    // Show hit line if configured
                    if (lineRenderer != null)
                    {
                        ShowHitLine(startPosition, hit.point);
                    }
                }
                else
                {
                    // Hit something but not on a valid layer, continue to max range
                    if (lineRenderer != null)
                    {
                        ShowHitLine(startPosition, startPosition + direction * maxRange);
                    }
                    isProcessingHit = true;
                }
            }
        }

        /// <summary>
        /// Moves the bullet visually.
        /// </summary>
        private void MoveBullet()
        {
            float moveDistance = speed * Time.deltaTime;
            distanceTraveled += moveDistance;

            // Move the bullet forward
            transform.position += velocity.normalized * moveDistance;

            // Check if we've reached max range
            if (distanceTraveled >= maxRange)
            {
                if (debugMode)
                {
                    Debug.Log("[BulletProjectile] Max range reached");
                }
                Deactivate();
            }
        }

        /// <summary>
        /// Shows a line from start to hit point.
        /// </summary>
        private void ShowHitLine(Vector3 start, Vector3 end)
        {
            if (lineRenderer == null)
            {
                return;
            }

            lineRenderer.enabled = true;
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, start);
            lineRenderer.SetPosition(1, end);
            hitLineTimer = 0f;
        }

        #endregion

        #region Public API

        /// <summary>
        /// Gets the hit point from the raycast (if any).
        /// </summary>
        /// <returns>The hit point, or Vector3.zero if no hit.</returns>
        public Vector3 GetHitPoint()
        {
            Vector3 direction = velocity.normalized;
            RaycastHit hit;

            if (Physics.Raycast(startPosition, direction, out hit, maxRange, hitLayers))
            {
                return hit.point;
            }

            return Vector3.zero;
        }

        /// <summary>
        /// Gets the hit collider from the raycast (if any).
        /// </summary>
        /// <returns>The hit collider, or null if no hit.</returns>
        public Collider GetHitCollider()
        {
            Vector3 direction = velocity.normalized;
            RaycastHit hit;

            if (Physics.Raycast(startPosition, direction, out hit, maxRange, hitLayers))
            {
                return hit.collider;
            }

            return null;
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
            // Disable visual effects
            if (trailRenderer != null)
            {
                trailRenderer.enabled = false;
                trailRenderer.Clear();
            }

            if (lineRenderer != null)
            {
                lineRenderer.enabled = false;
            }

            base.Deactivate();
        }

        #endregion
    }
}
