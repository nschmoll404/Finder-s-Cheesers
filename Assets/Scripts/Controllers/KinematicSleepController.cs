using UnityEngine;

namespace FindersCheesers
{
    /// <summary>
    /// Monitors a Rigidbody's position and sets it to kinematic
    /// if it hasn't moved beyond a threshold for a configurable duration.
    /// Useful for freezing objects that have come to rest naturally.
    /// </summary>
    [AddComponentMenu("Finders Cheesers/Kinematic Sleep Controller")]
    [RequireComponent(typeof(Rigidbody))]
    public class KinematicSleepController : MonoBehaviour
    {
        [Header("Detection Settings")]
        [Tooltip("How long (in seconds) the object must be stationary before becoming kinematic")]
        [SerializeField]
        private float sleepTime = 3f;

        [Tooltip("Maximum position change per frame to still be considered stationary")]
        [SerializeField]
        private float positionThreshold = 0.01f;

        [Header("Rotation Detection")]
        [Tooltip("Also check if rotation hasn't changed before sleeping")]
        [SerializeField]
        private bool checkRotation = false;

        [Tooltip("Maximum rotation change per frame (in degrees) to still be considered stationary")]
        [SerializeField]
        private float rotationThreshold = 0.1f;

        [Header("Debug")]
        [Tooltip("Show debug information in the console")]
        [SerializeField]
        private bool debugMode = false;

        private Rigidbody _rigidbody;
        private Vector3 lastPosition;
        private Quaternion lastRotation;
        private float stationaryTimer = 0f;
        private bool isKinematicSet = false;

        /// <summary>
        /// Gets whether the Rigidbody has been set to kinematic by this controller.
        /// </summary>
        public bool IsKinematicSet => isKinematicSet;

        /// <summary>
        /// Gets how long the object has been stationary in seconds.
        /// </summary>
        public float StationaryTime => stationaryTimer;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
        }

        private void OnEnable()
        {
            // Reset tracking when re-enabled
            ResetTracking();
        }

        private void FixedUpdate()
        {
            if (_rigidbody == null || isKinematicSet)
            {
                return;
            }

            // Already kinematic? No work to do.
            if (_rigidbody.isKinematic)
            {
                isKinematicSet = true;
                return;
            }

            bool positionChanged = Vector3.Distance(_rigidbody.position, lastPosition) > positionThreshold;
            bool rotationChanged = checkRotation && Quaternion.Angle(_rigidbody.rotation, lastRotation) > rotationThreshold;

            if (positionChanged || rotationChanged)
            {
                // Object is still moving — reset timer
                stationaryTimer = 0f;
                lastPosition = _rigidbody.position;
                lastRotation = _rigidbody.rotation;
            }
            else
            {
                // Object is stationary — accumulate time
                stationaryTimer += Time.fixedDeltaTime;

                if (stationaryTimer >= sleepTime)
                {
                    _rigidbody.isKinematic = true;
                    isKinematicSet = true;

                    if (debugMode)
                    {
                        Debug.Log(
                            $"[KinematicSleepController] '{gameObject.name}' set to kinematic after {sleepTime:F1}s of no movement.",
                            this
                        );
                    }
                }
            }
        }

        /// <summary>
        /// Manually wakes the object by setting isKinematic back to false
        /// and resetting the stationary timer.
        /// Call this when you want the object to become physics-driven again.
        /// </summary>
        /// <param name="applyVelocity">If true, clears any existing velocity after waking.</param>
        public void Wake(bool applyVelocity = false)
        {
            if (_rigidbody == null)
            {
                return;
            }

            _rigidbody.isKinematic = false;
            isKinematicSet = false;
            ResetTracking();

            if (!applyVelocity)
            {
                _rigidbody.linearVelocity = Vector3.zero;
                _rigidbody.angularVelocity = Vector3.zero;
            }

            if (debugMode)
            {
                Debug.Log($"[KinematicSleepController] '{gameObject.name}' woken up.", this);
            }
        }

        /// <summary>
        /// Resets the position/rotation tracking and stationary timer
        /// without changing the kinematic state.
        /// </summary>
        public void ResetTracking()
        {
            if (_rigidbody != null)
            {
                lastPosition = _rigidbody.position;
                lastRotation = _rigidbody.rotation;
            }
            else
            {
                lastPosition = transform.position;
                lastRotation = transform.rotation;
            }

            stationaryTimer = 0f;
        }

        /// <summary>
        /// Sets the sleep time duration.
        /// </summary>
        /// <param name="time">Time in seconds before the object becomes kinematic.</param>
        public void SetSleepTime(float time)
        {
            sleepTime = Mathf.Max(0f, time);
        }

        /// <summary>
        /// Sets the position threshold for movement detection.
        /// </summary>
        /// <param name="threshold">Minimum position change to be considered moving.</param>
        public void SetPositionThreshold(float threshold)
        {
            positionThreshold = Mathf.Max(0f, threshold);
        }

        private void Reset()
        {
            // Set default values when component is first added
            sleepTime = 3f;
            positionThreshold = 0.01f;
            checkRotation = false;
            rotationThreshold = 0.1f;
        }
    }
}
