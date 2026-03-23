using UnityEngine;
using Actions;

namespace FindersCheesers
{
    /// <summary>
    /// A component that runs actions in response to waypoint animator events.
    /// Supports waypoint reached, path complete, movement started, resumed, paused, and stopped events.
    /// </summary>
    [AddComponentMenu("Finders Cheesers/Waypoint Animator Actions")]
    public class WaypointAnimatorActions : MonoBehaviour
    {
        #region Settings

        [Header("References")]
        [Tooltip("The WaypointAnimator component to listen to events from. If null, will try to find one on this GameObject.")]
        [SerializeField]
        private WaypointAnimator waypointAnimator;

        [Header("Waypoint Reached Actions")]
        [Tooltip("Actions to run when a waypoint is reached.")]
        [SerializeField]
        private ActionRunner onWaypointReachedActions = new ActionRunner();

        [Header("Path Complete Actions")]
        [Tooltip("Actions to run when the path is complete.")]
        [SerializeField]
        private ActionRunner onPathCompleteActions = new ActionRunner();

        [Header("Movement Started Actions")]
        [Tooltip("Actions to run when movement initially starts.")]
        [SerializeField]
        private ActionRunner onMovementStartedActions = new ActionRunner();

        [Header("Movement Resumed Actions")]
        [Tooltip("Actions to run when movement resumes from a paused state.")]
        [SerializeField]
        private ActionRunner onMovementResumedActions = new ActionRunner();

        [Header("Movement Paused Actions")]
        [Tooltip("Actions to run when movement is paused.")]
        [SerializeField]
        private ActionRunner onMovementPausedActions = new ActionRunner();

        [Header("Movement Stopped Actions")]
        [Tooltip("Actions to run when movement stops.")]
        [SerializeField]
        private ActionRunner onMovementStoppedActions = new ActionRunner();

        #endregion

        #region Properties

        /// <summary>
        /// Gets the WaypointAnimator component being monitored.
        /// </summary>
        public WaypointAnimator WaypointAnimator => waypointAnimator;

        /// <summary>
        /// Gets the waypoint reached actions runner.
        /// </summary>
        public ActionRunner OnWaypointReachedActions => onWaypointReachedActions;

        /// <summary>
        /// Gets the path complete actions runner.
        /// </summary>
        public ActionRunner OnPathCompleteActions => onPathCompleteActions;

        /// <summary>
        /// Gets the movement started actions runner.
        /// </summary>
        public ActionRunner OnMovementStartedActions => onMovementStartedActions;

        /// <summary>
        /// Gets the movement resumed actions runner.
        /// </summary>
        public ActionRunner OnMovementResumedActions => onMovementResumedActions;

        /// <summary>
        /// Gets the movement paused actions runner.
        /// </summary>
        public ActionRunner OnMovementPausedActions => onMovementPausedActions;

        /// <summary>
        /// Gets the movement stopped actions runner.
        /// </summary>
        public ActionRunner OnMovementStoppedActions => onMovementStoppedActions;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Find WaypointAnimator component if not assigned
            if (waypointAnimator == null)
            {
                waypointAnimator = GetComponent<WaypointAnimator>();
            }

            if (waypointAnimator == null)
            {
                Debug.LogError($"[WaypointAnimatorActions] {gameObject.name} has no WaypointAnimator component assigned or found!");
            }
        }

        private void OnEnable()
        {
            if (waypointAnimator != null)
            {
                waypointAnimator.OnWaypointReached.AddListener(HandleWaypointReached);
                waypointAnimator.OnPathComplete.AddListener(HandlePathComplete);
                waypointAnimator.OnMovementStarted.AddListener(HandleMovementStarted);
                waypointAnimator.OnMovementResumed.AddListener(HandleMovementResumed);
                waypointAnimator.OnMovementPaused.AddListener(HandleMovementPaused);
                waypointAnimator.OnMovementStopped.AddListener(HandleMovementStopped);
            }
        }

