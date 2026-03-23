using UnityEngine;
using Actions;

namespace FindersCheesers
{
    /// <summary>
    /// A component that runs actions in response to health events.
    /// Supports damage, heal, and death events.
    /// </summary>
    [AddComponentMenu("Finders Cheesers/Health Actions")]
    public class HealthActions : MonoBehaviour
    {
        #region Settings

        [Header("References")]
        [Tooltip("The Health component to listen to events from. If null, will try to find one on this GameObject.")]
        [SerializeField]
        private Health health;

        [Header("Damage Actions")]
        [Tooltip("Actions to run when the entity takes damage.")]
        [SerializeField]
        private ActionRunner damagedActions = new ActionRunner();

        [Header("Heal Actions")]
        [Tooltip("Actions to run when the entity is healed.")]
        [SerializeField]
        private ActionRunner healActions = new ActionRunner();

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
        /// Gets the damaged actions runner.
        /// </summary>
        public ActionRunner DamagedActions => damagedActions;

        /// <summary>
        /// Gets the heal actions runner.
        /// </summary>
        public ActionRunner HealActions => healActions;

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
                health.OnDamageTaken += HandleDamaged;
                health.OnHealed += HandleHeal;
                health.OnDeath += HandleDeath;
            }
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.OnDamageTaken -= HandleDamaged;
                health.OnHealed -= HandleHeal;
                health.OnDeath -= HandleDeath;
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handles the damage event by running damaged actions.
        /// </summary>
        /// <param name="damageAmount">The amount of damage taken.</param>
        private void HandleDamaged(float damageAmount)
        {
            if (damagedActions != null && !damagedActions.IsEmpty())
            {
                damagedActions.RunAll(gameObject);
            }
        }

        /// <summary>
        /// Handles the heal event by running heal actions.
        /// </summary>
        /// <param name="healAmount">The amount healed.</param>
        private void HandleHeal(float healAmount)
        {
            if (healActions != null && !healActions.IsEmpty())
            {
                healActions.RunAll(gameObject);
            }
        }

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
                health.OnDamageTaken -= HandleDamaged;
                health.OnHealed -= HandleHeal;
                health.OnDeath -= HandleDeath;
            }

            health = newHealth;

            // Subscribe to new health if exists
            if (health != null && enabled)
            {
                health.OnDamageTaken += HandleDamaged;
                health.OnHealed += HandleHeal;
                health.OnDeath += HandleDeath;
            }
        }

        #endregion
    }
}
