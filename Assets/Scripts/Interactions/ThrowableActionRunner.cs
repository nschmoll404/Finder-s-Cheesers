using UnityEngine;
using Actions;

namespace FindersCheesers
{
    /// <summary>
    /// A MonoBehaviour component that finds an IThrowable on the same GameObject
    /// and runs actions when the object is picked up or thrown.
    /// Attach this alongside any IThrowable component to execute sequences of actions.
    /// </summary>
    [RequireComponent(typeof(IThrowable))]
    [AddComponentMenu("Finders Cheesers/Throwable Action Runner")]
    public class ThrowableActionRunner : MonoBehaviour
    {
        [Header("Pickup Actions")]
        [Tooltip("The action runner that will execute actions when the object is picked up.")]
        [SerializeField] private ActionRunner _pickupActionRunner;

        [Header("Throw Actions")]
        [Tooltip("The action runner that will execute actions when the object is thrown.")]
        [SerializeField] private ActionRunner _throwActionRunner;

        [Header("Settings")]
        [Tooltip("Should the pickup action runner be cleared after running?")]
        [SerializeField] private bool _clearPickupAfterRun = false;

        [Tooltip("Should the throw action runner be cleared after running?")]
        [SerializeField] private bool _clearThrowAfterRun = false;

        [Header("Debug")]
        [Tooltip("Show debug information in the console")]
        [SerializeField] private bool _debugMode = false;

        // Cached reference to the throwable
        private IThrowable _throwable;

        /// <summary>
        /// Gets or sets the pickup action runner for this component.
        /// </summary>
        public ActionRunner PickupActionRunner
        {
            get => _pickupActionRunner;
            set => _pickupActionRunner = value;
        }

        /// <summary>
        /// Gets or sets the throw action runner for this component.
        /// </summary>
        public ActionRunner ThrowActionRunner
        {
            get => _throwActionRunner;
            set => _throwActionRunner = value;
        }

        /// <summary>
        /// Gets or sets whether the pickup action runner should be cleared after running.
        /// </summary>
        public bool ClearPickupAfterRun
        {
            get => _clearPickupAfterRun;
            set => _clearPickupAfterRun = value;
        }

        /// <summary>
        /// Gets or sets whether the throw action runner should be cleared after running.
        /// </summary>
        public bool ClearThrowAfterRun
        {
            get => _clearThrowAfterRun;
            set => _clearThrowAfterRun = value;
        }

        private void Awake()
        {
            // Find the IThrowable component on this GameObject
            _throwable = GetComponent<IThrowable>();

            if (_throwable == null)
            {
                Debug.LogError($"[ThrowableActionRunner] No IThrowable component found on {gameObject.name}. Actions will not run.");
            }
        }

        private void OnEnable()
        {
            // Subscribe to the throwable's events
            if (_throwable != null)
            {
                // Try to get ThrowableObject for pickup event
                ThrowableObject throwableObject = GetComponent<ThrowableObject>();
                if (throwableObject != null)
                {
                    throwableObject.OnPickedUp += HandlePickup;

                    if (_debugMode)
                    {
                        Debug.Log($"[ThrowableActionRunner] Subscribed to pickup event on {gameObject.name}.");
                    }
                }

                // Subscribe to throw event from IThrowable
                _throwable.OnThrown += HandleThrow;

                if (_debugMode)
                {
                    Debug.Log($"[ThrowableActionRunner] Subscribed to throw event on {gameObject.name}.");
                }
            }
        }

        private void OnDisable()
        {
            // Unsubscribe from the throwable's events
            if (_throwable != null)
            {
                // Try to get ThrowableObject for pickup event
                ThrowableObject throwableObject = GetComponent<ThrowableObject>();
                if (throwableObject != null)
                {
                    throwableObject.OnPickedUp -= HandlePickup;

                    if (_debugMode)
                    {
                        Debug.Log($"[ThrowableActionRunner] Unsubscribed from pickup event on {gameObject.name}.");
                    }
                }

                // Unsubscribe from throw event from IThrowable
                _throwable.OnThrown -= HandleThrow;

                if (_debugMode)
                {
                    Debug.Log($"[ThrowableActionRunner] Unsubscribed from throw event on {gameObject.name}.");
                }
            }
        }