        private void OnDisable()
        {
            if (waypointAnimator != null)
            {
                waypointAnimator.OnWaypointReached.RemoveListener(HandleWaypointReached);
                waypointAnimator.OnPathComplete.RemoveListener(HandlePathComplete);
                waypointAnimator.OnMovementStarted.RemoveListener(HandleMovementStarted);
                waypointAnimator.OnMovementResumed.RemoveListener(HandleMovementResumed);
                waypointAnimator.OnMovementPaused.RemoveListener(HandleMovementPaused);
                waypointAnimator.OnMovementStopped.RemoveListener(HandleMovementStopped);
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handles the waypoint reached event by running waypoint reached actions.
        /// </summary>
        private void HandleWaypointReached()
        {
            if (onWaypointReachedActions != null && !onWaypointReachedActions.IsEmpty())
            {
                onWaypointReachedActions.RunAll(gameObject);
            }
        }

        /// <summary>
        /// Handles the path complete event by running path complete actions.
        /// </summary>
        private void HandlePathComplete()
        {
            if (onPathCompleteActions != null && !onPathCompleteActions.IsEmpty())
            {
                onPathCompleteActions.RunAll(gameObject);
            }
        }

        /// <summary>
        /// Handles the movement started event by running movement started actions.
        /// </summary>
        private void HandleMovementStarted()
        {
            if (onMovementStartedActions != null && !onMovementStartedActions.IsEmpty())
            {
                onMovementStartedActions.RunAll(gameObject);
            }
        }

        /// <summary>
        /// Handles the movement resumed event by running movement resumed actions.
        /// </summary>
        private void HandleMovementResumed()
        {
            if (onMovementResumedActions != null && !onMovementResumedActions.IsEmpty())
            {
                onMovementResumedActions.RunAll(gameObject);
            }
        }

        /// <summary>
        /// Handles the movement paused event by running movement paused actions.
        /// </summary>
        private void HandleMovementPaused()
        {
            if (onMovementPausedActions != null && !onMovementPausedActions.IsEmpty())
            {
                onMovementPausedActions.RunAll(gameObject);
            }
        }

        /// <summary>
        /// Handles the movement stopped event by running movement stopped actions.
        /// </summary>
        private void HandleMovementStopped()
        {
            if (onMovementStoppedActions != null && !onMovementStoppedActions.IsEmpty())
            {
                onMovementStoppedActions.RunAll(gameObject);
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Manually sets the WaypointAnimator component to monitor.
        /// </summary>
        /// <param name="newWaypointAnimator">The WaypointAnimator component to monitor.</param>
        public void SetWaypointAnimator(WaypointAnimator newWaypointAnimator)
        {
            // Unsubscribe from old waypoint animator if exists
            if (waypointAnimator != null && enabled)
            {
                waypointAnimator.OnWaypointReached.RemoveListener(HandleWaypointReached);
                waypointAnimator.OnPathComplete.RemoveListener(HandlePathComplete);
                waypointAnimator.OnMovementStarted.RemoveListener(HandleMovementStarted);
                waypointAnimator.OnMovementResumed.RemoveListener(HandleMovementResumed);
                waypointAnimator.OnMovementPaused.RemoveListener(HandleMovementPaused);
                waypointAnimator.OnMovementStopped.RemoveListener(HandleMovementStopped);
            }

            waypointAnimator = newWaypointAnimator;

            // Subscribe to new waypoint animator if exists and enabled
            if (waypointAnimator != null && enabled)
            {
                waypointAnimator.OnWaypointReached.AddListener(HandleWaypointReached);
                waypointAnimator.OnPathComplete.AddListener(HandlePathComplete);
                waypointAnimator.OnMovementStarted.AddListener(HandleMovementStarted);
                waypointAnimator.OnMovementResumed.AddListener(HandleMovementResumed);
                waypointAnimator.OnMovementPaused.AddListener(HandleMovementPaused);
                waypointAnimator.OnMovementStopped.AddListener(HandleMovementStopped);
            }
        }

        #endregion
    }
}
