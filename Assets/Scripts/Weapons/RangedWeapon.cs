using UnityEngine;
using System.Collections.Generic;

namespace FindersCheesers
{
    /// <summary>
    /// A ranged weapon component that fires projectile prefabs.
    /// Supports both rigidbody and bullet-style projectiles.
    /// </summary>
    [AddComponentMenu("Finders Cheesers/Weapons/RangedWeapon")]
    public class RangedWeapon : MonoBehaviour
    {
        #region Settings

        [Header("Weapon Settings")]
        [Tooltip("The projectile prefab to fire")]
        [SerializeField]
        private GameObject projectilePrefab;

        [Tooltip("Damage dealt by projectiles")]
        [SerializeField]
        private float damage = 10f;

        [Tooltip("Speed of the projectile")]
        [SerializeField]
        private float projectileSpeed = 20f;

        [Tooltip("Time between shots (in seconds)")]
        [SerializeField]
        private float fireRate = 0.5f;

        [Tooltip("Maximum range of the weapon")]
        [SerializeField]
        private float maxRange = 100f;

        [Tooltip("Whether to use automatic fire")]
        [SerializeField]
        private bool automaticFire = false;

        [Tooltip("Spread angle for projectiles (in degrees)")]
        [SerializeField]
        private float spreadAngle = 0f;

        [Tooltip("Number of projectiles per shot (for shotguns, etc.)")]
        [SerializeField]
        private int projectilesPerShot = 1;

        [Header("Firing Settings")]
        [Tooltip("Transform that represents the muzzle/barrel end")]
        [SerializeField]
        private Transform muzzleTransform;

        [Tooltip("Whether to fire in the direction the weapon is facing")]
        [SerializeField]
        private bool fireInFacingDirection = true;

        [Tooltip("Custom fire direction (if not firing in facing direction)")]
        [SerializeField]
        private Vector3 fireDirection = Vector3.forward;

        [Header("Object Pooling")]
        [Tooltip("Whether to use object pooling for projectiles")]
        [SerializeField]
        private bool useObjectPooling = true;

        [Tooltip("Initial pool size for projectiles")]
        [SerializeField]
        private int initialPoolSize = 10;

        [Header("Effects")]
        [Tooltip("Optional muzzle flash effect")]
        [SerializeField]
        private GameObject muzzleFlashPrefab;

        [Tooltip("Duration of muzzle flash (in seconds)")]
        [SerializeField]
        private float muzzleFlashDuration = 0.1f;

        [Header("Debug")]
        [Tooltip("Show debug information in the console")]
        [SerializeField]
        private bool debugMode = false;

        [Tooltip("Show fire direction gizmos in the scene")]
        [SerializeField]
        private bool showGizmos = true;

        #endregion

        #region Events

        /// <summary>
        /// Event fired when the weapon fires.
        /// </summary>
        public event System.Action OnFired;

        /// <summary>
        /// Event fired when a projectile hits something.
        /// </summary>
        public event System.Action<Collider> OnProjectileHit;

        /// <summary>
        /// Event fired when the weapon starts firing.
        /// </summary>
        public event System.Action OnFireStarted;

        /// <summary>
        /// Event fired when the weapon stops firing.
        /// </summary>
        public event System.Action OnFireStopped;

        #endregion

        #region Properties

        /// <summary>
        /// Gets whether the weapon is currently firing.
        /// </summary>
        public bool IsFiring { get; private set; }

        /// <summary>
        /// Gets whether the weapon can fire (not on cooldown).
        /// </summary>
        public bool CanFire => !IsOnCooldown;

        /// <summary>
        /// Gets whether the weapon is currently on cooldown.
        /// </summary>
        public bool IsOnCooldown { get; private set; }

        /// <summary>
        /// Gets the remaining cooldown time.
        /// </summary>
        public float RemainingCooldown { get; private set; }

        /// <summary>
        /// Gets or sets the damage.
        /// </summary>
        public float Damage
        {
            get => damage;
            set => damage = Mathf.Max(0f, value);
        }

        /// <summary>
        /// Gets or sets the fire rate.
        /// </summary>
        public float FireRate
        {
            get => fireRate;
            set => fireRate = Mathf.Max(0.01f, value);
        }

        /// <summary>
        /// Gets or sets the projectile speed.
        /// </summary>
        public float ProjectileSpeed
        {
            get => projectileSpeed;
            set => projectileSpeed = Mathf.Max(0.1f, value);
        }

