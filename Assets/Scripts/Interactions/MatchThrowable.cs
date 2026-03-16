using UnityEngine;
using System.Collections.Generic;

namespace FindersCheesers
{
    /// <summary>
    /// A throwable match component that can be lit and causes damage to objects within its flame radius.
    /// When lit, the match damages Health components and can ignite FireInteractable objects.
    /// The match will automatically extinguish after a set duration.
    /// </summary>
    [AddComponentMenu("Finders Cheesers/Match Throwable")]
    [RequireComponent(typeof(ThrowableObject))]
    public class MatchThrowable : MonoBehaviour
    {
        #region Settings

        [Header("Match Settings")]
        [Tooltip("Duration the match stays lit (in seconds)")]
        [SerializeField]
        private float burnDuration = 10f;

        [Tooltip("Radius of the flame that causes damage")]
        [SerializeField]
        private float flameRadius = 1f;

        [Tooltip("Damage per second to Health components within flame radius")]
        [SerializeField]
        private float damagePerSecond = 10f;

        [Tooltip("Layer mask for objects that can be damaged by the flame")]
        [SerializeField]
        private LayerMask damageableLayers = -1;

        [Tooltip("Automatically light the match when the object is picked up")]
        [SerializeField]
        private bool autoLightOnPickup = false;

        [Header("Ignition Settings")]
        [Tooltip("Whether the match can ignite FireInteractable objects")]
        [SerializeField]
        private bool canIgniteObjects = true;

        [Tooltip("Radius within which the match can ignite FireInteractable objects")]
        [SerializeField]
        private float ignitionRadius = 1.5f;

        [Header("Visual Settings")]
        [Tooltip("Transform representing the flame tip position for overlap checks")]
        [SerializeField]
        private Transform flameTipTransform;

        [Header("Debug")]
        [Tooltip("Show debug information in the console")]
        [SerializeField]
        private bool debugMode = false;

        [Tooltip("Draw flame and ignition radii in scene view")]
        [SerializeField]
        private bool drawRadii = true;

        #endregion

        #region Events

        /// <summary>
        /// Event fired when the match is lit.
        /// </summary>
        public event System.Action OnMatchLit;

        /// <summary>
        /// Event fired when the match is extinguished.
        /// </summary>
        public event System.Action OnMatchExtinguished;

        /// <summary>
        /// Event fired when the match ignites a FireInteractable object.
        /// </summary>
        public event System.Action<IFireInteractable> OnObjectIgnited;

        #endregion

        #region Properties

        /// <summary>
        /// Gets whether the match is currently lit.
        /// </summary>
        public bool IsLit { get; private set; }

        /// <summary>
        /// Gets whether the match has been extinguished (either naturally or manually).
        /// </summary>
        public bool IsExtinguished { get; private set; }

        /// <summary>
        /// Gets the remaining burn time (0 if match is not lit).
        /// </summary>
        public float RemainingBurnTime { get; private set; }

        /// <summary>
        /// Gets the position to use for overlap sphere checks.
        /// </summary>
        public Vector3 FlamePosition => flameTipTransform != null ? flameTipTransform.position : transform.position;

        #endregion

        #region Private Fields

        // Component references
        private ThrowableObject throwableObject;

        // Match state
        private float burnTimer;
        private bool isBurning;
        private bool hasSubscribedToPickupEvent = false;

        // Damage tracking to avoid damaging the same object multiple times per second
        private Dictionary<Health, float> lastDamageTimes = new Dictionary<Health, float>();
        private const float DAMAGE_INTERVAL = 0.5f; // Apply damage every 0.5 seconds

        // Ignition tracking to avoid igniting the same object multiple times
        private HashSet<IFireInteractable> ignitedObjects = new HashSet<IFireInteractable>();

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            throwableObject = GetComponent<ThrowableObject>();

            if (throwableObject == null)
            {
                Debug.LogError("[MatchThrowable] ThrowableObject component not found!");
            }

            // If no flame tip transform is set, use the object's transform
            if (flameTipTransform == null)
            {
                flameTipTransform = transform;
            }
        }

        private void OnEnable()
        {
            // Subscribe to pickup event
            if (throwableObject != null && !hasSubscribedToPickupEvent)
            {
                throwableObject.OnPickedUp += HandlePickup;
                hasSubscribedToPickupEvent = true;

                if (debugMode)
                {
                    Debug.Log("[MatchThrowable] Subscribed to ThrowableObject pickup event.");
                }
            }
        }

        private void OnDisable()
        {
            // Unsubscribe from pickup event
            if (throwableObject != null && hasSubscribedToPickupEvent)
            {
                throwableObject.OnPickedUp -= HandlePickup;
                hasSubscribedToPickupEvent = false;

                if (debugMode)
                {
                    Debug.Log("[MatchThrowable] Unsubscribed from ThrowableObject pickup event.");
                }
            }
        }

