using UnityEngine;
using Actions;

namespace FindersCheesers
{
    /// <summary>
    /// A MonoBehaviour component that finds a WeightZone on the same GameObject
    /// and runs actions when the weight threshold is reached or lost.
    /// Attach this alongside a WeightZone component to execute sequences of actions on threshold events.
    /// </summary>
    [AddComponentMenu("Finders Cheesers/Physics/Weight Zone Action Runner")]
    public class WeightZoneActionRunner : MonoBehaviour
    {
        [Header("Action Settings")]
        [Tooltip("The action runner that will execute actions when threshold is reached.")]
        [SerializeField] private ActionRunner _onThresholdReachedActions;

        [Tooltip("The action runner that will execute actions when threshold is lost.")]
        [SerializeField] private ActionRunner _onThresholdLostActions;

        [Header("Settings")]
        [Tooltip("Should action runners be cleared after running?")]
        [SerializeField] private bool _clearAfterRun = false;

        [Tooltip("Whether to show debug information in the console")]
        [SerializeField] private bool _debugMode = false;

        // Cached reference to the weight zone
        private WeightZone _weightZone;

        /// <summary>
        /// Gets or sets the action runner for threshold reached events.
        /// </summary>
        public ActionRunner OnThresholdReachedActions
        {
            get => _onThresholdReachedActions;
            set => _onThresholdReachedActions = value;
        }

        /// <summary>
        /// Gets or sets the action runner for threshold lost events.
        /// </summary>
        public ActionRunner OnThresholdLostActions
        {
            get => _onThresholdLostActions;
            set => _onThresholdLostActions = value;
        }

        /// <summary>
        /// Gets or sets whether action runners should be cleared after running.
        /// </summary>
        public bool ClearAfterRun
        {
            get => _clearAfterRun;
            set => _clearAfterRun = value;
        }

        private void Awake()
        {
            // Find WeightZone component on this GameObject
            _weightZone = GetComponent<WeightZone>();

            if (_weightZone == null)
            {
                Debug.LogWarning($"[WeightZoneActionRunner] No WeightZone component found on {gameObject.name}. Actions will not run.");
            }
        }

        private void OnEnable()
        {
            // Subscribe to weight zone's events
            if (_weightZone != null)
            {
                _weightZone.OnThresholdReached += HandleThresholdReached;
                _weightZone.OnThresholdLost += HandleThresholdLost;
            }
        }

        private void OnDisable()
        {
            // Unsubscribe from weight zone's events
            if (_weightZone != null)
            {
                _weightZone.OnThresholdReached -= HandleThresholdReached;
                _weightZone.OnThresholdLost -= HandleThresholdLost;
            }
        }

        /// <summary>
        /// Handles the threshold reached event from WeightZone component.
        /// </summary>
        private void HandleThresholdReached()
        {
            if (_debugMode)
            {
                Debug.Log($"[WeightZoneActionRunner] Threshold reached on {gameObject.name}");
            }

            RunActions(_onThresholdReachedActions, "Threshold Reached");
        }

        /// <summary>
        /// Handles the threshold lost event from WeightZone component.
        /// </summary>
        private void HandleThresholdLost()
        {
            if (_debugMode)
            {
                Debug.Log($"[WeightZoneActionRunner] Threshold lost on {gameObject.name}");
            }

            RunActions(_onThresholdLostActions, "Threshold Lost");
        }

        /// <summary>
        /// Executes actions from the specified action runner.
        /// </summary>
        /// <param name="actionRunner">The action runner to execute.</param>
        /// <param name="eventType">Description of the event type for logging.</param>
        private void RunActions(ActionRunner actionRunner, string eventType)
        {
            // Check if we have an action runner with actions
            if (actionRunner == null || actionRunner.IsEmpty())
            {
                if (actionRunner == null && _debugMode)
                {
                    Debug.LogWarning($"[WeightZoneActionRunner] ActionRunner is not set for {eventType} on {gameObject.name}.");
                }
                return;
            }

            // Create a context object with information about the weight zone event
            var context = new WeightZoneContext
            {
                WeightZone = _weightZone,
                EventType = eventType,
                ActionRunner = this,
                TotalWeight = _weightZone.TotalWeight,
                ThresholdWeight = _weightZone.ThresholdWeight,
                ObjectCount = _weightZone.ObjectCount
            };

            // Run all actions with the context
            actionRunner.RunAll(context);

            if (_debugMode)
            {
                Debug.Log($"[WeightZoneActionRunner] Executed {actionRunner.ActionCount} actions for {eventType} on {gameObject.name}");
            }

            // Clear actions if configured to do so
            if (_clearAfterRun)
            {
                actionRunner.ClearActions();
            }
        }

        /// <summary>
        /// Context object passed to actions during weight zone events.
        /// </summary>
        public class WeightZoneContext
        {
            /// <summary>
            /// The weight zone that triggered the event.
            /// </summary>
            public WeightZone WeightZone { get; set; }

            /// <summary>
            /// The type of event that occurred ("Threshold Reached" or "Threshold Lost").
            /// </summary>
            public string EventType { get; set; }

            /// <summary>
            /// The action runner that is executing the actions.
            /// </summary>
            public WeightZoneActionRunner ActionRunner { get; set; }

            /// <summary>
            /// The current total weight in the zone.
            /// </summary>
            public float TotalWeight { get; set; }

            /// <summary>
            /// The threshold weight value.
            /// </summary>
            public float ThresholdWeight { get; set; }

            /// <summary>
            /// The number of weighted objects in the zone.
            /// </summary>
            public int ObjectCount { get; set; }
        }
    }
}
