using UnityEngine;

namespace FindersCheesers
{
    /// <summary>
    /// Interface for projectiles that can be fired by ranged weapons.
    /// Provides a common contract for different projectile types.
    /// </summary>
    public interface IProjectile
    {
        /// <summary>
        /// Gets whether the projectile is currently active/flying.
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// Gets the damage dealt by this projectile.
        /// </summary>
        float Damage { get; }

        /// <summary>
        /// Gets the owner/creator of this projectile (who fired it).
        /// </summary>
        GameObject Owner { get; }

        /// <summary>
        /// Event fired when the projectile hits something.
        /// </summary>
        event System.Action<Collider> OnHit;

        /// <summary>
        /// Event fired when the projectile is destroyed.
        /// </summary>
        event System.Action OnDestroyed;

        /// <summary>
        /// Initializes and fires the projectile.
        /// </summary>
        /// <param name="origin">The starting position of the projectile.</param>
        /// <param name="direction">The direction to fire in (normalized).</param>
        /// <param name="speed">The speed of the projectile.</param>
        /// <param name="damage">The damage to deal on impact.</param>
        /// <param name="owner">The GameObject that fired this projectile.</param>
        void Fire(Vector3 origin, Vector3 direction, float speed, float damage, GameObject owner);

        /// <summary>
        /// Deactivates/destroys the projectile.
        /// </summary>
        void Deactivate();
    }
}
