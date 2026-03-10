using UnityEngine;

namespace FindersCheesers
{
    /// <summary>
    /// Example script demonstrating how to use the RangedWeapon component.
    /// This script shows how to control a ranged weapon with keyboard input.
    /// </summary>
    [AddComponentMenu("Finders Cheesers/Weapons/RangedWeaponExample")]
    public class RangedWeaponExample : MonoBehaviour
    {
        #region Settings

        [Header("Weapon Reference")]
        [Tooltip("The ranged weapon to control")]
        [SerializeField]
        private RangedWeapon weapon;

        [Header("Input Settings")]
        [Tooltip("Input button name for firing")]
        [SerializeField]
        private string fireButton = "Fire1";

        [Tooltip("Input button name for switching to single fire mode")]
        [SerializeField]
        private string singleFireButton = "Fire2";

        [Tooltip("Input button name for switching to automatic fire mode")]
        [SerializeField]
        private string autoFireButton = "Fire3";

        [Header("Debug Settings")]
        [Tooltip("Show debug information in the console")]
        [SerializeField]
        private bool debugMode = true;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Get weapon component if not assigned
            if (weapon == null)
            {
                weapon = GetComponent<RangedWeapon>();
            }

            if (weapon == null)
            {
                Debug.LogError("[RangedWeaponExample] RangedWeapon component not found!");
                enabled = false;
                return;
            }

            // Subscribe to weapon events
            weapon.OnFired += HandleWeaponFired;
            weapon.OnProjectileHit += HandleProjectileHit;
            weapon.OnFireStarted += HandleFireStarted;
            weapon.OnFireStopped += HandleFireStopped;

            if (debugMode)
            {
                Debug.Log("[RangedWeaponExample] Weapon example initialized");
            }
        }

        private void Update()
        {
            if (weapon == null)
            {
                return;
            }

            // Handle fire input
            if (Input.GetButtonDown(fireButton))
            {
                if (weapon.AutomaticFire)
                {
                    weapon.StartFiring();
                }
                else
                {
                    weapon.TryFire();
                }
            }

            // Handle fire release for automatic weapons
            if (Input.GetButtonUp(fireButton))
            {
                if (weapon.AutomaticFire)
                {
                    weapon.StopFiring();
                }
            }

            // Handle mode switching
            if (Input.GetButtonDown(singleFireButton))
            {
                weapon.AutomaticFire = false;
                if (debugMode)
                {
                    Debug.Log("[RangedWeaponExample] Switched to single fire mode");
                }
            }

            if (Input.GetButtonDown(autoFireButton))
            {
                weapon.AutomaticFire = true;
                if (debugMode)
                {
                    Debug.Log("[RangedWeaponExample] Switched to automatic fire mode");
                }
            }
        }

        private void OnDestroy()
        {
            // Unsubscribe from weapon events
            if (weapon != null)
            {
                weapon.OnFired -= HandleWeaponFired;
                weapon.OnProjectileHit -= HandleProjectileHit;
                weapon.OnFireStarted -= HandleFireStarted;
                weapon.OnFireStopped -= HandleFireStopped;
            }
        }

        #endregion

        #region Event Handlers

        private void HandleWeaponFired()
        {
            if (debugMode)
            {
                Debug.Log("[RangedWeaponExample] Weapon fired!");
            }
        }

        private void HandleProjectileHit(Collider hitCollider)
        {
            if (debugMode)
            {
                Debug.Log($"[RangedWeaponExample] Projectile hit: {hitCollider.name}");
            }
        }

        private void HandleFireStarted()
        {
            if (debugMode)
            {
                Debug.Log("[RangedWeaponExample] Automatic fire started");
            }
        }

        private void HandleFireStopped()
        {
            if (debugMode)
            {
                Debug.Log("[RangedWeaponExample] Automatic fire stopped");
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Sets the weapon to control.
        /// </summary>
        /// <param name="newWeapon">The new weapon.</param>
        public void SetWeapon(RangedWeapon newWeapon)
        {
            // Unsubscribe from old weapon
            if (weapon != null)
            {
                weapon.OnFired -= HandleWeaponFired;
                weapon.OnProjectileHit -= HandleProjectileHit;
                weapon.OnFireStarted -= HandleFireStarted;
                weapon.OnFireStopped -= HandleFireStopped;
            }

            // Set new weapon
            weapon = newWeapon;

            // Subscribe to new weapon
            if (weapon != null)
            {
                weapon.OnFired += HandleWeaponFired;
                weapon.OnProjectileHit += HandleProjectileHit;
                weapon.OnFireStarted += HandleFireStarted;
                weapon.OnFireStopped += HandleFireStopped;

                if (debugMode)
                {
                    Debug.Log("[RangedWeaponExample] Weapon set");
                }
            }
        }

        /// <summary>
        /// Fires the weapon at a target position.
        /// </summary>
        /// <param name="targetPosition">The target position.</param>
        public void FireAtTarget(Vector3 targetPosition)
        {
            if (weapon != null)
            {
                weapon.FireAt(targetPosition);
            }
        }

        #endregion
    }
}
