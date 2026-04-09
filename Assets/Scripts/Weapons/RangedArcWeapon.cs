using UnityEngine;
using System.Collections.Generic;

namespace FindersCheesers
{
    [AddComponentMenu("Finders Cheesers/Weapons/RangedArcWeapon")]
    public class RangedArcWeapon : MonoBehaviour, IRangedWeapon
    {
        #region Settings

        [Header("Weapon Settings")]
        [Tooltip("The projectile prefab to fire")]
        [SerializeField]
        private GameObject projectilePrefab;

        [Tooltip("Damage dealt by projectiles")]
        [SerializeField]
        private float damage = 10f;

        [Tooltip("Launch speed of the projectile")]
        [SerializeField]
        private float launchSpeed = 20f;

        [Tooltip("Launch angle in degrees (0 = horizontal, 90 = straight up)")]
        [SerializeField]
        private float launchAngle = 45f;

        [Tooltip("Gravity multiplier applied to the projectile")]
        [SerializeField]
        private float gravityMultiplier = 1f;

        [Header("Auto Trajectory")]
        [Tooltip("Automatically calculate launch angle and speed to hit the target")]
        [SerializeField]
        private bool autoCalculateTrajectory = false;

        [Tooltip("When auto-calculating, prefer the high arc solution over the low arc")]
        [SerializeField]
        private bool preferHighArc = false;

        [Tooltip("Target transform to auto-aim at (used by auto-fire and gizmos)")]
        [SerializeField]
        private Transform targetTransform;

        [Tooltip("Minimum launch speed when auto-calculating")]
        [SerializeField]
        private float minLaunchSpeed = 5f;

        [Tooltip("Maximum launch speed when auto-calculating")]
        [SerializeField]
        private float maxLaunchSpeed = 50f;

        [Tooltip("Time between shots (in seconds)")]
        [SerializeField]
        private float fireRate = 1f;

        [Tooltip("Whether to use automatic fire")]
        [SerializeField]
        private bool automaticFire = false;

        [Tooltip("Spread angle for projectiles (in degrees)")]
        [SerializeField]
        private float spreadAngle = 0f;

        [Tooltip("Number of projectiles per shot")]
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

        [Tooltip("Show predicted trajectory gizmos in the scene")]
        [SerializeField]
        private bool showGizmos = true;

        [Tooltip("Number of trajectory preview points")]
        [SerializeField]
        private int trajectoryPreviewSteps = 30;

        [Tooltip("Time step between trajectory preview points")]
        [SerializeField]
        private float trajectoryPreviewStep = 0.1f;

        #endregion

        #region Events

        public event System.Action OnFired;
        public event System.Action<Collider> OnProjectileHit;
        public event System.Action OnFireStarted;
        public event System.Action OnFireStopped;

        #endregion

        #region Properties

        public bool IsFiring { get; private set; }
        public bool CanFire => !IsOnCooldown;
        public bool IsOnCooldown { get; private set; }
        public float RemainingCooldown { get; private set; }

        public float Damage
        {
            get => damage;
            set => damage = Mathf.Max(0f, value);
        }

        public float FireRate
        {
            get => fireRate;
            set => fireRate = Mathf.Max(0.01f, value);
        }

        public float LaunchSpeed
        {
            get => launchSpeed;
            set => launchSpeed = Mathf.Max(0.1f, value);
        }

        public float LaunchAngle
        {
            get => launchAngle;
            set => launchAngle = Mathf.Clamp(value, 1f, 89f);
        }

        public float GravityMultiplier
        {
            get => gravityMultiplier;
            set => gravityMultiplier = Mathf.Max(0.01f, value);
        }

        public bool AutomaticFire
        {
            get => automaticFire;
            set => automaticFire = value;
        }

        public bool AutoCalculateTrajectory
        {
            get => autoCalculateTrajectory;
            set => autoCalculateTrajectory = value;
        }

        public bool PreferHighArc
        {
            get => preferHighArc;
            set => preferHighArc = value;
        }

