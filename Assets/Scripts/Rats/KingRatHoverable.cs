using UnityEngine;
using Actions;

namespace FindersCheesers
{
    /// <summary>
    /// A MonoBehaviour component that detects when the KingRatHandler's throw target
    /// position is hovering over this object's collider while the handler is holding a throwable.
    /// Fires ActionRunner events on hover enter and hover exit.
    /// Attach this alongside a Collider component to define the hoverable area.
    /// </summary>
    [AddComponentMenu("Finders Cheesers/King Rat Hoverable")]
    public class KingRatHoverable : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Action Settings")]
        [Tooltip("The action runner that will execute actions when the throw target starts hovering over this object.")]
        [SerializeField] private ActionRunner _onHoverEnterActions;

        [Tooltip("The action runner that will execute actions when the throw target stops hovering over this object.")]
        [SerializeField] private ActionRunner _onHoverExitActions;

        [Header("References")]
        [Tooltip("Reference to the KingRatHandler. If not assigned, it will be found automatically.")]
        [SerializeField] private KingRatHandler _kingRatHandler;

        [Header("Detection Settings")]
        [Tooltip("The collider used to define the hoverable area. If not assigned, uses the first Collider on this GameObject.")]
        [SerializeField] private Collider _hoverCollider;

        [Tooltip("Height offset added to the target position when checking hover. Useful for objects that sit above the ground.")]
        [SerializeField] private float _targetHeightTolerance = 2f;

        [Header("Behaviour")]
        [Tooltip("Should action runners be cleared after running?")]
        [SerializeField] private bool _clearAfterRun = false;

        [Tooltip("If true, actions will only run once per event type. If false, actions can run multiple times.")]
        [SerializeField] private bool _runOnce = false;

        [Tooltip("Whether to show debug information in the console")]
        [SerializeField] private bool _debugMode = false;

        #endregion

        #region State

        private bool _isHovered = false;
        private bool _hoverEnterHasRun = false;
        private bool _hoverExitHasRun = false;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the action runner for hover enter events.
        /// </summary>
        public ActionRunner OnHoverEnterActions
        {
            get => _onHoverEnterActions;
            set => _onHoverEnterActions = value;
        }

        /// <summary>
        /// Gets or sets the action runner for hover exit events.
        /// </summary>
        public ActionRunner OnHoverExitActions
        {
            get => _onHoverExitActions;
            set => _onHoverExitActions = value;
        }

        /// <summary>
        /// Gets whether the throw target is currently hovering over this object.
        /// </summary>
        public bool IsHovered => _isHovered;

        /// <summary>
        /// Gets or sets the KingRatHandler reference.
        /// </summary>
        public KingRatHandler KingRatHandler
        {
            get => _kingRatHandler;
            set => _kingRatHandler = value;
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

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Get collider if not assigned
            if (_hoverCollider == null)
            {
                _hoverCollider = GetComponent<Collider>();

                if (_hoverCollider == null)
                {
                    Debug.LogWarning($"[KingRatHoverable] No Collider component found on {gameObject.name}. Hover detection will not work.");
                }
            }

            // Find KingRatHandler if not assigned
            if (_kingRatHandler == null)
            {
                _kingRatHandler = FindFirstObjectByType<KingRatHandler>();

                if (_kingRatHandler == null)
                {
                    Debug.LogWarning($"[KingRatHoverable] No KingRatHandler found in the scene. Hover detection will not work on {gameObject.name}.");
                }
            }
        }

        private void Update()
        {
            if (_kingRatHandler == null || _hoverCollider == null)
            {
                return;
            }

            // Only check hover when the handler is grabbing a throwable
            if (!_kingRatHandler.IsGrabbing)
            {
                // If we were hovered and the handler stopped grabbing, fire exit
                if (_isHovered)
                {
                    HandleHoverExit();
                }
                return;
            }

            // Get the target position from the handler
            Vector3? targetPos = _kingRatHandler.GetTargetPosition();
            if (!targetPos.HasValue)
            {
                if (_isHovered)
                {
                    HandleHoverExit();
                }
                return;
            }

            // Check if the target position is within the collider bounds (with height tolerance)
            bool isWithinBounds = IsTargetWithinBounds(targetPos.Value);

            if (isWithinBounds && !_isHovered)
            {
                HandleHoverEnter();
            }
            else if (!isWithinBounds && _isHovered)
            {
                HandleHoverExit();
            }
        }

        #endregion

        #region Detection Methods

