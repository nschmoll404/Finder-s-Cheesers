using UnityEngine;

namespace FindersCheesers
{
    /// <summary>
    /// A component that handles the throwable behavior of King Rat.
    /// Manages the throw arc animation and physics state during flight.
    /// </summary>
    [AddComponentMenu("Finders Cheesers/King Rat Throwable")]
    public class KingRatThrowable : MonoBehaviour, IThrowable
    {
        [Header("Throw Settings")]
        [Tooltip("Duration of the throw animation")]
        [SerializeField]
        private float throwDuration = 1f;

        [Tooltip("Launch speed for the throw arc")]
        [SerializeField]
        private float launchSpeed = 10f;

        [Tooltip("Enable GroundPounder during throw")]
        [SerializeField]
        private bool enableGroundPounderDuringThrow = true;

        [Header("Debug")]
        [Tooltip("Show debug information in the console")]
        [SerializeField]
        private bool debugMode = false;

        // Component references
        private Rigidbody rb;
        private GroundPounder groundPounder;

        // Current state
        private bool isThrowing;
        private Vector3 throwStartPosition;
        private Vector3 throwEndPosition;
        private float throwTimer;
        private bool wasKinematicBeforePickup;

        /// <summary>
        /// Event fired when the King Rat is thrown.
        /// </summary>
        public event System.Action<Vector3> OnThrown;

        /// <summary>
        /// Event fired when the King Rat lands.
        /// </summary>
        public event System.Action<Vector3> OnLanded;

        /// <summary>
        /// Gets whether the King Rat is currently being thrown.
        /// </summary>
        public bool IsThrowing => isThrowing;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            groundPounder = GetComponent<GroundPounder>();
            
            // Store original kinematic state before any pickup
            if (rb != null)
            {
                wasKinematicBeforePickup = rb.isKinematic;
            }

            // Ensure GroundPounder is initially disabled if we're going to control it
            if (groundPounder != null && enableGroundPounderDuringThrow)
            {
                groundPounder.enabled = false;
            }
        }

        private void Update()
        {
            // Update throw animation
            if (isThrowing)
            {
                UpdateThrowAnimation();
            }
        }

        /// <summary>
        /// Starts a throw to the specified destination.
        /// </summary>
        /// <param name="destination">The target destination for the throw.</param>
        /// <param name="speed">The launch speed for the arc.</param>
        public void ThrowTo(Vector3 destination, float speed)
        {
            if (isThrowing)
            {
                if (debugMode)
                {
                    Debug.LogWarning("[KingRatThrowable] Already being thrown!");
                }
                return;
            }

            // Make kinematic for the throw arc and disable collision detection
            // to prevent triggering OnTriggerEnter/OnTriggerStay events during flight
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.detectCollisions = false;
            }

            // Enable GroundPounder during throw if the option is enabled
            if (groundPounder != null && enableGroundPounderDuringThrow)
            {
                groundPounder.enabled = true;
            }

            // Initialize throw state
            isThrowing = true;
            throwStartPosition = transform.position;
            throwEndPosition = destination;
            throwTimer = 0f;
            launchSpeed = speed;

            // Fire event
            OnThrown?.Invoke(destination);

            if (debugMode)
            {
                Debug.Log($"[KingRatThrowable] Throwing to {destination} with speed {speed:F2}");
            }
        }

        /// <summary>
        /// Updates the throw animation.
        /// </summary>
        private void UpdateThrowAnimation()
        {
            throwTimer += Time.deltaTime;
            float t = Mathf.Clamp01(throwTimer / throwDuration);

            // Calculate arc position
            Vector3 position = CalculateArcPosition(throwStartPosition, throwEndPosition, t);
            transform.position = position;

            // Check if throw is complete
            if (t >= 1f)
            {
                FinishThrowAnimation();
            }
        }

        /// <summary>
        /// Calculates the position along the arc at time t (0-1).
        /// Uses a quadratic Bezier curve to ensure the arc always completes from start to end.
        /// </summary>
        private Vector3 CalculateArcPosition(Vector3 start, Vector3 end, float t)
        {
            // Calculate horizontal distance
            float distance = Vector3.Distance(new Vector3(start.x, 0, start.z), new Vector3(end.x, 0, end.z));

            // Calculate control point for the arc (midpoint horizontally, elevated vertically)
            Vector3 midPoint = Vector3.Lerp(start, end, 0.5f);
            float arcHeight = Mathf.Max(distance * 0.3f, launchSpeed * 0.15f);
            Vector3 controlPoint = new Vector3(midPoint.x, Mathf.Max(start.y, end.y) + arcHeight, midPoint.z);

            // Quadratic Bezier formula: (1-t)² * P0 + 2(1-t)t * P1 + t² * P2
            float oneMinusT = 1f - t;
            Vector3 point = (oneMinusT * oneMinusT * start) +
                            (2f * oneMinusT * t * controlPoint) +
                            (t * t * end);

            return point;
        }

        /// <summary>
        /// Finishes the throw animation.
        /// </summary>
        private void FinishThrowAnimation()
        {
            // Re-enable physics and collision detection after throw completes
            // Restore to original state from before pickup
            if (rb != null)
            {
                rb.isKinematic = wasKinematicBeforePickup;
                rb.detectCollisions = true;
            }

            // Disable GroundPounder after throw completes if the option is enabled
            if (groundPounder != null && enableGroundPounderDuringThrow)
            {
                groundPounder.enabled = false;
            }

            // Fire event
            OnLanded?.Invoke(throwEndPosition);

            if (debugMode)
            {
                Debug.Log($"[KingRatThrowable] Landed at {throwEndPosition}");
            }

            isThrowing = false;
        }

        /// <summary>
        /// Cancels the current throw (if any).
        /// </summary>
        public void CancelThrow()
        {
            if (!isThrowing)
            {
                return;
            }

            // Re-enable physics and collision detection
            // Restore to original state from before pickup
            if (rb != null)
            {
                rb.isKinematic = wasKinematicBeforePickup;
                rb.detectCollisions = true;
            }

            // Disable GroundPounder when throw is cancelled if the option is enabled
            if (groundPounder != null && enableGroundPounderDuringThrow)
            {
                groundPounder.enabled = false;
            }

            isThrowing = false;

            if (debugMode)
            {
                Debug.Log("[KingRatThrowable] Throw cancelled");
            }
        }

        /// <summary>
        /// Simply drops the King Rat, allowing it to fall with physics.
        /// This is used when releasing the King Rat without throwing.
        /// </summary>
        public void Drop()
        {
            // Cancel any ongoing throw
            if (isThrowing)
            {
                CancelThrow();
            }

            // Restore original kinematic state from before pickup
            // If it wasn't kinematic before pickup, physics will be enabled so it falls
            if (rb != null)
            {
                rb.isKinematic = wasKinematicBeforePickup;
            }

            if (debugMode)
            {
                Debug.Log("[KingRatThrowable] King Rat dropped");
            }
        }

        /// <summary>
        /// Sets the throw duration.
        /// </summary>
        public void SetThrowDuration(float duration)
        {
            throwDuration = Mathf.Max(0.1f, duration);
        }

        /// <summary>
        /// Gets the current throw duration.
        /// </summary>
        public float GetThrowDuration()
        {
            return throwDuration;
        }
    }
}