        public Transform TargetTransform
        {
            get => targetTransform;
            set => targetTransform = value;
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
            fireTransform = muzzleTransform != null ? muzzleTransform : transform;

            if (useObjectPooling)
            {
                InitializeObjectPool();
            }

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
            foreach (var projectile in activeProjectiles)
            {
                if (projectile != null)
                {
                    projectile.OnHit -= HandleProjectileHit;
                }
            }
            activeProjectiles.Clear();

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

        public bool TryFire()
        {
            if (!CanFire)
            {
                if (debugMode)
                {
                    Debug.Log("[RangedArcWeapon] Cannot fire - on cooldown");
                }
                return false;
            }

            if (projectilePrefab == null)
            {
                Debug.LogError("[RangedArcWeapon] No projectile prefab assigned!");
                return false;
            }

            Fire();
            return true;
        }

        public void Fire()
        {
            if (debugMode)
            {
                Debug.Log("[RangedArcWeapon] Firing weapon");
            }

            IsOnCooldown = true;
            cooldownTimer = fireRate;
            RemainingCooldown = fireRate;

            for (int i = 0; i < projectilesPerShot; i++)
            {
                FireProjectile(i);
            }

            ShowMuzzleFlash();
            OnFired?.Invoke();
        }

        public void StartFiring()
        {
            if (!automaticFire)
            {
                Debug.LogWarning("[RangedArcWeapon] Cannot start continuous firing - automatic fire is disabled");
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
                Debug.Log("[RangedArcWeapon] Started firing");
            }

            TryFire();
        }

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
                Debug.Log("[RangedArcWeapon] Stopped firing");
            }
        }

        public void ResetCooldown()
        {
            cooldownTimer = 0f;
            IsOnCooldown = false;
            RemainingCooldown = 0f;

            if (debugMode)
            {
                Debug.Log("[RangedArcWeapon] Fire cooldown reset");
            }
        }

        public void SetProjectilePrefab(GameObject prefab)
        {
            projectilePrefab = prefab;

            if (useObjectPooling && projectilePool != null)
            {
                ClearObjectPool();
                InitializeObjectPool();
            }
        }

        public bool FireAt(Vector3 targetPosition)
        {
            if (!CanFire)
            {
                return false;
            }

            if (autoCalculateTrajectory)
            {
                return FireAtAutoCalculated(targetPosition);
            }

            return FireAtFixedAngle(targetPosition);
        }

        private bool FireAtAutoCalculated(Vector3 targetPosition)
        {
            Vector3 firePos = fireTransform.position;
            float gravity = Mathf.Abs(Physics.gravity.y) * gravityMultiplier;

            Vector3 horizontalDir = new Vector3(
                targetPosition.x - firePos.x, 0f, targetPosition.z - firePos.z
            );
            float horizontalDistance = horizontalDir.magnitude;
            horizontalDir.Normalize();

            float verticalDistance = targetPosition.y - firePos.y;

            if (horizontalDistance < 0.01f)
            {
                horizontalDistance = 0.01f;
            }

            float angleRad;
            float calculatedSpeed;

            if (preferHighArc)
            {
                angleRad = 75f * Mathf.Deg2Rad;
            }
            else
            {
                angleRad = 45f * Mathf.Deg2Rad;
            }

            float speedSquared = gravity * horizontalDistance * horizontalDistance /
                (2f * Mathf.Cos(angleRad) * Mathf.Cos(angleRad) *
                 (horizontalDistance * Mathf.Tan(angleRad) - verticalDistance));

            if (speedSquared < 0f)
            {
                angleRad = preferHighArc ? 45f * Mathf.Deg2Rad : 75f * Mathf.Deg2Rad;

                speedSquared = gravity * horizontalDistance * horizontalDistance /
                    (2f * Mathf.Cos(angleRad) * Mathf.Cos(angleRad) *
                     (horizontalDistance * Mathf.Tan(angleRad) - verticalDistance));
            }

            if (speedSquared < 0f)
            {
                if (debugMode)
                {
                    Debug.LogWarning($"[RangedArcWeapon] Target at {targetPosition} is unreachable");
                }
                return false;
            }

            calculatedSpeed = Mathf.Sqrt(speedSquared);
            calculatedSpeed = Mathf.Clamp(calculatedSpeed, minLaunchSpeed, maxLaunchSpeed);

            Vector3 launchVelocity = new Vector3(
                horizontalDir.x * Mathf.Cos(angleRad) * calculatedSpeed,
                Mathf.Sin(angleRad) * calculatedSpeed,
                horizontalDir.z * Mathf.Cos(angleRad) * calculatedSpeed
            );

            Vector3 arcDirection = launchVelocity.normalized;

            IsOnCooldown = true;
            cooldownTimer = fireRate;
            RemainingCooldown = fireRate;

            for (int i = 0; i < projectilesPerShot; i++)
            {
                FireProjectileWithVelocity(arcDirection, launchVelocity, i);
            }

            ShowMuzzleFlash();
            OnFired?.Invoke();
            return true;
        }

