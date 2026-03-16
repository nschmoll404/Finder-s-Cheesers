using UnityEngine;
using Actions;

namespace FindersCheesers
{
    /// <summary>
    /// A MonoBehaviour component that finds a DetectionZone on the same GameObject
    /// and runs actions when the zone is triggered, untriggered, detected, or undetected.
    /// Attach this alongside a DetectionZone component to execute sequences of actions on detection events.
    /// </summary>
    [AddComponentMenu("Finders Cheesers/Physics/Detection Zone Action Runner")]
    public class DetectionZoneActionRunner : MonoBehaviour
    {
        [Header("Action Settings")]
        [Tooltip("The action runner that will execute actions when the zone is triggered (count >= trigger amount).")]
        [SerializeField] private ActionRunner _onTriggeredActions;

        [Tooltip("The action runner that will execute actions when the zone is untriggered (count < trigger amount).")]
        [SerializeField] private ActionRunner _onUntriggeredActions;

        [Tooltip("The action runner that will execute actions when the zone is detected (count > 0).")]
        [SerializeField] private ActionRunner _onDetectedActions;

        [Tooltip("The action runner that will execute actions when the zone is undetected (count == 0).")]
        [SerializeField] private ActionRunner _onUndetectedActions;

        [Header("Settings")]
        [Tooltip("Should action runners be cleared after running?")]
        [SerializeField] private bool _clearAfterRun = false;

        [Tooltip("If true, actions will only run once per event type. If false, actions can run multiple times.")]
        [SerializeField] private bool _runOnce = false;

        [Tooltip("Whether to show debug information in the console")]
        [SerializeField] private bool _debugMode = false;

        // Cached reference to the detection zone
        private DetectionZone _detectionZone;

        // Track which events have already run (for runOnce option)
        private bool _triggeredHasRun = false;
        private bool _untriggeredHasRun = false;
        private bool _detectedHasRun = false;
        private bool _undetectedHasRun = false;

        /// <summary>
        /// Gets or sets the action runner for triggered events.
        /// </summary>
        public ActionRunner OnTriggeredActions
        {
            get => _onTriggeredActions;
            set => _onTriggeredActions = value;
        }

        /// <summary>
        /// Gets or sets the action runner for untriggered events.
        /// </summary>
        public ActionRunner OnUntriggeredActions
        {
            get => _onUntriggeredActions;
            set => _onUntriggeredActions = value;
        }

        /// <summary>
        /// Gets or sets the action runner for detected events.
        /// </summary>
        public ActionRunner OnDetectedActions
        {
            get => _onDetectedActions;
            set => _onDetectedActions = value;
        }

        /// <summary>
        /// Gets or sets the action runner for undetected events.
        /// </summary>
        public ActionRunner OnUndetectedActions
        {
            get => _onUndetectedActions;
            set => _onUndetectedActions = value;
        }

        /// <summary>
        /// Gets or sets whether action runners should be cleared after running.
        /// </summary>
        public bool ClearAfterRun
        {
            get => _clearAfterRun;
            set => _clearAfterRun = value;
        }

        /// <summary>
        /// Gets or sets whether actions should only run once per event type.
        /// </summary>
        public bool RunOnce
        {
            get => _runOnce;
            set => _runOnce = value;
        }

        /// <summary>
        /// Resets the run-once flags, allowing actions to run again.
        /// </summary>
        public void ResetRunFlags()
        {
            _triggeredHasRun = false;
            _untriggeredHasRun = false;
            _detectedHasRun = false;
            _undetectedHasRun = false;

            if (_debugMode)
            {
                Debug.Log($"[DetectionZoneActionRunner] Run flags reset on {gameObject.name}");
            }
        }

        private void Awake()
        {
            // Find DetectionZone component on this GameObject
            _detectionZone = GetComponent<DetectionZone>();

            if (_detectionZone == null)
            {
                Debug.LogWarning($"[DetectionZoneActionRunner] No DetectionZone component found on {gameObject.name}. Actions will not run.");
            }
        }

        private void OnEnable()
        {
            // Subscribe to detection zone's events
            if (_detectionZone != null)
            {
                _detectionZone.OnTriggered += HandleTriggered;
                _detectionZone.OnUntriggered += HandleUntriggered;
                _detectionZone.OnObjectEntered += HandleObjectEntered;
                _detectionZone.OnObjectExited += HandleObjectExited;
            }
        }

