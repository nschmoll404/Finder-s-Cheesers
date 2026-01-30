using UnityEngine;

namespace FindersCheesers
{
    /// <summary>
    /// A component that manages health for a GameObject.
    /// Provides events for health changes, damage, healing, and death.
    /// </summary>
    [AddComponentMenu("Finders Cheesers/Health")]
    public class Health : MonoBehaviour
    {
        #region Settings

        [Header("Health Settings")]
        [Tooltip("The maximum health value")]
        [SerializeField]
        private float maxHealth = 100f;

        [Tooltip("The current health value")]
        [SerializeField]
        private float currentHealth = 100f;

        [Tooltip("Whether the GameObject is destroyed when health reaches zero")]
        [SerializeField]
        private bool destroyOnDeath = false;

        [Tooltip("Delay before destroying the GameObject (in seconds)")]
        [SerializeField]
        private float destroyDelay = 0f;

        [Tooltip("Whether to show debug information in the console")]
        [SerializeField]
        private bool debugMode = false;

        #endregion

        #region Events

        /// <summary>
        /// Event fired when health changes.
        /// </summary>
        public event System.Action<float, float> OnHealthChanged;

        /// <summary>
        /// Event fired when damage is taken.
        /// </summary>
        public event System.Action<float> OnDamageTaken;

        /// <summary>
        /// Event fired when healing occurs.
        /// </summary>
        public event System.Action<float> OnHealed;

        /// <summary>
        /// Event fired when health reaches zero.
        /// </summary>
        public event System.Action OnDeath;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the maximum health value.
        /// </summary>
        public float MaxHealth => maxHealth;

        /// <summary>
        /// Gets the current health value.
        /// </summary>
        public float CurrentHealth => currentHealth;

        /// <summary>
        /// Gets the current health as a percentage (0.0 to 1.0).
        /// </summary>
        public float HealthPercentage => maxHealth > 0f ? currentHealth / maxHealth : 0f;

        /// <summary>
        /// Gets whether the entity is dead (health <= 0).
        /// </summary>
        public bool IsDead => currentHealth <= 0f;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Clamp current health to max health
            currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        }

        #endregion

        #region Public API

        /// <summary>
        /// Takes the specified amount of damage.
        /// </summary>
        /// <param name="damage">The amount of damage to take.</param>
        public void TakeDamage(float damage)
        {
            if (IsDead)
            {
                if (debugMode)
                {
                    Debug.LogWarning($"[Health] {gameObject.name} is already dead and cannot take damage.");
                }
                return;
            }

            if (damage <= 0f)
            {
                if (debugMode)
                {
                    Debug.LogWarning($"[Health] Damage amount must be positive. Received: {damage}");
                }
                return;
            }

            float oldHealth = currentHealth;
            currentHealth = Mathf.Max(0f, currentHealth - damage);

            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            OnDamageTaken?.Invoke(damage);

            if (debugMode)
            {
                Debug.Log($"[Health] {gameObject.name} took {damage} damage. Health: {currentHealth}/{maxHealth}");
            }

            // Check for death
            if (currentHealth <= 0f)
            {
                Die();
            }
        }

        /// <summary>
        /// Heals the specified amount.
        /// </summary>
        /// <param name="amount">The amount to heal.</param>
        public void Heal(float amount)
        {
            if (IsDead)
            {
                if (debugMode)
                {
                    Debug.LogWarning($"[Health] {gameObject.name} is dead and cannot be healed.");
                }
                return;
            }

            if (amount <= 0f)
            {
                if (debugMode)
                {
                    Debug.LogWarning($"[Health] Heal amount must be positive. Received: {amount}");
                }
                return;
            }

            float oldHealth = currentHealth;
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);

            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            OnHealed?.Invoke(amount);

            if (debugMode)
            {
                Debug.Log($"[Health] {gameObject.name} healed for {amount}. Health: {currentHealth}/{maxHealth}");
            }
        }

        /// <summary>
        /// Sets the current health directly.
        /// </summary>
        /// <param name="health">The new health value.</param>
        public void SetHealth(float health)
        {
            float oldHealth = currentHealth;
            currentHealth = Mathf.Clamp(health, 0f, maxHealth);

            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            if (debugMode)
            {
                Debug.Log($"[Health] {gameObject.name} health set to {currentHealth}/{maxHealth}");
            }

            // Check for death
            if (currentHealth <= 0f && oldHealth > 0f)
            {
                Die();
            }
        }

        /// <summary>
        /// Sets the maximum health value.
        /// </summary>
        /// <param name="newMaxHealth">The new maximum health value.</param>
        public void SetMaxHealth(float newMaxHealth)
        {
            if (newMaxHealth <= 0f)
            {
                Debug.LogError($"[Health] Max health must be positive. Received: {newMaxHealth}");
                return;
            }

            float oldMaxHealth = maxHealth;
            maxHealth = newMaxHealth;

            // Adjust current health proportionally
            if (oldMaxHealth > 0f)
            {
                float healthRatio = currentHealth / oldMaxHealth;
                currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
            }
            else
            {
                currentHealth = maxHealth;
            }

            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            if (debugMode)
            {
                Debug.Log($"[Health] {gameObject.name} max health set to {maxHealth}. Current: {currentHealth}");
            }
        }

        /// <summary>
        /// Resets health to maximum.
        /// </summary>
        public void ResetHealth()
        {
            SetHealth(maxHealth);

            if (debugMode)
            {
                Debug.Log($"[Health] {gameObject.name} health reset to maximum.");
            }
        }

        /// <summary>
        /// Revives the entity, restoring it to a specified health amount.
        /// </summary>
        /// <param name="healthAmount">The health amount to revive with (default: max health).</param>
        public void Revive(float healthAmount = -1f)
        {
            if (!IsDead)
            {
                if (debugMode)
                {
                    Debug.LogWarning($"[Health] {gameObject.name} is not dead and cannot be revived.");
                }
                return;
            }

            float reviveHealth = healthAmount < 0f ? maxHealth : Mathf.Clamp(healthAmount, 0f, maxHealth);
            currentHealth = reviveHealth;

            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            if (debugMode)
            {
                Debug.Log($"[Health] {gameObject.name} revived with {currentHealth} health.");
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Handles death logic.
        /// </summary>
        private void Die()
        {
            OnDeath?.Invoke();

            if (debugMode)
            {
                Debug.Log($"[Health] {gameObject.name} has died!");
            }

            // Destroy GameObject if configured
            if (destroyOnDeath)
            {
                if (destroyDelay > 0f)
                {
                    Destroy(gameObject, destroyDelay);
                }
                else
                {
                    Destroy(gameObject);
                }
            }
        }

        #endregion

        #region Editor

        private void OnValidate()
        {
            // Ensure values are valid in editor
            maxHealth = Mathf.Max(1f, maxHealth);
            currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
            destroyDelay = Mathf.Max(0f, destroyDelay);
        }

        #endregion
    }
}
