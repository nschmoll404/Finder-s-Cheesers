using UnityEngine;

namespace FindersCheesers
{
    /// <summary>
    /// A component that detects colliders beneath a GameObject and damages them
    /// when the GameObject is moving downward with sufficient velocity.
    /// </summary>
    [AddComponentMenu("Finders Cheesers/Ground Pounder")]
    public class GroundPounder : MonoBehaviour
    {
        #region Settings

        [Header("Detection Settings")]
        [Tooltip("The radius of the sphere cast used to detect colliders beneath the GameObject")]
        [SerializeField]
        private float detectionRadius = 1f;

        [Tooltip("The distance below the GameObject to check for colliders")]
        [SerializeField]
        private float detectionDistance = 2f;

        [Tooltip("Local-space offset applied to the sphere cast origin (e.g. to shift detection to the character's feet)")]
        [SerializeField]
        private Vector3 detectionOffset = Vector3.zero;

        [Tooltip("The minimum downward velocity required to trigger ground pound damage")]
        [SerializeField]
        private float minDownwardVelocity = 5f;

        [Tooltip("Layer mask to filter which colliders can be damaged")]
        [SerializeField]
        private LayerMask damageableLayers = -1;

        [Header("Damage Settings")]
        [Tooltip("The amount of damage to apply to detected colliders")]
        [SerializeField]
        private float damageAmount = 25f;

        [Tooltip("Whether to apply damage only once per ground pound (true) or continuously while moving down (false)")]
        [SerializeField]
        private bool singleHitPerPound = true;

        [Tooltip("Whether to show debug information in the console and scene view")]
        [SerializeField]
        private bool debugMode = false;

        #endregion

        #region Private Fields

        private Vector3 _previousPosition;
        private Vector3 _currentVelocity;
        private bool _hasHitThisPound = false;

        #endregion

        #region Properties

        /// <summary>
        /// Gets whether the GameObject is currently moving downward with sufficient velocity.
        /// </summary>
        public bool IsGroundPounding => _currentVelocity.y < -minDownwardVelocity;

        /// <summary>
        /// Gets the current calculated velocity of the GameObject.
        /// </summary>
        public Vector3 CurrentVelocity => _currentVelocity;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _previousPosition = transform.position;
        }

        private void Update()
        {
            // Calculate velocity based on transform position change
            Vector3 currentPosition = transform.position;
            _currentVelocity = (currentPosition - _previousPosition) / Time.deltaTime;
            _previousPosition = currentPosition;

            // Check if moving downward with sufficient velocity
            if (_currentVelocity.y < -minDownwardVelocity)
            {
                // Reset hit tracking if we're starting a new ground pound
                if (!_hasHitThisPound || !singleHitPerPound)
                {
                    PerformGroundPound();
                }
            }
            else
            {
                // Reset hit tracking when not moving downward
                _hasHitThisPound = false;
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Performs the ground pound by detecting colliders beneath the GameObject and applying damage.
        /// </summary>
        private void PerformGroundPound()
        {
            // Cast a sphere downward from the offset position
            Vector3 origin = transform.position + transform.TransformDirection(detectionOffset);
            Vector3 direction = Vector3.down;
            
            RaycastHit[] hits = Physics.SphereCastAll(
                origin,
                detectionRadius,
                direction,
                detectionDistance,
                damageableLayers
            );

            if (hits.Length > 0)
            {
                if (debugMode)
                {
                    Debug.Log($"[GroundPounder] {gameObject.name} detected {hits.Length} collider(s) beneath it.");
                }

                // Apply damage to each detected collider that has a Health component
                int damagedCount = 0;
                foreach (RaycastHit hit in hits)
                {
                    // Skip if we hit our own collider
                    if (hit.collider.gameObject == gameObject)
                    {
                        continue;
                    }

                    Health health = hit.collider.GetComponent<Health>();
                    if (health != null)
                    {
                        health.TakeDamage(damageAmount);
                        damagedCount++;

                        if (debugMode)
                        {
                            Debug.Log($"[GroundPounder] {gameObject.name} dealt {damageAmount} damage to {hit.collider.gameObject.name}");
                        }
                    }
                }

                if (damagedCount > 0)
                {
                    _hasHitThisPound = true;

                    if (debugMode)
                    {
                        Debug.Log($"[GroundPounder] {gameObject.name} damaged {damagedCount} target(s).");
                    }
                }
            }
        }

        #endregion

        #region Editor

        private void OnDrawGizmosSelected()
        {
            // Draw the detection sphere in the scene view when the GameObject is selected
            Vector3 origin = transform.position + transform.TransformDirection(detectionOffset);
            Vector3 bottom = origin + Vector3.down * detectionDistance;

            // Draw the sphere at the bottom of the detection range
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
            Gizmos.DrawWireSphere(bottom, detectionRadius);

            // Draw a line from the GameObject to the bottom of detection
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
            Gizmos.DrawLine(origin, bottom);

            // Draw a semi-transparent sphere at the bottom
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.1f);
            Gizmos.DrawSphere(bottom, detectionRadius);
        }

        private void OnValidate()
        {
            // Ensure values are valid in editor
            detectionRadius = Mathf.Max(0.1f, detectionRadius);
            detectionDistance = Mathf.Max(0.1f, detectionDistance);
            minDownwardVelocity = Mathf.Max(0.1f, minDownwardVelocity);
            damageAmount = Mathf.Max(0f, damageAmount);
        }

        #endregion
    }
}