        private bool FireAtFixedAngle(Vector3 targetPosition)
        {

            Vector3 firePos = fireTransform.position;
            float horizontalDistance = Vector3.Distance(
                new Vector3(firePos.x, 0f, firePos.z),
                new Vector3(targetPosition.x, 0f, targetPosition.z)
            );

            float verticalDistance = targetPosition.y - firePos.y;
            float gravity = Mathf.Abs(Physics.gravity.y) * gravityMultiplier;

            float speedSquared = launchSpeed * launchSpeed;
            float speedFourth = speedSquared * speedSquared;

            float discriminant = speedFourth - gravity *
                (gravity * horizontalDistance * horizontalDistance + 2f * verticalDistance * speedSquared);

            if (discriminant < 0f)
            {
                if (debugMode)
                {
                    Debug.LogWarning($"[RangedArcWeapon] Target at {targetPosition} is out of range");
                }
                return false;
            }

            float discriminantSqrt = Mathf.Sqrt(discriminant);

            float lowAngle = Mathf.Atan2(
                speedSquared - discriminantSqrt,
                gravity * horizontalDistance
            ) * Mathf.Rad2Deg;

            float highAngle = Mathf.Atan2(
                speedSquared + discriminantSqrt,
                gravity * horizontalDistance
            ) * Mathf.Rad2Deg;

            float angleToUse = Mathf.Abs(launchAngle - highAngle) < Mathf.Abs(launchAngle - lowAngle)
                ? highAngle
                : lowAngle;

            Vector3 horizontalDir = new Vector3(
                targetPosition.x - firePos.x, 0f, targetPosition.z - firePos.z
            ).normalized;

            float angleRad = angleToUse * Mathf.Deg2Rad;
            Vector3 launchVelocity = new Vector3(
                horizontalDir.x * Mathf.Cos(angleRad) * launchSpeed,
                Mathf.Sin(angleRad) * launchSpeed,
                horizontalDir.z * Mathf.Cos(angleRad) * launchSpeed
            );

            Vector3 originalDirection = fireDirection;
            bool originalUseFacing = fireInFacingDirection;

            fireDirection = launchVelocity.normalized;
            fireInFacingDirection = false;

            bool result = TryFire();

            if (result)
            {
                var lastProjectile = activeProjectiles.Count > 0 ? activeProjectiles[activeProjectiles.Count - 1] : null;
                if (lastProjectile is ArcProjectile arcProj)
                {
                    arcProj.SetVelocity(launchVelocity);
                }
            }

            fireDirection = originalDirection;
            fireInFacingDirection = originalUseFacing;

            return result;
        }