        /// <summary>
        /// Handles the pickup event from the ThrowableObject component.
        /// </summary>
        private void HandlePickup()
        {
            if (_debugMode)
            {
                Debug.Log($"[ThrowableActionRunner] HandlePickup() called on {gameObject.name}.");
            }

            // Check if we have a pickup action runner with actions
            if (_pickupActionRunner == null || _pickupActionRunner.IsEmpty())
            {
                if (_debugMode)
                {
                    if (_pickupActionRunner == null)
                    {
                        Debug.LogWarning($"[ThrowableActionRunner] PickupActionRunner is not set on {gameObject.name}.");
                    }
                    else
                    {
                        Debug.Log($"[ThrowableActionRunner] PickupActionRunner is empty on {gameObject.name}.");
                    }
                }
                return;
            }

            // Create a context object with information about the pickup
            var context = new PickupContext
            {
                Throwable = _throwable,
                GameObject = gameObject,
                ActionRunner = this
            };

            // Run all pickup actions with the context
            _pickupActionRunner.RunAll(context);

            if (_debugMode)
            {
                Debug.Log($"[ThrowableActionRunner] Ran {_pickupActionRunner.ActionCount} pickup actions on {gameObject.name}.");
            }

            // Clear actions if configured to do so
            if (_clearPickupAfterRun)
            {
                _pickupActionRunner.ClearActions();

                if (_debugMode)
                {
                    Debug.Log($"[ThrowableActionRunner] Cleared pickup actions on {gameObject.name}.");
                }
            }
        }

        /// <summary>
        /// Handles the throw event from the IThrowable component.
        /// </summary>
        /// <param name="destination">The destination the object was thrown to.</param>
        private void HandleThrow(Vector3 destination)
        {
            if (_debugMode)
            {
                Debug.Log($"[ThrowableActionRunner] HandleThrow() called on {gameObject.name}. Destination: {destination}.");
            }

            // Check if we have a throw action runner with actions
            if (_throwActionRunner == null || _throwActionRunner.IsEmpty())
            {
                if (_debugMode)
                {
                    if (_throwActionRunner == null)
                    {
                        Debug.LogWarning($"[ThrowableActionRunner] ThrowActionRunner is not set on {gameObject.name}.");
                    }
                    else
                    {
                        Debug.Log($"[ThrowableActionRunner] ThrowActionRunner is empty on {gameObject.name}.");
                    }
                }
                return;
            }

            // Create a context object with information about the throw
            var context = new ThrowContext
            {
                Throwable = _throwable,
                GameObject = gameObject,
                Destination = destination,
                ActionRunner = this
            };

            // Run all throw actions with the context
            _throwActionRunner.RunAll(context);

            if (_debugMode)
            {
                Debug.Log($"[ThrowableActionRunner] Ran {_throwActionRunner.ActionCount} throw actions on {gameObject.name}.");
            }

            // Clear actions if configured to do so
            if (_clearThrowAfterRun)
            {
                _throwActionRunner.ClearActions();

                if (_debugMode)
                {
                    Debug.Log($"[ThrowableActionRunner] Cleared throw actions on {gameObject.name}.");
                }
            }
        }

        /// <summary>
        /// Context object passed to actions during pickup.
        /// </summary>
        public class PickupContext
        {
            /// <summary>
            /// The throwable object being picked up.
            /// </summary>
            public IThrowable Throwable { get; set; }

            /// <summary>
            /// The GameObject being picked up.
            /// </summary>
            public GameObject GameObject { get; set; }

            /// <summary>
            /// The action runner that is executing the actions.
            /// </summary>
            public ThrowableActionRunner ActionRunner { get; set; }
        }

        /// <summary>
        /// Context object passed to actions during throw.
        /// </summary>
        public class ThrowContext
        {
            /// <summary>
            /// The throwable object being thrown.
            /// </summary>
            public IThrowable Throwable { get; set; }

            /// <summary>
            /// The GameObject being thrown.
            /// </summary>
            public GameObject GameObject { get; set; }

            /// <summary>
            /// The destination the object was thrown to.
            /// </summary>
            public Vector3 Destination { get; set; }

            /// <summary>
            /// The action runner that is executing the actions.
            /// </summary>
            public ThrowableActionRunner ActionRunner { get; set; }
        }
    }
}
