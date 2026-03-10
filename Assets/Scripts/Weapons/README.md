# Ranged Weapon System

A flexible and generic ranged weapon system for Unity that supports both physics-based projectiles and instant-hit bullets.

## Overview

The weapon system consists of the following components:

- **[`IProjectile`](IProjectile.cs)** - Generic interface for all projectiles
- **[`BaseProjectile`](BaseProjectile.cs)** - Abstract base class with common projectile functionality
- **[`RigidbodyProjectile`](RigidbodyProjectile.cs)** - Physics-based projectile using Unity's Rigidbody
- **[`BulletProjectile`](BulletProjectile.cs)** - Raycast-based projectile for instant hits
- **[`RangedWeapon`](RangedWeapon.cs)** - The main weapon component that fires projectiles

## Features

### Generic Projectile System
- Support for multiple projectile types through a common interface
- Easy to extend with custom projectile behaviors
- Automatic damage dealing to objects with [`Health`](../Health/Health.cs) components

### Rigidbody Projectile
- Physics-based movement affected by gravity
- Collision detection with Unity's physics system
- Configurable mass, drag, and collision detection mode
- Optional rotation to face velocity direction
- Maximum lifetime to prevent projectiles from existing forever

### Bullet Projectile
- Instant hit detection using raycasting
- Visual trail effects using TrailRenderer
- Hit visualization using LineRenderer
- Configurable range and visual speed
- No physics overhead for instant-hit weapons

### Ranged Weapon
- Configurable fire rate and damage
- Support for automatic fire
- Spread angle for shotgun-style weapons
- Multiple projectiles per shot
- Object pooling for performance optimization
- Optional muzzle flash effects
- Events for firing, hitting, and state changes

## Quick Start

### 1. Create a Projectile Prefab

#### For Rigidbody Projectile:
1. Create a new GameObject
2. Add a [`RigidbodyProjectile`](RigidbodyProjectile.cs) component
3. Add a Collider (e.g., SphereCollider)
4. Add visual components (MeshRenderer, etc.)
5. Save as a prefab in your Projectiles folder

#### For Bullet Projectile:
1. Create a new GameObject
2. Add a [`BulletProjectile`](BulletProjectile.cs) component
3. Optionally add a TrailRenderer or LineRenderer for visual effects
4. Save as a prefab in your Projectiles folder

### 2. Set Up the Weapon

1. Add a [`RangedWeapon`](RangedWeapon.cs) component to your GameObject
2. Assign your projectile prefab to the "Projectile Prefab" field
3. Configure the weapon settings:
   - **Damage**: Amount of damage per shot
   - **Projectile Speed**: Speed of the projectile
   - **Fire Rate**: Time between shots (seconds)
   - **Max Range**: Maximum range of the weapon
   - **Automatic Fire**: Enable for continuous firing
   - **Spread Angle**: Angle of spread for projectiles
   - **Projectiles Per Shot**: Number of projectiles fired per trigger

### 3. Firing the Weapon

```csharp
using FindersCheesers;

// Get the weapon component
RangedWeapon weapon = GetComponent<RangedWeapon>();

// Fire once
weapon.TryFire();

// Start automatic firing
weapon.StartFiring();

// Stop automatic firing
weapon.StopFiring();

// Fire at a specific target
Vector3 targetPosition = enemy.transform.position;
weapon.FireAt(targetPosition);
```

## API Reference

### IProjectile Interface

```csharp
public interface IProjectile
{
    bool IsActive { get; }
    float Damage { get; }
    GameObject Owner { get; }
    event System.Action<Collider> OnHit;
    event System.Action OnDestroyed;
    void Fire(Vector3 origin, Vector3 direction, float speed, float damage, GameObject owner);
    void Deactivate();
}
```

### RangedWeapon Component

#### Properties
- `bool IsFiring` - Whether the weapon is currently firing
- `bool CanFire` - Whether the weapon can fire (not on cooldown)
- `bool IsOnCooldown` - Whether the weapon is on cooldown
- `float RemainingCooldown` - Remaining cooldown time
- `float Damage` - Damage dealt by projectiles
- `float FireRate` - Time between shots
- `float ProjectileSpeed` - Speed of projectiles

#### Methods
- `bool TryFire()` - Attempts to fire the weapon once
- `void Fire()` - Fires the weapon immediately
- `void StartFiring()` - Starts continuous automatic firing
- `void StopFiring()` - Stops continuous firing
- `void ResetCooldown()` - Resets the fire cooldown
- `void SetProjectilePrefab(GameObject prefab)` - Sets the projectile prefab
- `bool FireAt(Vector3 targetPosition)` - Fires at a specific target position