        public Vector3[] GetTrajectoryPoints(Vector3? fromPosition = null, Vector3? targetPosition = null)
        {
            Transform currentTransform = muzzleTransform != null ? muzzleTransform : transform;
            Vector3 origin = fromPosition ?? currentTransform.position;

            if (currentTransform == null)
            {
                return System.Array.Empty<Vector3>();
            }

            Vector3 direction;
            float trajectorySpeed = launchSpeed;

            if (targetPosition.HasValue)
            {
                if (autoCalculateTrajectory)
                {
                    Vector3 hDir = new Vector3(
                        targetPosition.Value.x - origin.x, 0f, targetPosition.Value.z - origin.z
                    );
                    float hDist = hDir.magnitude;
                    hDir.Normalize();
                    float vDist = targetPosition.Value.y - origin.y;

                    if (hDist < 0.01f) hDist = 0.01f;

                    float grav = Mathf.Abs(Physics.gravity.y) * gravityMultiplier;
                    float angleRad = preferHighArc ? 75f * Mathf.Deg2Rad : 45f * Mathf.Deg2Rad;

                    float speedSq = grav * hDist * hDist /
                        (2f * Mathf.Cos(angleRad) * Mathf.Cos(angleRad) *
                         (hDist * Mathf.Tan(angleRad) - vDist));

                    if (speedSq < 0f)
                    {
                        angleRad = preferHighArc ? 45f * Mathf.Deg2Rad : 75f * Mathf.Deg2Rad;
                        speedSq = grav * hDist * hDist /
                            (2f * Mathf.Cos(angleRad) * Mathf.Cos(angleRad) *
                             (hDist * Mathf.Tan(angleRad) - vDist));
                    }

                    if (speedSq < 0f)
                    {
                        return System.Array.Empty<Vector3>();
                    }

                    trajectorySpeed = Mathf.Clamp(Mathf.Sqrt(speedSq), minLaunchSpeed, maxLaunchSpeed);

                    direction = new Vector3(
                        hDir.x * Mathf.Cos(angleRad),
                        Mathf.Sin(angleRad),
                        hDir.z * Mathf.Cos(angleRad)
                    ).normalized;
                }
                else
                {
                    float hDist = Vector3.Distance(
                        new Vector3(origin.x, 0f, origin.z),
                        new Vector3(targetPosition.Value.x, 0f, targetPosition.Value.z)
                    );
                    float vDist = targetPosition.Value.y - origin.y;
                    float gravity = Mathf.Abs(Physics.gravity.y) * gravityMultiplier;
                    float speedSq = launchSpeed * launchSpeed;
                    float speedFth = speedSq * speedSq;
                    float disc = speedFth - gravity * (gravity * hDist * hDist + 2f * vDist * speedSq);

                    if (disc < 0f)
                    {
                        return System.Array.Empty<Vector3>();
                    }

                    float discSqrt = Mathf.Sqrt(disc);
                    float highAngle = Mathf.Atan2(speedSq + discSqrt, gravity * hDist);
                    float lowAngle = Mathf.Atan2(speedSq - discSqrt, gravity * hDist);

                    float angleRad = Mathf.Abs(launchAngle * Mathf.Deg2Rad - highAngle) <
                                     Mathf.Abs(launchAngle * Mathf.Deg2Rad - lowAngle)
                        ? highAngle
                        : lowAngle;

                    Vector3 hDir = new Vector3(
                        targetPosition.Value.x - origin.x, 0f, targetPosition.Value.z - origin.z
                    ).normalized;

                    direction = new Vector3(
                        hDir.x * Mathf.Cos(angleRad),
                        Mathf.Sin(angleRad),
                        hDir.z * Mathf.Cos(angleRad)
                    ).normalized;
                }
            }
            else
            {
                Vector3 baseDir = fireInFacingDirection ? currentTransform.forward : fireDirection.normalized;
                float angleRad = launchAngle * Mathf.Deg2Rad;
                Vector3 right = Vector3.Cross(Vector3.up, baseDir).normalized;

                direction = (baseDir * Mathf.Cos(angleRad) + Vector3.up * Mathf.Sin(angleRad)).normalized;
                if (right.sqrMagnitude > 0.001f)
                {
                    direction = Quaternion.AngleAxis(0f, right) * direction;
                }
            }

            Vector3 currentPos = origin;
            Vector3 currentVel = direction * trajectorySpeed;
            float gravityMag = Mathf.Abs(Physics.gravity.y) * gravityMultiplier;
            Vector3 gravityVec = Vector3.down * gravityMag;

            Vector3[] points = new Vector3[trajectoryPreviewSteps + 1];
            points[0] = currentPos;

            for (int i = 1; i <= trajectoryPreviewSteps; i++)
            {
                currentVel += gravityVec * trajectoryPreviewStep;
                currentPos += currentVel * trajectoryPreviewStep;
                points[i] = currentPos;

                if (currentPos.y < origin.y - 5f && i > 2)
                {
                    System.Array.Resize(ref points, i + 1);
                    break;
                }
            }

            return points;
        }

