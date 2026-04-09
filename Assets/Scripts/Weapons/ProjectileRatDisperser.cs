using UnityEngine;

namespace FindersCheesers
{
    [AddComponentMenu("Finders Cheesers/Weapons/Projectile Rat Disperser")]
    [RequireComponent(typeof(BaseProjectile))]
    public class ProjectileRatDisperser : MonoBehaviour
    {
        [Header("Disperse Settings")]
        [Tooltip("Number of rats to disperse on hit")]
        [SerializeField]
        private int disperseAmount = 3;

        [Header("Debug")]
        [Tooltip("Show debug information in the console")]
        [SerializeField]
        private bool debugMode = false;

        private BaseProjectile projectile;

        private void Awake()
        {
            projectile = GetComponent<BaseProjectile>();
        }

        private void OnEnable()
        {
            if (projectile != null)
            {
                projectile.OnHit += OnProjectileHit;
            }
        }

        private void OnDisable()
        {
            if (projectile != null)
            {
                projectile.OnHit -= OnProjectileHit;
            }
        }

        private void OnProjectileHit(Collider hitCollider)
        {
            if (hitCollider == null)
            {
                return;
            }

            RatInventory ratInventory = hitCollider.GetComponent<RatInventory>();

            if (ratInventory == null)
            {
                ratInventory = hitCollider.GetComponentInParent<RatInventory>();
            }

            if (ratInventory == null)
            {
                if (debugMode)
                {
                    Debug.Log($"[ProjectileRatDisperser] Hit object '{hitCollider.name}' has no RatInventory.");
                }
                return;
            }

            int dispersed = ratInventory.DisperseRats(disperseAmount);

            if (debugMode)
            {
                Debug.Log($"[ProjectileRatDisperser] Dispersed {dispersed} rats from '{hitCollider.name}'.");
            }
        }
    }
}
