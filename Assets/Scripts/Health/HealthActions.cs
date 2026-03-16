using UnityEngine;
using Actions;

namespace FindersCheesers
{
    /// <summary>
    /// A component that runs actions in response to health events.
    /// Currently supports death events.
    /// </summary>
    [AddComponentMenu("Finders Cheesers/Health Actions")]
    public class HealthActions : MonoBehaviour
    {
        #region Settings

        [Header("References")]
        [Tooltip("The Health component to listen to events from. If null, will try to find one on this GameObject.")]
        [SerializeField]
        private Health health;

        [Header("Death Actions")]
        [Tooltip("Actions to run when the entity dies.")]
        [SerializeField]
        private ActionRunner deathActions = new ActionRunner();

        #endregion

        #region Properties

        /// <summary>
        /// Gets the Health component being monitored.
        /// </summary>
        public Health Health => health;

        /// <summary>
        /// Gets the death actions runner.
        /// </summary>
        public ActionRunner DeathActions => deathActions;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Find Health component if not assigned
            if (health == null)
            {
                health = GetComponent<Health>();
            }

            if (health == null)
            {
                Debug.LogError($"[HealthActions] {gameObject.name} has no Health component assigned or found!");
            }
        }

        private void OnEnable()
        {
            if (health != null)
            {
                health.OnDeath += HandleDeath;
            }
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.OnDeath -= HandleDeath;
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handles the death event by running death actions.
        /// </summary>
        private void HandleDeath()
        {
            if (deathActions != null && !deathActions.IsEmpty())
            {
                deathActions.RunAll(gameObject);
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Manually sets the Health component to monitor.
        /// </summary>
        /// <param name="newHealth">The Health component to monitor.</param>
        public void SetHealth(Health newHealth)
        {
            // Unsubscribe from old health if exists
            if (health != null && enabled)
            {
                health.OnDeath -= HandleDeath;
            }

            health = newHealth;

            // Subscribe to new health if exists
            if (health != null && enabled)
            {
                health.OnDeath += HandleDeath;
            }
        }

        #endregion
    }
}
