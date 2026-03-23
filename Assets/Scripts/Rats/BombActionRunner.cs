using UnityEngine;
using Actions;

namespace FindersCheesers
{
    /// <summary>
    /// A specialized action runner for bombs that runs actions when the fuse is lit or when the bomb explodes.
    /// Extends ThrowableActionRunner to add bomb-specific events.
    /// Attach this alongside a Bomb component to execute sequences of actions.
    /// </summary>
    [RequireComponent(typeof(Bomb))]
    [AddComponentMenu("Finders Cheesers/Bomb Action Runner")]
    public class BombActionRunner : ThrowableActionRunner
    {
        [Header("Fuse Lit Actions")]
        [Tooltip("The action runner that will execute actions when the bomb fuse is lit.")]
        [SerializeField] private ActionRunner _fuseLitActionRunner;

        [Header("Explosion Actions")]
        [Tooltip("The action runner that will execute actions when the bomb explodes.")]
        [SerializeField] private ActionRunner _explosionActionRunner;

        [Header("Settings")]
        [Tooltip("Should the fuse lit action runner be cleared after running?")]
        [SerializeField] private bool _clearFuseLitAfterRun = false;

        [Tooltip("Should the explosion action runner be cleared after running?")]
        [SerializeField] private bool _clearExplosionAfterRun = false;

        // Cached reference to the bomb
        private Bomb _bomb;
        private bool _hasSubscribedToBombEvents = false;

        /// <summary>
        /// Gets or sets the fuse lit action runner for this component.
        /// </summary>
        public ActionRunner FuseLitActionRunner
        {
            get => _fuseLitActionRunner;
            set => _fuseLitActionRunner = value;
        }

        /// <summary>
        /// Gets or sets the explosion action runner for this component.
        /// </summary>
        public ActionRunner ExplosionActionRunner
        {
            get => _explosionActionRunner;
            set => _explosionActionRunner = value;
        }

        /// <summary>
        /// Gets or sets whether the fuse lit action runner should be cleared after running.
        /// </summary>
        public bool ClearFuseLitAfterRun
        {
            get => _clearFuseLitAfterRun;
            set => _clearFuseLitAfterRun = value;
        }

        /// <summary>
        /// Gets or sets whether the explosion action runner should be cleared after running.
        /// </summary>
        public bool ClearExplosionAfterRun
        {
            get => _clearExplosionAfterRun;
            set => _clearExplosionAfterRun = value;
        }

        private void Awake()
        {
            // Find the Bomb component on this GameObject
            _bomb = GetComponent<Bomb>();

            if (_bomb == null)
            {
                Debug.LogError($"[BombActionRunner] No Bomb component found on {gameObject.name}. Bomb-specific actions will not run.");
            }
        }

        private void OnEnable()
        {
            // Subscribe to bomb-specific events
            if (_bomb != null && !_hasSubscribedToBombEvents)
            {
                _bomb.OnFuseLit += HandleFuseLit;
                _bomb.OnExploded += HandleExploded;
                _hasSubscribedToBombEvents = true;
            }
        }

        private void OnDisable()
        {
            // Unsubscribe from bomb-specific events
            if (_bomb != null && _hasSubscribedToBombEvents)
            {
                _bomb.OnFuseLit -= HandleFuseLit;
                _bomb.OnExploded -= HandleExploded;
                _hasSubscribedToBombEvents = false;
            }
        }

        /// <summary>
        /// Handles the fuse lit event from the Bomb component.
        /// </summary>
        private void HandleFuseLit()
        {
            // Check if we have a fuse lit action runner with actions
            if (_fuseLitActionRunner == null || _fuseLitActionRunner.IsEmpty())
            {
                return;
            }

            // Create a context object with information about the fuse being lit
            var context = new FuseLitContext
            {
                Bomb = _bomb,
                GameObject = gameObject,
                ActionRunner = this
            };

            // Run all fuse lit actions with the context
            _fuseLitActionRunner.RunAll(context);

            // Clear actions if configured to do so
            if (_clearFuseLitAfterRun)
            {
                _fuseLitActionRunner.ClearActions();
            }
        }

        /// <summary>
        /// Handles the exploded event from the Bomb component.
        /// </summary>
        /// <param name="explosionPosition">The position where the explosion occurred.</param>
        private void HandleExploded(Vector3 explosionPosition)
        {
            // Check if we have an explosion action runner with actions
            if (_explosionActionRunner == null || _explosionActionRunner.IsEmpty())
            {
                return;
            }

            // Create a context object with information about the explosion
            var context = new ExplosionContext
            {
                Bomb = _bomb,
                GameObject = gameObject,
                ExplosionPosition = explosionPosition,
                ActionRunner = this
            };

            // Run all explosion actions with the context
            _explosionActionRunner.RunAll(context);

            // Clear actions if configured to do so
            if (_clearExplosionAfterRun)
            {
                _explosionActionRunner.ClearActions();
            }
        }

        /// <summary>
        /// Context object passed to actions when the bomb fuse is lit.
        /// </summary>
        public class FuseLitContext
        {
            /// <summary>
            /// The bomb whose fuse was lit.
            /// </summary>
            public Bomb Bomb { get; set; }

            /// <summary>
            /// The GameObject containing the bomb.
            /// </summary>
            public GameObject GameObject { get; set; }

            /// <summary>
            /// The action runner that is executing actions.
            /// </summary>
            public BombActionRunner ActionRunner { get; set; }
        }

        /// <summary>
        /// Context object passed to actions when the bomb explodes.
        /// </summary>
        public class ExplosionContext
        {
            /// <summary>
            /// The bomb that exploded.
            /// </summary>
            public Bomb Bomb { get; set; }

            /// <summary>
            /// The GameObject containing the bomb.
            /// </summary>
            public GameObject GameObject { get; set; }

            /// <summary>
            /// The position where the explosion occurred.
            /// </summary>
            public Vector3 ExplosionPosition { get; set; }

            /// <summary>
            /// The action runner that is executing actions.
            /// </summary>
            public BombActionRunner ActionRunner { get; set; }
        }
    }
}