        private void OnDisable()
        {
            // Unsubscribe from detection zone's events
            if (_detectionZone != null)
            {
                _detectionZone.OnTriggered -= HandleTriggered;
                _detectionZone.OnUntriggered -= HandleUntriggered;
                _detectionZone.OnObjectEntered -= HandleObjectEntered;
                _detectionZone.OnObjectExited -= HandleObjectExited;
            }
        }

        /// <summary>
        /// Handles the triggered event from DetectionZone component.
        /// </summary>
        private void HandleTriggered()
        {
            if (_runOnce && _triggeredHasRun)
            {
                return;
            }

            if (_debugMode)
            {
                Debug.Log($"[DetectionZoneActionRunner] Triggered on {gameObject.name}");
            }

            RunActions(_onTriggeredActions, "Triggered");
            _triggeredHasRun = true;
        }

        /// <summary>
        /// Handles the untriggered event from DetectionZone component.
        /// </summary>
        private void HandleUntriggered()
        {
            if (_runOnce && _untriggeredHasRun)
            {
                return;
            }

            if (_debugMode)
            {
                Debug.Log($"[DetectionZoneActionRunner] Untriggered on {gameObject.name}");
            }

            RunActions(_onUntriggeredActions, "Untriggered");
            _untriggeredHasRun = true;
        }

        /// <summary>
        /// Handles the object entered event from DetectionZone component.
        /// This fires when the zone becomes detected (count > 0).
        /// </summary>
        private void HandleObjectEntered(GameObject obj)
        {
            // Only fire detected event when count goes from 0 to > 0
            if (_detectionZone.DetectedCount > 1)
            {
                return;
            }

            if (_runOnce && _detectedHasRun)
            {
                return;
            }

            if (_debugMode)
            {
                Debug.Log($"[DetectionZoneActionRunner] Detected on {gameObject.name}");
            }

            RunActions(_onDetectedActions, "Detected");
            _detectedHasRun = true;
        }

        /// <summary>
        /// Handles the object exited event from DetectionZone component.
        /// This fires when the zone becomes undetected (count == 0).
        /// </summary>
        private void HandleObjectExited(GameObject obj)
        {
            // Only fire undetected event when count goes from > 0 to 0
            if (_detectionZone.DetectedCount > 0)
            {
                return;
            }

            if (_runOnce && _undetectedHasRun)
            {
                return;
            }

            if (_debugMode)
            {
                Debug.Log($"[DetectionZoneActionRunner] Undetected on {gameObject.name}");
            }

            RunActions(_onUndetectedActions, "Undetected");
            _undetectedHasRun = true;
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
                    Debug.LogWarning($"[DetectionZoneActionRunner] ActionRunner is not set for {eventType} on {gameObject.name}.");
                }
                return;
            }

            // Create a context object with information about the detection zone event
            var context = new DetectionZoneContext
            {
                DetectionZone = _detectionZone,
                EventType = eventType,
                ActionRunner = this,
                DetectedCount = _detectionZone.DetectedCount,
                TriggerAmount = _detectionZone.TriggerAmount,
                IsTriggered = _detectionZone.IsTriggered,
                IsDetected = _detectionZone.IsDetected
            };

            // Run all actions with the context
            actionRunner.RunAll(context);

            if (_debugMode)
            {
                Debug.Log($"[DetectionZoneActionRunner] Executed {actionRunner.ActionCount} actions for {eventType} on {gameObject.name}");
            }

            // Clear actions if configured to do so
            if (_clearAfterRun)
            {
                actionRunner.ClearActions();
            }
        }

        /// <summary>
        /// Context object passed to actions during detection zone events.
        /// </summary>
        public class DetectionZoneContext
        {
            /// <summary>
            /// The detection zone that triggered the event.
            /// </summary>
            public DetectionZone DetectionZone { get; set; }

            /// <summary>
            /// The type of event that occurred ("Triggered", "Untriggered", "Detected", or "Undetected").
            /// </summary>
            public string EventType { get; set; }

            /// <summary>
            /// The action runner that is executing the actions.
            /// </summary>
            public DetectionZoneActionRunner ActionRunner { get; set; }

            /// <summary>
            /// The current number of detected objects in the zone.
            /// </summary>
            public int DetectedCount { get; set; }

            /// <summary>
            /// The trigger amount value.
            /// </summary>
            public int TriggerAmount { get; set; }

            /// <summary>
            /// Whether the zone is currently triggered.
            /// </summary>
            public bool IsTriggered { get; set; }

            /// <summary>
            /// Whether the zone is currently detected (has any objects).
            /// </summary>
            public bool IsDetected { get; set; }
        }
    }
}
