using UnityEngine;

namespace FindersCheesers
{
    public interface IRangedWeapon
    {
        bool CanFire { get; }
        bool IsOnCooldown { get; }
        bool IsFiring { get; }
        bool AutomaticFire { get; set; }
        float Damage { get; set; }
        float FireRate { get; set; }

        event System.Action OnFired;
        event System.Action<Collider> OnProjectileHit;

        bool TryFire();
        bool FireAt(Vector3 targetPosition);
        void StopFiring();
    }
}
