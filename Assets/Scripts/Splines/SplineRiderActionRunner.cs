using UnityEngine;
using Actions;

namespace FindersCheesers.Splines
{
    /// <summary>
    /// A MonoBehaviour component that finds a SplineRider on the same GameObject
    /// and runs actions when the rider is riding along a spline or when the ride ends.
    /// Attach this alongside a SplineRider component to execute a sequence of actions during the ride.
    /// </summary>
    public class SplineRiderActionRunner : MonoBehaviour
    {
        [Header("Ride Actions")]
        [Tooltip("The action runner that will execute actions while riding.")]
        [SerializeField] private ActionRunner _rideActionRunner;

        [Header("Ride End Actions")]
        [Tooltip("The action runner that will execute actions when the ride ends.")]
        [SerializeField] private ActionRunner _rideEndActionRunner;

        [Header("Settings")]
        [Tooltip("Should ride actions run continuously while riding?")]
        [SerializeField] private bool _runContinuously = true;

        [Tooltip("Minimum time between ride action executions (in seconds). Only used if runContinuously is true.")]
        [SerializeField] private float _executionInterval = 0f;

        [Tooltip("Should the action runners be cleared after the ride ends?")]
        [SerializeField] private bool _clearAfterRide = false;

        // Cached reference to the spline rider
        private SplineRider _splineRider;

        // Timer for execution interval
        private float _timeSinceLastExecution;

        /// <summary>
        /// Gets or sets the ride action runner for this component.
        /// </summary>
        public ActionRunner RideActionRunner
        {
            get => _rideActionRunner;
            set => _rideActionRunner = value;
        }

        /// <summary>
        /// Gets or sets the ride end action runner for this component.
        /// </summary>
        public ActionRunner RideEndActionRunner
        {
            get => _rideEndActionRunner;
            set => _rideEndActionRunner = value;
        }

        /// <summary>
        /// Gets or sets whether ride actions should run continuously while riding.
        /// </summary>
        public bool RunContinuously
        {
            get => _runContinuously;
            set => _runContinuously = value;
        }

        /// <summary>
        /// Gets or sets the minimum time between ride action executions.
        /// </summary>
        public float ExecutionInterval
        {
            get => _executionInterval;
            set => _executionInterval = Mathf.Max(0f, value);
        }

        /// <summary>
        /// Gets or sets whether the action runners should be cleared after the ride ends.
        /// </summary>
        public bool ClearAfterRide
        {
            get => _clearAfterRide;
            set => _clearAfterRide = value;
        }

        private void Awake()
        {
            // Find the SplineRider component on this GameObject
            _splineRider = GetComponent<SplineRider>();

            if (_splineRider == null)
            {
                Debug.LogWarning($"[SplineRiderActionRunner] No SplineRider component found on {gameObject.name}. Actions will not run.");
            }
        }

        private void OnEnable()
        {
            // Subscribe to the spline rider's events
            if (_splineRider != null)
            {
                _splineRider.OnRide += HandleRide;
                _splineRider.OnRideEnd += HandleRideEnd;
            }
        }

        private void OnDisable()
        {
            // Unsubscribe from the spline rider's events
            if (_splineRider != null)
            {
                _splineRider.OnRide -= HandleRide;
                _splineRider.OnRideEnd -= HandleRideEnd;
            }
        }

        /// <summary>
        /// Handles the ride event from the SplineRider component.
        /// </summary>
        private void HandleRide()
        {
            // Check if we have a ride action runner with actions
            if (_rideActionRunner == null || _rideActionRunner.IsEmpty())
            {
                return;
            }

            // Check execution interval
            if (_runContinuously && _executionInterval > 0f)
            {
                _timeSinceLastExecution += Time.deltaTime;
                if (_timeSinceLastExecution < _executionInterval)
                {
                    return;
                }
                _timeSinceLastExecution = 0f;
            }

            // Create a context object with information about the ride
            var context = new RideContext
            {
                SplineRider = _splineRider,
                CurrentProgress = _splineRider.CurrentProgress,
                IsRiding = _splineRider.IsRiding,
                ActionRunner = this
            };

            // Run all ride actions with the context
            _rideActionRunner.RunAll(context);
        }

        /// <summary>
        /// Handles the ride end event from the SplineRider component.
        /// </summary>
        private void HandleRideEnd()
        {
            // Run ride end actions if available
            if (_rideEndActionRunner != null && !_rideEndActionRunner.IsEmpty())
            {
                // Create a context object with information about the ride end
                var context = new RideContext
                {
                    SplineRider = _splineRider,
                    CurrentProgress = _splineRider.CurrentProgress,
                    IsRiding = _splineRider.IsRiding,
                    ActionRunner = this
                };

                // Run all ride end actions with the context
                _rideEndActionRunner.RunAll(context);
            }

            // Clear actions if configured to do so
            if (_clearAfterRide)
            {
                if (_rideActionRunner != null)
                {
                    _rideActionRunner.ClearActions();
                }
                if (_rideEndActionRunner != null)
                {
                    _rideEndActionRunner.ClearActions();
                }
            }

            // Reset the execution timer
            _timeSinceLastExecution = 0f;
        }

        /// <summary>
        /// Context object passed to actions during the ride.
        /// </summary>
        public class RideContext
        {
            /// <summary>
            /// The spline rider that is currently riding.
            /// </summary>
            public SplineRider SplineRider { get; set; }

            /// <summary>
            /// The current progress along the spline (0-1).
            /// </summary>
            public float CurrentProgress { get; set; }

            /// <summary>
            /// Whether the rider is currently riding.
            /// </summary>
            public bool IsRiding { get; set; }

            /// <summary>
            /// The action runner that is executing the actions.
            /// </summary>
            public SplineRiderActionRunner ActionRunner { get; set; }
        }
    }
}
