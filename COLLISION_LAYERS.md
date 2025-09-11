# Collision Layer System

This document outlines the collision layer configuration for the HoardSurvivor3.0 project.

## Layer Setup

- **Layer 1**: Ground/Environment/Static Bodies
  - Used for: Terrain, walls, static obstacles
  - Collision: Everything collides with this layer for physics/navigation

- **Layer 2**: Players  
  - Used for: Player characters
  - Collision: Only collides with environment (layer 1), phases through enemies and other players

- **Layer 3**: Enemies (Semi-physical)
  - Used for: Enemy characters like CocoChaser
  - Collision: Collides with ground/environment (layer 1) and other enemies (layer 3), but not players (layer 2)
  - Interaction: Uses distance-based detection for damage/attacks with players

- **Layer 4**: Projectiles
  - Used for: Spell projectiles like Fireball
  - Collision: Detects environment (layer 1) and enemies (layer 3), ignores players

## Behavior

- **Players phase through enemies**: Complete non-physical interaction - no blocking, no pushing
- **Enemies collide with each other**: Enemies can't pass through or stack on top of each other
- **Enemies can pathfind**: Enemies still collide with environment for navigation
- **Projectiles hit enemies**: Projectiles detect and collide with enemies on layer 3
- **Damage still works**: Enemy damage uses distance-based detection, not collision
- **Enemies chase individually**: Each enemy targets the nearest player dynamically with spread positioning and separation forces
- **Pooled enemies are isolated**: Enemies in the object pool have all collision disabled and are moved to far-away storage location (10000, -1000, 10000)

## Pool Management

### Enemy Pool Isolation:
When enemies are returned to the pool:
- **All collision layers/masks disabled**: No interaction with any layer
- **Removed from "enemies" group**: Won't be targeted by player spells or AI
- **Moved to storage location**: Position (10000, -1000, 10000) prevents any interference
- **Visibility disabled**: Hidden and processing disabled
- **Safety checks**: Player targeting ignores invisible enemies and those in storage location

## Implementation

### Enemy collision setup (CocoChaser.cs, EnemyPool.cs):
```csharp
SetCollisionLayerValue(1, false);  // Not on ground layer
SetCollisionLayerValue(2, false);  // Not on player layer
SetCollisionLayerValue(3, true);   // On enemy layer
SetCollisionMaskValue(1, true);    // Collide with ground/environment
SetCollisionMaskValue(2, false);   // Don't physically interact with players
SetCollisionMaskValue(3, true);    // DO collide with other enemies to prevent stacking
```

### Player collision setup (Player.cs):
```csharp
SetCollisionLayerValue(1, false);  // Not on ground layer
SetCollisionLayerValue(2, true);   // On player layer
SetCollisionMaskValue(1, true);    // Collide with ground/environment
SetCollisionMaskValue(2, false);   // Don't collide with other players
SetCollisionMaskValue(3, false);   // Don't collide with enemies (phase through)
```

### Projectile collision setup (Fireball.cs):
```csharp
SetCollisionLayerValue(4, true);   // Projectiles on layer 4
SetCollisionMaskValue(1, true);    // Detect environment/boundaries
SetCollisionMaskValue(3, true);    // Detect enemies
SetCollisionMaskValue(2, false);   // Don't detect players
```