        /// <summary>
        /// Gets or sets whether automatic fire is enabled.
        /// </summary>
        public bool AutomaticFire
        {
            get => automaticFire;
            set => automaticFire = value;
        }

        #endregion

        #region Component References

        private Transform fireTransform;

        #endregion

        #region State Variables

        private float cooldownTimer;
        private List<GameObject> projectilePool;
        private List<IProjectile> activeProjectiles;
        private GameObject currentMuzzleFlash;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Set fire transform (use muzzle transform if available, otherwise use this transform)
            fireTransform = muzzleTransform != null ? muzzleTransform : transform;

            // Initialize object pool if enabled
            if (useObjectPooling)
            {
                InitializeObjectPool();
            }

            // Initialize active projectiles list
            activeProjectiles = new List<IProjectile>();
        }

        private void Update()
        {
            UpdateCooldown();
            UpdateFiring();
            UpdateActiveProjectiles();
        }

        private void OnDestroy()
        {
            // Clean up active projectiles
            foreach (var projectile in activeProjectiles)
            {
                if (projectile != null)
                {
                    projectile.OnHit -= HandleProjectileHit;
                }
            }
            activeProjectiles.Clear();

            // Clean up object pool
            if (projectilePool != null)
            {
                foreach (var pooledProjectile in projectilePool)
                {
                    if (pooledProjectile != null)
                    {
                        Destroy(pooledProjectile);
                    }
                }
                projectilePool.Clear();
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Attempts to fire the weapon.
        /// </summary>
        /// <returns>True if the weapon fired, false otherwise.</returns>
        public bool TryFire()
        {
            if (!CanFire)
            {
                if (debugMode)
                {
                    Debug.Log("[RangedWeapon] Cannot fire - on cooldown");
                }
                return false;
            }

            if (projectilePrefab == null)
            {
                Debug.LogError("[RangedWeapon] No projectile prefab assigned!");
                return false;
            }

            Fire();
            return true;
        }

        /// <summary>
        /// Fires the weapon once.
        /// </summary>
        public void Fire()
        {
            if (debugMode)
            {
                Debug.Log("[RangedWeapon] Firing weapon");
            }

            // Start cooldown
            IsOnCooldown = true;
            cooldownTimer = fireRate;
            RemainingCooldown = fireRate;

            // Fire projectiles
            for (int i = 0; i < projectilesPerShot; i++)
            {
                FireProjectile(i);
            }

            // Show muzzle flash
            ShowMuzzleFlash();

            // Fire event
            OnFired?.Invoke();
        }

        /// <summary>
        /// Starts continuous firing.
        /// </summary>
        public void StartFiring()
        {
            if (!automaticFire)
            {
                Debug.LogWarning("[RangedWeapon] Cannot start continuous firing - automatic fire is disabled");
                return;
            }

            if (IsFiring)
            {
                return;
            }

            IsFiring = true;
            OnFireStarted?.Invoke();

            if (debugMode)
            {
                Debug.Log("[RangedWeapon] Started firing");
            }

            // Fire immediately
            TryFire();
        }

        /// <summary>
        /// Stops continuous firing.
        /// </summary>
        public void StopFiring()
        {
            if (!IsFiring)
            {
                return;
            }

            IsFiring = false;
            OnFireStopped?.Invoke();

            if (debugMode)
            {
                Debug.Log("[RangedWeapon] Stopped firing");
            }
        }

        /// <summary>
        /// Resets the fire cooldown.
        /// </summary>
        public void ResetCooldown()
        {
            cooldownTimer = 0f;
            IsOnCooldown = false;
            RemainingCooldown = 0f;

            if (debugMode)
            {
                Debug.Log("[RangedWeapon] Fire cooldown reset");
            }
        }

        /// <summary>
        /// Sets the projectile prefab.
        /// </summary>
        /// <param name="prefab">The new projectile prefab.</param>
        public void SetProjectilePrefab(GameObject prefab)
        {
            projectilePrefab = prefab;

            // Reinitialize object pool if needed
            if (useObjectPooling && projectilePool != null)
            {
                ClearObjectPool();
                InitializeObjectPool();
            }
        }

        /// <summary>
        /// Fires at a specific target position.
        /// </summary>
        /// <param name="targetPosition">The target position to fire at.</param>
        /// <returns>True if the weapon fired, false otherwise.</returns>
        public bool FireAt(Vector3 targetPosition)
        {
            if (!CanFire)
            {
                return false;
            }

            // Calculate direction to target
            Vector3 direction = (targetPosition - fireTransform.position).normalized;

            // Temporarily override fire direction
            Vector3 originalDirection = fireDirection;
            bool originalUseFacing = fireInFacingDirection;

            fireDirection = direction;
            fireInFacingDirection = false;

            // Fire
            bool result = TryFire();

            // Restore original settings
            fireDirection = originalDirection;
            fireInFacingDirection = originalUseFacing;

            return result;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Fires a single projectile.
        /// </summary>
        /// <param name="projectileIndex">The index of the projectile (for multi-shot weapons).</param>
        private void FireProjectile(int projectileIndex)
        {
            // Get or create projectile
            GameObject projectileObj = GetProjectile();

            if (projectileObj == null)
            {
                Debug.LogError("[RangedWeapon] Failed to get projectile!");
                return;
            }

            // Get projectile component
            IProjectile projectile = projectileObj.GetComponent<IProjectile>();

            if (projectile == null)
            {
                Debug.LogError("[RangedWeapon] Projectile prefab does not have an IProjectile component!");
                return;
            }

            // Calculate fire direction with spread
            Vector3 direction = CalculateFireDirection(projectileIndex);

            // Fire the projectile
            projectile.Fire(fireTransform.position, direction, projectileSpeed, damage, gameObject);

            // Subscribe to hit event
            projectile.OnHit += HandleProjectileHit;

            // Track active projectile
            activeProjectiles.Add(projectile);

            if (debugMode)
            {
                Debug.Log($"[RangedWeapon] Fired projectile {projectileIndex + 1}/{projectilesPerShot}");
            }
        }

        /// <summary>
        /// Calculates the fire direction with spread applied.
        /// </summary>
        /// <param name="projectileIndex">The index of the projectile.</param>
        /// <returns>The fire direction.</returns>
        private Vector3 CalculateFireDirection(int projectileIndex)
        {
            Vector3 baseDirection;

            if (fireInFacingDirection)
            {
                baseDirection = fireTransform.forward;
            }
            else
            {
                baseDirection = fireDirection.normalized;
            }

            // Apply spread
            if (spreadAngle > 0f)
            {
                // Calculate spread offset
                float spreadRadians = spreadAngle * Mathf.Deg2Rad;
                float horizontalSpread = Random.Range(-spreadRadians, spreadRadians);
                float verticalSpread = Random.Range(-spreadRadians, spreadRadians);

                // For multi-shot, distribute projectiles more evenly
                if (projectilesPerShot > 1)
                {
                    float step = spreadRadians * 2f / (projectilesPerShot - 1);
                    horizontalSpread = -spreadRadians + step * projectileIndex;
                }

                // Apply rotation for spread
                Quaternion spreadRotation = Quaternion.Euler(verticalSpread * Mathf.Rad2Deg, horizontalSpread * Mathf.Rad2Deg, 0f);
                baseDirection = spreadRotation * baseDirection;
            }

            return baseDirection.normalized;
        }

        /// <summary>
        /// Gets a projectile from the pool or creates a new one.
        /// </summary>
        /// <returns>The projectile GameObject.</returns>
        private GameObject GetProjectile()
        {
            if (useObjectPooling)
            {
                // Try to get from pool
                foreach (var pooledProjectile in projectilePool)
                {
                    if (pooledProjectile != null && !pooledProjectile.activeInHierarchy)
                    {
                        pooledProjectile.SetActive(true);
                        return pooledProjectile;
                    }
                }

                // Pool is empty, create new projectile
                return CreateProjectile();
            }
            else
            {
                // Create new projectile
                return CreateProjectile();
            }
        }

        /// <summary>
        /// Creates a new projectile instance.
        /// </summary>
        /// <returns>The new projectile GameObject.</returns>
        private GameObject CreateProjectile()
        {
            GameObject projectile = Instantiate(projectilePrefab, fireTransform.position, fireTransform.rotation);
            return projectile;
        }

        /// <summary>
        /// Initializes the object pool.
        /// </summary>
        private void InitializeObjectPool()
        {
            if (projectilePrefab == null)
            {
                return;
            }

            projectilePool = new List<GameObject>();

            for (int i = 0; i < initialPoolSize; i++)
            {
                GameObject projectile = CreateProjectile();
                projectile.SetActive(false);
                projectilePool.Add(projectile);
            }

            if (debugMode)
            {
                Debug.Log($"[RangedWeapon] Initialized object pool with {initialPoolSize} projectiles");
            }
        }

        /// <summary>
        /// Clears the object pool.
        /// </summary>
        private void ClearObjectPool()
        {
            if (projectilePool == null)
            {
                return;
            }

            foreach (var projectile in projectilePool)
            {
                if (projectile != null)
                {
                    Destroy(projectile);
                }
            }

            projectilePool.Clear();
        }

        /// <summary>
        /// Shows the muzzle flash effect.
        /// </summary>
        private void ShowMuzzleFlash()
        {
            if (muzzleFlashPrefab == null)
            {
                return;
            }

            // Destroy existing muzzle flash
            if (currentMuzzleFlash != null)
            {
                Destroy(currentMuzzleFlash);
            }

            // Create new muzzle flash
            currentMuzzleFlash = Instantiate(muzzleFlashPrefab, fireTransform.position, fireTransform.rotation, fireTransform);

            // Destroy after duration
            Destroy(currentMuzzleFlash, muzzleFlashDuration);
        }

        /// <summary>
        /// Handles projectile hit events.
        /// </summary>
        private void HandleProjectileHit(Collider hitCollider)
        {
            OnProjectileHit?.Invoke(hitCollider);

            if (debugMode)
            {
                Debug.Log($"[RangedWeapon] Projectile hit {hitCollider.name}");
            }
        }

        /// <summary>
        /// Updates the fire cooldown.
        /// </summary>
        private void UpdateCooldown()
        {
            if (IsOnCooldown)
            {
                cooldownTimer -= Time.deltaTime;
                RemainingCooldown = Mathf.Max(0f, cooldownTimer);

                if (cooldownTimer <= 0f)
                {
                    IsOnCooldown = false;
                    cooldownTimer = 0f;
                }
            }
        }

        /// <summary>
        /// Updates the firing behavior.
        /// </summary>
        private void UpdateFiring()
        {
            if (!IsFiring || !automaticFire)
            {
                return;
            }

            // Try to fire if not on cooldown
            if (!IsOnCooldown && CanFire)
            {
                TryFire();
            }
        }

        /// <summary>
        /// Updates active projectiles and removes inactive ones.
        /// </summary>
        private void UpdateActiveProjectiles()
        {
            // Remove inactive projectiles
            for (int i = activeProjectiles.Count - 1; i >= 0; i--)
            {
                if (activeProjectiles[i] == null || !activeProjectiles[i].IsActive)
                {
                    if (activeProjectiles[i] != null)
                    {
                        activeProjectiles[i].OnHit -= HandleProjectileHit;
                    }
                    activeProjectiles.RemoveAt(i);
                }
            }
        }

        #endregion

        #region Editor

        private void OnValidate()
        {
            damage = Mathf.Max(0f, damage);
            fireRate = Mathf.Max(0.01f, fireRate);
            projectileSpeed = Mathf.Max(0.1f, projectileSpeed);
            spreadAngle = Mathf.Max(0f, spreadAngle);
            projectilesPerShot = Mathf.Max(1, projectilesPerShot);
            initialPoolSize = Mathf.Max(1, initialPoolSize);
            muzzleFlashDuration = Mathf.Max(0f, muzzleFlashDuration);
        }

        private void OnDrawGizmos()
        {
            if (!showGizmos)
            {
                return;
            }

            // Set fire transform
            Transform currentFireTransform = muzzleTransform != null ? muzzleTransform : transform;

            // Draw fire direction
            Gizmos.color = Color.yellow;
            Vector3 direction = fireInFacingDirection ? currentFireTransform.forward : fireDirection.normalized;

            // Draw spread cone
            if (spreadAngle > 0f)
            {
                Vector3 leftDirection = Quaternion.Euler(0f, -spreadAngle, 0f) * direction;
                Vector3 rightDirection = Quaternion.Euler(0f, spreadAngle, 0f) * direction;
                Vector3 upDirection = Quaternion.Euler(spreadAngle, 0f, 0f) * direction;
                Vector3 downDirection = Quaternion.Euler(-spreadAngle, 0f, 0f) * direction;

                Gizmos.DrawRay(currentFireTransform.position, direction * 5f);
                Gizmos.DrawRay(currentFireTransform.position, leftDirection * 5f);
                Gizmos.DrawRay(currentFireTransform.position, rightDirection * 5f);
                Gizmos.DrawRay(currentFireTransform.position, upDirection * 5f);
                Gizmos.DrawRay(currentFireTransform.position, downDirection * 5f);
            }
            else
            {
                Gizmos.DrawRay(currentFireTransform.position, direction * 5f);
            }

            // Draw max range
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(currentFireTransform.position, maxRange);
        }

        #endregion
    }
}
