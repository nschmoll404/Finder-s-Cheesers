using UnityEngine;
using Actions;

namespace FindersCheesers
{
    /// <summary>
    /// A specialized action runner for match throwables that runs actions when the match is lit or extinguished.
    /// Extends ThrowableActionRunner to add match-specific events.
    /// Attach this alongside a MatchThrowable component to execute sequences of actions.
    /// </summary>
    [RequireComponent(typeof(MatchThrowable))]
    [AddComponentMenu("Finders Cheesers/Match Throwable Action Runner")]
    public class MatchThrowableActionRunner : ThrowableActionRunner
    {
        
        [Header("Match Lit Actions")]
        [Tooltip("The action runner that will execute actions when the match is lit.")]
        [SerializeField] private ActionRunner _matchLitActionRunner;

        [Header("Match Extinguished Actions")]
        [Tooltip("The action runner that will execute actions when the match is extinguished.")]
        [SerializeField] private ActionRunner _matchExtinguishedActionRunner;

        [Header("Settings")]
        [Tooltip("Should the match lit action runner be cleared after running?")]
        [SerializeField] private bool _clearMatchLitAfterRun = false;

        [Tooltip("Should the match extinguished action runner be cleared after running?")]
        [SerializeField] private bool _clearMatchExtinguishedAfterRun = false;

        // Cached reference to the match throwable
        private MatchThrowable _matchThrowable;
        private bool _hasSubscribedToMatchEvents = false;

        /// <summary>
        /// Gets or sets the match lit action runner for this component.
        /// </summary>
        public ActionRunner MatchLitActionRunner
        {
            get => _matchLitActionRunner;
            set => _matchLitActionRunner = value;
        }

        /// <summary>
        /// Gets or sets the match extinguished action runner for this component.
        /// </summary>
        public ActionRunner MatchExtinguishedActionRunner
        {
            get => _matchExtinguishedActionRunner;
            set => _matchExtinguishedActionRunner = value;
        }

        /// <summary>
        /// Gets or sets whether the match lit action runner should be cleared after running.
        /// </summary>
        public bool ClearMatchLitAfterRun
        {
            get => _clearMatchLitAfterRun;
            set => _clearMatchLitAfterRun = value;
        }

        /// <summary>
        /// Gets or sets whether the match extinguished action runner should be cleared after running.
        /// </summary>
        public bool ClearMatchExtinguishedAfterRun
        {
            get => _clearMatchExtinguishedAfterRun;
            set => _clearMatchExtinguishedAfterRun = value;
        }

        private void Awake()
        {
            // Find the MatchThrowable component on this GameObject
            _matchThrowable = GetComponent<MatchThrowable>();

            if (_matchThrowable == null)
            {
                Debug.LogError($"[MatchThrowableActionRunner] No MatchThrowable component found on {gameObject.name}. Match-specific actions will not run.");
            }
        }

        private new void OnEnable()
        {
            // Subscribe to match-specific events
            if (_matchThrowable != null && !_hasSubscribedToMatchEvents)
            {
                _matchThrowable.OnMatchLit += HandleMatchLit;
                _matchThrowable.OnMatchExtinguished += HandleMatchExtinguished;
                _hasSubscribedToMatchEvents = true;
            }
        }

        private new void OnDisable()
        {
            // Unsubscribe from match-specific events
            if (_matchThrowable != null && _hasSubscribedToMatchEvents)
            {
                _matchThrowable.OnMatchLit -= HandleMatchLit;
                _matchThrowable.OnMatchExtinguished -= HandleMatchExtinguished;
                _hasSubscribedToMatchEvents = false;
            }
        }

        /// <summary>
        /// Handles the match lit event from the MatchThrowable component.
        /// </summary>
        private void HandleMatchLit()
        {
            // Check if we have a match lit action runner with actions
            if (_matchLitActionRunner == null || _matchLitActionRunner.IsEmpty())
            {
                return;
            }

            // Create a context object with information about the match being lit
            var context = new MatchLitContext
            {
                MatchThrowable = _matchThrowable,
                GameObject = gameObject,
                ActionRunner = this
            };

            // Run all match lit actions with the context
            _matchLitActionRunner.RunAll(context);

            // Clear actions if configured to do so
            if (_clearMatchLitAfterRun)
            {
                _matchLitActionRunner.ClearActions();
            }
        }

        /// <summary>
        /// Handles the match extinguished event from the MatchThrowable component.
        /// </summary>
        private void HandleMatchExtinguished()
        {
            // Check if we have a match extinguished action runner with actions
            if (_matchExtinguishedActionRunner == null || _matchExtinguishedActionRunner.IsEmpty())
            {
                return;
            }

            // Create a context object with information about the match being extinguished
            var context = new MatchExtinguishedContext
            {
                MatchThrowable = _matchThrowable,
                GameObject = gameObject,
                ActionRunner = this
            };

            // Run all match extinguished actions with the context
            _matchExtinguishedActionRunner.RunAll(context);

            // Clear actions if configured to do so
            if (_clearMatchExtinguishedAfterRun)
            {
                _matchExtinguishedActionRunner.ClearActions();
            }
        }

        /// <summary>
        /// Context object passed to actions when the match is lit.
        /// </summary>
        public class MatchLitContext
        {
            /// <summary>
            /// The match throwable that was lit.
            /// </summary>
            public MatchThrowable MatchThrowable { get; set; }

            /// <summary>
            /// The GameObject containing the match.
            /// </summary>
            public GameObject GameObject { get; set; }

            /// <summary>
            /// The action runner that is executing actions.
            /// </summary>
            public MatchThrowableActionRunner ActionRunner { get; set; }
        }

        /// <summary>
        /// Context object passed to actions when the match is extinguished.
        /// </summary>
        public class MatchExtinguishedContext
        {
            /// <summary>
            /// The match throwable that was extinguished.
            /// </summary>
            public MatchThrowable MatchThrowable { get; set; }

            /// <summary>
            /// The GameObject containing the match.
            /// </summary>
            public GameObject GameObject { get; set; }

            /// <summary>
            /// The action runner that is executing actions.
            /// </summary>
            public MatchThrowableActionRunner ActionRunner { get; set; }
        }
    }
}