        /// <summary>
        /// Checks if the target position is within the collider's bounds with height tolerance.
        /// </summary>
        /// <param name="targetPosition">The throw target position to check.</param>
        /// <returns>True if the target is within bounds, false otherwise.</returns>
        private bool IsTargetWithinBounds(Vector3 targetPosition)
        {
            if (_hoverCollider == null)
            {
                return false;
            }

            Bounds bounds = _hoverCollider.bounds;

            // Check X and Z within bounds
            bool withinXZ = targetPosition.x >= bounds.min.x && targetPosition.x <= bounds.max.x &&
                            targetPosition.z >= bounds.min.z && targetPosition.z <= bounds.max.z;

            // Check Y with tolerance (target can be slightly below or above the bounds)
            bool withinY = targetPosition.y >= (bounds.min.y - _targetHeightTolerance) &&
                           targetPosition.y <= (bounds.max.y + _targetHeightTolerance);

            return withinXZ && withinY;
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handles the hover enter event. Fires the OnHoverEnter actions.
        /// </summary>
        private void HandleHoverEnter()
        {
            _isHovered = true;

            if (_runOnce && _hoverEnterHasRun)
            {
                return;
            }

            if (_debugMode)
            {
                Debug.Log($"[KingRatHoverable] Hover enter on {gameObject.name}");
            }

            RunActions(_onHoverEnterActions, "HoverEnter");
            _hoverEnterHasRun = true;
        }

        /// <summary>
        /// Handles the hover exit event. Fires the OnHoverExit actions.
        /// </summary>
        private void HandleHoverExit()
        {
            _isHovered = false;

            if (_runOnce && _hoverExitHasRun)
            {
                return;
            }

            if (_debugMode)
            {
                Debug.Log($"[KingRatHoverable] Hover exit on {gameObject.name}");
            }

            RunActions(_onHoverExitActions, "HoverExit");
            _hoverExitHasRun = true;
        }

        #endregion

        #region Action Execution

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
                    Debug.LogWarning($"[KingRatHoverable] ActionRunner is not set for {eventType} on {gameObject.name}.");
                }
                return;
            }

            // Create a context object with information about the hover event
            var context = new KingRatHoverableContext
            {
                Hoverable = this,
                EventType = eventType,
                ActionRunner = this,
                TargetPosition = _kingRatHandler.GetTargetPosition(),
                IsGrabbing = _kingRatHandler.IsGrabbing,
                IsThrowing = _kingRatHandler.IsThrowing,
                GameObject = gameObject
            };

            // Run all actions with the context
            actionRunner.RunAll(context);

            if (_debugMode)
            {
                Debug.Log($"[KingRatHoverable] Executed {actionRunner.ActionCount} actions for {eventType} on {gameObject.name}");
            }

            // Clear actions if configured to do so
            if (_clearAfterRun)
            {
                actionRunner.ClearActions();
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Resets the run-once flags, allowing actions to run again.
        /// </summary>
        public void ResetRunFlags()
        {
            _hoverEnterHasRun = false;
            _hoverExitHasRun = false;

            if (_debugMode)
            {
                Debug.Log($"[KingRatHoverable] Run flags reset on {gameObject.name}");
            }
        }

        /// <summary>
        /// Forces the hover state to exit, firing the OnHoverExit actions if currently hovered.
        /// </summary>
        public void ForceExit()
        {
            if (_isHovered)
            {
                HandleHoverExit();
            }
        }

        #endregion

        #region Context

        /// <summary>
        /// Context object passed to actions during KingRatHoverable events.
        /// </summary>
        public class KingRatHoverableContext
        {
            /// <summary>
            /// The KingRatHoverable component that triggered the event.
            /// </summary>
            public KingRatHoverable Hoverable { get; set; }

            /// <summary>
            /// The type of event that occurred ("HoverEnter" or "HoverExit").
            /// </summary>
            public string EventType { get; set; }

            /// <summary>
            /// The action runner component that is executing the actions.
            /// </summary>
            public KingRatHoverable ActionRunner { get; set; }

            /// <summary>
            /// The current throw target position from the KingRatHandler, or null if not available.
            /// </summary>
            public Vector3? TargetPosition { get; set; }

            /// <summary>
            /// Whether the KingRatHandler is currently grabbing a throwable.
            /// </summary>
            public bool IsGrabbing { get; set; }

            /// <summary>
            /// Whether the KingRatHandler is currently throwing.
            /// </summary>
            public bool IsThrowing { get; set; }

            /// <summary>
            /// The GameObject this KingRatHoverable is attached to.
            /// </summary>
            public GameObject GameObject { get; set; }
        }

        #endregion
    }
}