        #endregion

        #region Private Methods

        private void FireProjectile(int projectileIndex)
        {
            GameObject projectileObj = GetProjectile();

            if (projectileObj == null)
            {
                Debug.LogError("[RangedArcWeapon] Failed to get projectile!");
                return;
            }

            IProjectile projectile = projectileObj.GetComponent<IProjectile>();

            if (projectile == null)
            {
                Debug.LogError("[RangedArcWeapon] Projectile prefab does not have an IProjectile component!");
                return;
            }

            Vector3 baseDirection = fireInFacingDirection ? fireTransform.forward : fireDirection.normalized;

            float angleRad = launchAngle * Mathf.Deg2Rad;
            Vector3 arcDirection = (baseDirection * Mathf.Cos(angleRad) + Vector3.up * Mathf.Sin(angleRad)).normalized;

            if (spreadAngle > 0f)
            {
                float spreadRadians = spreadAngle * Mathf.Deg2Rad;
                float horizontalSpread = Random.Range(-spreadRadians, spreadRadians);
                float verticalSpread = Random.Range(-spreadRadians, spreadRadians);

                if (projectilesPerShot > 1)
                {
                    float step = spreadRadians * 2f / (projectilesPerShot - 1);
                    horizontalSpread = -spreadRadians + step * projectileIndex;
                }

                Quaternion spreadRotation = Quaternion.Euler(
                    verticalSpread * Mathf.Rad2Deg, horizontalSpread * Mathf.Rad2Deg, 0f
                );
                arcDirection = spreadRotation * arcDirection;
            }

            arcDirection = arcDirection.normalized;

            Vector3 launchVel = arcDirection * launchSpeed;
            FireProjectileInternal(projectile, arcDirection, launchVel, projectileIndex);
        }

        private void FireProjectileWithVelocity(Vector3 direction, Vector3 velocity, int projectileIndex)
        {
            GameObject projectileObj = GetProjectile();

            if (projectileObj == null)
            {
                Debug.LogError("[RangedArcWeapon] Failed to get projectile!");
                return;
            }

            IProjectile projectile = projectileObj.GetComponent<IProjectile>();

            if (projectile == null)
            {
                Debug.LogError("[RangedArcWeapon] Projectile prefab does not have an IProjectile component!");
                return;
            }

            Vector3 finalDirection = direction;
            Vector3 finalVelocity = velocity;

            if (spreadAngle > 0f)
            {
                float spreadRadians = spreadAngle * Mathf.Deg2Rad;
                float horizontalSpread = Random.Range(-spreadRadians, spreadRadians);
                float verticalSpread = Random.Range(-spreadRadians, spreadRadians);

                if (projectilesPerShot > 1)
                {
                    float step = spreadRadians * 2f / (projectilesPerShot - 1);
                    horizontalSpread = -spreadRadians + step * projectileIndex;
                }

                Quaternion spreadRotation = Quaternion.Euler(
                    verticalSpread * Mathf.Rad2Deg, horizontalSpread * Mathf.Rad2Deg, 0f
                );
                finalDirection = (spreadRotation * finalDirection).normalized;
                finalVelocity = spreadRotation * finalVelocity;
            }

            FireProjectileInternal(projectile, finalDirection, finalVelocity, projectileIndex);
        }

        private void FireProjectileInternal(IProjectile projectile, Vector3 direction, Vector3 velocity, int projectileIndex)
        {
            float speed = velocity.magnitude;
            projectile.Fire(fireTransform.position, direction, speed, damage, gameObject);

            if (projectile is ArcProjectile arcProjectile)
            {
                arcProjectile.SetVelocity(velocity);
            }

            projectile.OnHit += HandleProjectileHit;
            activeProjectiles.Add(projectile);

            if (debugMode)
            {
                Debug.Log($"[RangedArcWeapon] Fired projectile {projectileIndex + 1}/{projectilesPerShot}");
            }
        }

