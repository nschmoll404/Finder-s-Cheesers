using UnityEngine;

namespace FindersCheesers
{
    /// <summary>
    /// A component that implements IFireInteractable and provides events for other components to react to.
    /// This can be attached to any GameObject that should be able to catch fire (e.g., wood, paper, oil barrels).
    /// </summary>
    [AddComponentMenu("Finders Cheesers/Fire Interactable")]
    public class FireInteractable : MonoBehaviour, IFireInteractable
    {
        [Header("Fire Settings")]
        [Tooltip("Whether this object can currently be ignited")]
        [SerializeField]
        private bool canBeIgnited = true;

        [Tooltip("Duration the fire will burn (in seconds)")]
        [SerializeField]
        private float fireDuration = 10f;

        [Tooltip("Damage per second while on fire")]
        [SerializeField]
        private float fireDamagePerSecond = 5f;

        [Tooltip("Whether to destroy the GameObject when fire extinguishes")]
        [SerializeField]
        private bool destroyOnExtinguish = false;

        [Tooltip("Delay before destroying the GameObject (in seconds)")]
        [SerializeField]
        private float destroyDelay = 0f;

        [Header("Debug")]
        [Tooltip("Show debug information in the console")]
        [SerializeField]
        private bool debugMode = false;

        /// <summary>
        /// Event fired when the object is ignited.
        /// </summary>
        public event System.Action<GameObject> OnIgnited;

        /// <summary>
        /// Event fired when the object is extinguished.
        /// </summary>
        public event System.Action OnExtinguished;

        // Fire state
        private bool isOnFire;
        private float fireTimer;
        private bool isBurning;

        // Component reference
        private Health health;

        /// <summary>
        /// Gets whether the object is currently on fire.
        /// </summary>
        public bool IsOnFire => isOnFire;

        /// <summary>
        /// Gets the transform of this interactable.
        /// </summary>
        public Transform Transform => transform;

        /// <summary>
        /// Gets or sets whether this object can currently be ignited.
        /// </summary>
        public bool CanBeIgnited
        {
            get => canBeIgnited;
            set => canBeIgnited = value;
        }

        private void Awake()
        {
            health = GetComponent<Health>();
        }

        private void Update()
        {
            // Update fire timer
            if (isBurning)
            {
                fireTimer += Time.deltaTime;

                // Apply fire damage
                if (health != null && fireDamagePerSecond > 0f)
                {
                    health.TakeDamage(fireDamagePerSecond * Time.deltaTime);
                }

                // Check if fire duration is up
                if (fireTimer >= fireDuration)
                {
                    isBurning = false;
                    Extinguish();

                    if (debugMode)
                    {
                        Debug.Log($"[FireInteractable] Fire duration expired. {gameObject.name} extinguished.");
                    }
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Draw a visual indicator for this fire interactable
            Gizmos.color = IsOnFire ? Color.red : (canBeIgnited ? Color.yellow : Color.gray);
            Gizmos.DrawWireSphere(transform.position, 0.5f);

            // Draw a label with the status
            if (Application.isPlaying)
            {
                #if UNITY_EDITOR
                string status = IsOnFire ? "ON FIRE" : (canBeIgnited ? "Ready" : "Cannot Ignite");
                UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, 
                    $"{gameObject.name}\n{status}");
                #endif
            }
        }

        /// <summary>
        /// Called when the object is ignited by a fire source.
        /// </summary>
        /// <param name="ignitionSource">The GameObject that ignited this object (e.g., a match).</param>
        /// <returns>True if the object was successfully ignited, false otherwise.</returns>
        public bool Ignite(GameObject ignitionSource)
        {
            if (debugMode)
            {
                Debug.Log($"[FireInteractable] Ignite() called on {gameObject.name}. IsOnFire: {IsOnFire}, canBeIgnited: {canBeIgnited}");
            }

            if (IsOnFire)
            {
                if (debugMode)
                {
                    Debug.LogWarning($"[FireInteractable] {gameObject.name} is already on fire!");
                }
                return false;
            }

            if (!canBeIgnited)
            {
                if (debugMode)
                {
                    Debug.LogWarning($"[FireInteractable] {gameObject.name} cannot be ignited!");
                }
                return false;
            }

            // Ignite the object
            isOnFire = true;
            isBurning = true;
            fireTimer = 0f;

            // Fire event
            OnIgnited?.Invoke(ignitionSource);

            if (debugMode)
            {
                Debug.Log($"[FireInteractable] {gameObject.name} ignited by {ignitionSource?.name ?? "unknown source"}!");
            }

            return true;
        }

        /// <summary>
        /// Called when the object should extinguish its fire.
        /// </summary>
        public void Extinguish()
        {
            if (!IsOnFire)
            {
                if (debugMode)
                {
                    Debug.LogWarning($"[FireInteractable] {gameObject.name} is not on fire!");
                }
                return;
            }

            // Extinguish the fire
            isOnFire = false;
            isBurning = false;

            // Fire event
            OnExtinguished?.Invoke();

            if (debugMode)
            {
                Debug.Log($"[FireInteractable] {gameObject.name} extinguished!");
            }

            // Destroy the GameObject if configured
            if (destroyOnExtinguish)
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

        /// <summary>
        /// Checks if this object can currently be ignited.
        /// </summary>
        /// <returns>True if the object can be ignited, false otherwise.</returns>
        public bool CanIgnite()
        {
            return canBeIgnited && !IsOnFire;
        }

        /// <summary>
        /// Sets whether this object can be ignited.
        /// </summary>
        /// <param name="canIgnite">Whether the object can be ignited.</param>
        public void SetCanBeIgnited(bool canIgnite)
        {
            canBeIgnited = canIgnite;

            if (debugMode)
            {
                Debug.Log($"[FireInteractable] {gameObject.name} can be ignited set to: {canIgnite}");
            }
        }

        /// <summary>
        /// Sets the fire duration.
        /// </summary>
        /// <param name="duration">The new fire duration in seconds.</param>
        public void SetFireDuration(float duration)
        {
            fireDuration = Mathf.Max(0.1f, duration);

            if (debugMode)
            {
                Debug.Log($"[FireInteractable] {gameObject.name} fire duration set to: {fireDuration:F2} seconds.");
            }
        }

        /// <summary>
        /// Sets the fire damage per second.
        /// </summary>
        /// <param name="damage">The new fire damage per second.</param>
        public void SetFireDamagePerSecond(float damage)
        {
            fireDamagePerSecond = Mathf.Max(0f, damage);

            if (debugMode)
            {
                Debug.Log($"[FireInteractable] {gameObject.name} fire damage per second set to: {fireDamagePerSecond:F2}.");
            }
        }

        private void OnValidate()
        {
            // Ensure values are valid in editor
            fireDuration = Mathf.Max(0.1f, fireDuration);
            fireDamagePerSecond = Mathf.Max(0f, fireDamagePerSecond);
            destroyDelay = Mathf.Max(0f, destroyDelay);
        }
    }
}