        private void Update()
        {
            // Update burn timer
            if (isBurning)
            {
                UpdateBurnTimer();
            }

            // Apply damage and ignite objects if lit
            if (IsLit)
            {
                ApplyFlameDamage();
                IgniteNearbyObjects();
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawRadii)
            {
                return;
            }

            // Draw flame radius (damage area)
            Vector3 flamePos = Application.isPlaying ? FlamePosition : 
                              (flameTipTransform != null ? flameTipTransform.position : transform.position);
            
            Gizmos.color = IsLit ? Color.red : Color.yellow;
            Gizmos.DrawWireSphere(flamePos, flameRadius);

            // Draw ignition radius
            if (canIgniteObjects)
            {
                Gizmos.color = Color.orange;
                Gizmos.DrawWireSphere(flamePos, ignitionRadius);
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Lights the match and starts the burn timer.
        /// </summary>
        public void LightMatch()
        {
            if (debugMode)
            {
                Debug.Log($"[MatchThrowable] LightMatch() called. IsLit: {IsLit}, IsExtinguished: {IsExtinguished}");
            }

            if (IsLit)
            {
                if (debugMode)
                {
                    Debug.LogWarning("[MatchThrowable] Match is already lit!");
                }
                return;
            }

            if (IsExtinguished)
            {
                if (debugMode)
                {
                    Debug.LogWarning("[MatchThrowable] Match has been extinguished and cannot be relit!");
                }
                return;
            }

            // Light the match
            IsLit = true;
            isBurning = true;
            burnTimer = 0f;
            RemainingBurnTime = burnDuration;

            // Clear tracking dictionaries
            lastDamageTimes.Clear();
            ignitedObjects.Clear();

            // Fire event
            OnMatchLit?.Invoke();

            if (debugMode)
            {
                Debug.Log($"[MatchThrowable] Match lit! Will burn for {burnDuration:F2} seconds.");
            }
        }

        /// <summary>
        /// Extinguishes the match manually.
        /// </summary>
        public void ExtinguishMatch()
        {
            if (!IsLit)
            {
                if (debugMode)
                {
                    Debug.LogWarning("[MatchThrowable] Match is not lit!");
                }
                return;
            }

            // Extinguish the match
            IsLit = false;
            IsExtinguished = true;
            isBurning = false;
            RemainingBurnTime = 0f;

            // Clear tracking dictionaries
            lastDamageTimes.Clear();
            ignitedObjects.Clear();

            // Fire event
            OnMatchExtinguished?.Invoke();

            if (debugMode)
            {
                Debug.Log("[MatchThrowable] Match extinguished!");
            }
        }

        /// <summary>
        /// Resets the match to its initial state.
        /// </summary>
        public void ResetMatch()
        {
            IsLit = false;
            IsExtinguished = false;
            isBurning = false;
            burnTimer = 0f;
            RemainingBurnTime = 0f;

            // Clear tracking dictionaries
            lastDamageTimes.Clear();
            ignitedObjects.Clear();

            if (debugMode)
            {
                Debug.Log("[MatchThrowable] Match reset.");
            }
        }

        /// <summary>
        /// Sets the burn duration.
        /// </summary>
        /// <param name="duration">The new burn duration in seconds.</param>
        public void SetBurnDuration(float duration)
        {
            burnDuration = Mathf.Max(0.1f, duration);

            if (debugMode)
            {
                Debug.Log($"[MatchThrowable] Burn duration set to: {burnDuration:F2} seconds.");
            }
        }

        /// <summary>
        /// Sets the flame radius.
        /// </summary>
        /// <param name="radius">The new flame radius.</param>
        public void SetFlameRadius(float radius)
        {
            flameRadius = Mathf.Max(0.1f, radius);

            if (debugMode)
            {
                Debug.Log($"[MatchThrowable] Flame radius set to: {flameRadius:F2} units.");
            }
        }

        /// <summary>
        /// Sets the damage per second.
        /// </summary>
        /// <param name="damage">The new damage per second.</param>
        public void SetDamagePerSecond(float damage)
        {
            damagePerSecond = Mathf.Max(0f, damage);

            if (debugMode)
            {
                Debug.Log($"[MatchThrowable] Damage per second set to: {damagePerSecond:F2}.");
            }
        }

        /// <summary>
        /// Sets the ignition radius.
        /// </summary>
        /// <param name="radius">The new ignition radius.</param>
        public void SetIgnitionRadius(float radius)
        {
            ignitionRadius = Mathf.Max(0.1f, radius);

            if (debugMode)
            {
                Debug.Log($"[MatchThrowable] Ignition radius set to: {ignitionRadius:F2} units.");
            }
        }

        /// <summary>
        /// Sets whether the match can ignite FireInteractable objects.
        /// </summary>
        /// <param name="canIgnite">Whether the match can ignite objects.</param>
        public void SetCanIgniteObjects(bool canIgnite)
        {
            canIgniteObjects = canIgnite;

            if (debugMode)
            {
                Debug.Log($"[MatchThrowable] Can ignite objects set to: {canIgniteObjects}");
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Updates the burn timer and extinguishes the match when time runs out.
        /// </summary>
        private void UpdateBurnTimer()
        {
            burnTimer += Time.deltaTime;
            RemainingBurnTime = Mathf.Max(0f, burnDuration - burnTimer);

            // Check if burn time is up
            if (burnTimer >= burnDuration)
            {
                isBurning = false;
                ExtinguishMatch();

                if (debugMode)
                {
                    Debug.Log("[MatchThrowable] Burn time expired. Match extinguished.");
                }
            }
        }

        /// <summary>
        /// Applies flame damage to Health components within the flame radius.
        /// </summary>
        private void ApplyFlameDamage()
        {
            // Find all colliders within flame radius
            Collider[] hitColliders = Physics.OverlapSphere(
                FlamePosition,
                flameRadius,
                damageableLayers,
                QueryTriggerInteraction.Ignore
            );

            float currentTime = Time.time;

            foreach (Collider collider in hitColliders)
            {
                // Skip self
                if (collider.gameObject == gameObject)
                {
                    continue;
                }

                Health health = collider.GetComponent<Health>();
                if (health != null)
                {
                    // Check if we should apply damage (based on interval)
                    if (lastDamageTimes.TryGetValue(health, out float lastDamageTime))
                    {
                        if (currentTime - lastDamageTime < DAMAGE_INTERVAL)
                        {
                            continue; // Too soon to damage again
                        }
                    }

                    // Calculate damage for this interval
                    float damageAmount = damagePerSecond * DAMAGE_INTERVAL;
                    health.TakeDamage(damageAmount);
                    lastDamageTimes[health] = currentTime;

                    if (debugMode)
                    {
                        Debug.Log($"[MatchThrowable] Damaged {collider.gameObject.name} for {damageAmount:F2} damage.");
                    }
                }
            }
        }

        /// <summary>
        /// Ignites FireInteractable objects within the ignition radius.
        /// </summary>
        private void IgniteNearbyObjects()
        {
            if (!canIgniteObjects)
            {
                return;
            }

            // Find all colliders within ignition radius
            Collider[] hitColliders = Physics.OverlapSphere(
                FlamePosition,
                ignitionRadius,
                damageableLayers,
                QueryTriggerInteraction.Ignore
            );

            foreach (Collider collider in hitColliders)
            {
                // Skip self
                if (collider.gameObject == gameObject)
                {
                    continue;
                }

                // Check for IFireInteractable interface
                IFireInteractable fireInteractable = collider.GetComponent<IFireInteractable>();
                if (fireInteractable != null)
                {
                    // Skip if already ignited
                    if (ignitedObjects.Contains(fireInteractable))
                    {
                        continue;
                    }

                    // Check if the object can be ignited
                    if (fireInteractable.CanIgnite())
                    {
                        // Try to ignite the object
                        bool success = fireInteractable.Ignite(gameObject);

                        if (success)
                        {
                            // Track that we've ignited this object
                            ignitedObjects.Add(fireInteractable);

                            // Fire event
                            OnObjectIgnited?.Invoke(fireInteractable);

                            if (debugMode)
                            {
                                Debug.Log($"[MatchThrowable] Ignited {collider.gameObject.name}!");
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Handles the pickup event from ThrowableObject.
        /// </summary>
        private void HandlePickup()
        {
            if (debugMode)
            {
                Debug.Log($"[MatchThrowable] HandlePickup() called. autoLightOnPickup: {autoLightOnPickup}");
            }

            if (autoLightOnPickup)
            {
                LightMatch();

                if (debugMode)
                {
                    Debug.Log("[MatchThrowable] Auto-lit match on pickup.");
                }
            }
            else
            {
                if (debugMode)
                {
                    Debug.Log("[MatchThrowable] Pickup received but auto-light is disabled.");
                }
            }
        }

        #endregion

        #region Editor

        private void OnValidate()
        {
            // Ensure values are valid in editor
            burnDuration = Mathf.Max(0.1f, burnDuration);
            flameRadius = Mathf.Max(0.1f, flameRadius);
            damagePerSecond = Mathf.Max(0f, damagePerSecond);
            ignitionRadius = Mathf.Max(0.1f, ignitionRadius);
        }

        #endregion
    }
}
