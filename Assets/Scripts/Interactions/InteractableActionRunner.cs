using UnityEngine;
using Actions;

namespace FindersCheesers
{
    /// <summary>
    /// A MonoBehaviour component that finds an IInteractable on the same GameObject
    /// and runs actions when the interactable is interacted with.
    /// Attach this alongside any IInteractable component to execute a sequence of actions on interaction.
    /// </summary>
    public class InteractableActionRunner : MonoBehaviour
    {
        [Header("Action Settings")]
        [Tooltip("The action runner that will execute actions when the interactable is interacted with.")]
        [SerializeField] private ActionRunner _actionRunner;

        [Header("Settings")]
        [Tooltip("Should actions run only on successful interactions?")]
        [SerializeField] private bool _runOnSuccessOnly = true;

        [Tooltip("Should the action runner be cleared after running?")]
        [SerializeField] private bool _clearAfterRun = false;

        // Cached reference to the interactable
        private IInteractable _interactable;

        /// <summary>
        /// Gets or sets the action runner for this component.
        /// </summary>
        public ActionRunner ActionRunner
        {
            get => _actionRunner;
            set => _actionRunner = value;
        }

        /// <summary>
        /// Gets or sets whether actions should run only on successful interactions.
        /// </summary>
        public bool RunOnSuccessOnly
        {
            get => _runOnSuccessOnly;
            set => _runOnSuccessOnly = value;
        }

        /// <summary>
        /// Gets or sets whether the action runner should be cleared after running.
        /// </summary>
        public bool ClearAfterRun
        {
            get => _clearAfterRun;
            set => _clearAfterRun = value;
        }

        private void Awake()
        {
            // Find the IInteractable component on this GameObject
            _interactable = GetComponent<IInteractable>();

            if (_interactable == null)
            {
                Debug.LogWarning($"[InteractableActionRunner] No IInteractable component found on {gameObject.name}. Actions will not run.");
            }
        }

        private void OnEnable()
        {
            // Subscribe to the interactable's event
            if (_interactable != null)
            {
                _interactable.OnInteracted += HandleInteraction;
            }
        }

        private void OnDisable()
        {
            // Unsubscribe from the interactable's event
            if (_interactable != null)
            {
                _interactable.OnInteracted -= HandleInteraction;
            }
        }

        /// <summary>
        /// Handles the interaction event from the IInteractable component.
        /// </summary>
        /// <param name="interactor">The GameObject performing the interaction.</param>
        /// <param name="success">Whether the interaction was successful.</param>
        private void HandleInteraction(GameObject interactor, bool success)
        {
            // Check if we should run based on success setting
            if (_runOnSuccessOnly && !success)
            {
                return;
            }

            // Check if we have an action runner with actions
            if (_actionRunner == null || _actionRunner.IsEmpty())
            {
                if (_actionRunner == null)
                {
                    Debug.LogWarning($"[InteractableActionRunner] ActionRunner is not set on {gameObject.name}.");
                }
                return;
            }

            // Create a context object with information about the interaction
            var context = new InteractionContext
            {
                Interactable = _interactable,
                Interactor = interactor,
                Success = success,
                ActionRunner = this
            };

            // Run all actions with the context
            _actionRunner.RunAll(context);

            // Clear actions if configured to do so
            if (_clearAfterRun)
            {
                _actionRunner.ClearActions();
            }
        }

        /// <summary>
        /// Context object passed to actions during interaction.
        /// </summary>
        public class InteractionContext
        {
            /// <summary>
            /// The interactable object being interacted with.
            /// </summary>
            public IInteractable Interactable { get; set; }

            /// <summary>
            /// The GameObject performing the interaction.
            /// </summary>
            public GameObject Interactor { get; set; }

            /// <summary>
            /// Whether the interaction was successful.
            /// </summary>
            public bool Success { get; set; }

            /// <summary>
            /// The action runner that is executing the actions.
            /// </summary>
            public InteractableActionRunner ActionRunner { get; set; }
        }
    }
}