        private GameObject GetProjectile()
        {
            if (useObjectPooling)
            {
                foreach (var pooledProjectile in projectilePool)
                {
                    if (pooledProjectile != null && !pooledProjectile.activeInHierarchy)
                    {
                        pooledProjectile.SetActive(true);
                        return pooledProjectile;
                    }
                }

                return CreateProjectile();
            }

            return CreateProjectile();
        }

        private GameObject CreateProjectile()
        {
            GameObject projectile = Instantiate(projectilePrefab, fireTransform.position, fireTransform.rotation);
            return projectile;
        }

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
                Debug.Log($"[RangedArcWeapon] Initialized object pool with {initialPoolSize} projectiles");
            }
        }

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

        private void ShowMuzzleFlash()
        {
            if (muzzleFlashPrefab == null)
            {
                return;
            }

            if (currentMuzzleFlash != null)
            {
                Destroy(currentMuzzleFlash);
            }

            currentMuzzleFlash = Instantiate(muzzleFlashPrefab, fireTransform.position, fireTransform.rotation, fireTransform);
            Destroy(currentMuzzleFlash, muzzleFlashDuration);
        }

        private void HandleProjectileHit(Collider hitCollider)
        {
            OnProjectileHit?.Invoke(hitCollider);

            if (debugMode)
            {
                Debug.Log($"[RangedArcWeapon] Projectile hit {hitCollider.name}");
            }
        }

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

        private void UpdateFiring()
        {
            if (!IsFiring || !automaticFire)
            {
                return;
            }

            if (!IsOnCooldown && CanFire)
            {
                TryFire();
            }
        }

        private void UpdateActiveProjectiles()
        {
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
            launchSpeed = Mathf.Max(0.1f, launchSpeed);
            launchAngle = Mathf.Clamp(launchAngle, 1f, 89f);
            gravityMultiplier = Mathf.Max(0.01f, gravityMultiplier);
            spreadAngle = Mathf.Max(0f, spreadAngle);
            projectilesPerShot = Mathf.Max(1, projectilesPerShot);
            initialPoolSize = Mathf.Max(1, initialPoolSize);
            muzzleFlashDuration = Mathf.Max(0f, muzzleFlashDuration);
            minLaunchSpeed = Mathf.Max(0.1f, minLaunchSpeed);
            maxLaunchSpeed = Mathf.Max(minLaunchSpeed, maxLaunchSpeed);
        }

        private void OnDrawGizmos()
        {
            if (!showGizmos)
            {
                return;
            }

            Transform currentFireTransform = muzzleTransform != null ? muzzleTransform : transform;
            if (currentFireTransform == null)
            {
                return;
            }

            if (autoCalculateTrajectory && targetTransform != null)
            {
                Vector3[] trajectoryPoints = GetTrajectoryPoints(
                    currentFireTransform.position, targetTransform.position
                );
                if (trajectoryPoints.Length > 1)
                {
                    Gizmos.color = Color.green;

                    for (int i = 0; i < trajectoryPoints.Length - 1; i++)
                    {
                        Gizmos.DrawLine(trajectoryPoints[i], trajectoryPoints[i + 1]);
                    }

                    foreach (var point in trajectoryPoints)
                    {
                        Gizmos.DrawWireSphere(point, 0.05f);
                    }
                }

                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(targetTransform.position, 0.3f);
            }
            else
            {
                Vector3[] trajectoryPoints = GetTrajectoryPoints(
                    currentFireTransform.position
                );
                if (trajectoryPoints.Length > 1)
                {
                    Gizmos.color = Color.yellow;

                    for (int i = 0; i < trajectoryPoints.Length - 1; i++)
                    {
                        Gizmos.DrawLine(trajectoryPoints[i], trajectoryPoints[i + 1]);
                    }

                    foreach (var point in trajectoryPoints)
                    {
                        Gizmos.DrawWireSphere(point, 0.05f);
                    }
                }

                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(currentFireTransform.position, launchSpeed * launchSpeed / (2f * Mathf.Abs(Physics.gravity.y) * gravityMultiplier));
            }
        }

        #endregion
    }
}