#### Events
- `OnFired` - Fired when the weapon fires
- `OnProjectileHit` - Fired when a projectile hits something
- `OnFireStarted` - Fired when continuous firing starts
- `OnFireStopped` - Fired when continuous firing stops

## Configuration Examples

### Pistol (Single Shot, Low Spread)
```
Damage: 25
Projectile Speed: 50
Fire Rate: 0.3
Automatic Fire: false
Spread Angle: 2
Projectiles Per Shot: 1
```

### Shotgun (Multi-Shot, High Spread)
```
Damage: 10
Projectile Speed: 40
Fire Rate: 0.8
Automatic Fire: false
Spread Angle: 15
Projectiles Per Shot: 8
```

### Machine Gun (Automatic, Moderate Spread)
```
Damage: 15
Projectile Speed: 60
Fire Rate: 0.1
Automatic Fire: true
Spread Angle: 5
Projectiles Per Shot: 1
```

### Sniper Rifle (Single Shot, No Spread, High Damage)
```
Damage: 100
Projectile Speed: 200
Fire Rate: 1.5
Automatic Fire: false
Spread Angle: 0
Projectiles Per Shot: 1
```

## Integration with Existing Systems

### Health System
Projectiles automatically deal damage to objects with a [`Health`](../Health/Health.cs) component. The damage amount is set by the weapon when firing.

### Enemy AI
You can integrate the ranged weapon with the [`AttackingAI`](../EnemyAI/AttackingAI.cs) system by subscribing to the `OnTargetInAttackRange` event:

```csharp
private void Awake()
{
    rangedWeapon = GetComponent<RangedWeapon>();
    enemyAI = GetComponent<EnemyAI>();
    
    enemyAI.OnTargetInAttackRange += (target) => 
    {
        rangedWeapon.FireAt(target.position);
    };
}
```

### Player Input
Connect the weapon to player input:

```csharp
private void Update()
{
    if (Input.GetButtonDown("Fire1"))
    {
        rangedWeapon.TryFire();
    }
    
    if (Input.GetButton("Fire1") && rangedWeapon.AutomaticFire)
    {
        rangedWeapon.StartFiring();
    }
    else
    {
        rangedWeapon.StopFiring();
    }
}
```

## Performance Optimization

### Object Pooling
The weapon system supports object pooling to reduce instantiation overhead:

1. Enable "Use Object Pooling" in the weapon component
2. Set the "Initial Pool Size" based on your expected maximum active projectiles
3. Projectiles are reused instead of being destroyed and recreated

### Collision Layers
Configure the "Hit Layers" field in projectile components to limit collision checks to relevant layers only, improving performance.

## Custom Projectile Types

To create a custom projectile type:

1. Create a new class that inherits from [`BaseProjectile`](BaseProjectile.cs)
2. Implement the required [`IProjectile`](IProjectile.cs) interface methods
3. Override `OnFired()` for custom initialization
4. Override `OnHitObject()` for custom hit behavior
5. Add any additional functionality you need

Example:

```csharp
using UnityEngine;
using FindersCheesers;

public class ExplosiveProjectile : BaseProjectile
{
    [Header("Explosion Settings")]
    [SerializeField] private float explosionRadius = 5f;
    [SerializeField] private float explosionDamage = 50f;

    protected override void OnHitObject(Collider hitCollider)
    {
        // Deal area damage
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (var collider in hitColliders)
        {
            Health health = collider.GetComponent<Health>();
            if (health != null)
            {
                health.TakeDamage(explosionDamage);
            }
        }

        // Create explosion effect
        // ...

        base.OnHitObject(hitCollider);
    }
}
```

## Troubleshooting

### Projectiles not firing
- Check that a projectile prefab is assigned to the weapon
- Verify the prefab has an [`IProjectile`](IProjectile.cs) component
- Ensure the weapon is not on cooldown

### Projectiles not dealing damage
- Verify target objects have a [`Health`](../Health/Health.cs) component
- Check that the "Hit Layers" setting includes the target's layer
- Ensure the owner is not the same as the target (self-damage is prevented)

### Performance issues
- Enable object pooling in the weapon component
- Reduce the number of active projectiles
- Limit collision checks by configuring hit layers
- Consider using bullet projectiles instead of rigidbody projectiles for instant-hit weapons

## File Structure

```
Assets/Scripts/Weapons/
├── IProjectile.cs           # Projectile interface
├── BaseProjectile.cs        # Base projectile class
├── RigidbodyProjectile.cs   # Physics-based projectile
├── BulletProjectile.cs      # Raycast-based projectile
├── RangedWeapon.cs          # Main weapon component
└── README.md               # This file
```

## Requirements

- Unity 2020.3 or later
- No external dependencies
- Compatible with existing FindersCheesers systems (Health, EnemyAI, etc.)

## License

Part of the FindersCheesers project.
