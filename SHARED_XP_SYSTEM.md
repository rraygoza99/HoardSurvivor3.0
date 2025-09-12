# Shared XP System

This document explains the shared XP and leveling system implemented in HoardSurvivor3.0.

## Overview

The shared XP system ensures that all players in a multiplayer session have the same XP and level. When any player collects XP orbs, all players benefit equally from the progression.

## Components

### 1. GameData.gd (GDScript Singleton)
- **Location**: `features/global/GameData.gd`
- **Purpose**: Stores shared XP state and handles progression logic
- **Key Variables**:
  - `shared_current_xp`: Current XP amount shared by all players
  - `shared_xp_to_next_level`: XP required for next level
  - `shared_current_level`: Current level shared by all players

### 2. SharedXPManager.cs (C# Autoload)
- **Location**: `features/global/SharedXPManager.cs`
- **Purpose**: C# wrapper for GameData, handles networking and signals
- **Features**:
  - RPC synchronization across multiplayer
  - Signal relay between GDScript and C#
  - Singleton pattern for easy access

### 3. PlayerController Modifications
- **Location**: `features/player/PlayerController.cs`
- **Changes**:
  - `GainXp()` now uses shared system instead of individual progression
  - Automatic synchronization with shared XP values
  - Signal connections for real-time updates

## How It Works

### XP Collection Flow
1. Player collects XP orb
2. `PlayerController.GainXp()` called with XP amount
3. Only multiplayer authority processes the gain (prevents duplicates)
4. `SharedXPManager.GainSharedXp()` called
5. GameData updates shared values and checks for level up
6. RPC sent to sync with other players
7. All players receive updated XP/level via signals

### Level Up System
- **Automatic**: Level up occurs when `shared_current_xp >= shared_xp_to_next_level`
- **Scaling**: Each level requires 1.5x more XP than the previous level
- **Synchronized**: All players level up simultaneously

## Multiplayer Synchronization

### RPC System
- Only the multiplayer authority (host) processes XP gains
- RPC calls ensure all clients receive the same XP updates
- Prevents race conditions and duplicate XP gains

### Signal System
```csharp
// GameData signals (GDScript)
signal shared_xp_gained(amount, total_xp)
signal shared_level_up(new_level)
signal shared_xp_changed(current_xp, xp_to_next, level)

// SharedXPManager signals (C#)
[Signal] SharedXpGained(int amount, int totalXp)
[Signal] SharedLevelUp(int newLevel)
[Signal] SharedXpChanged(int currentXp, int xpToNext, int level)
```

## Usage Examples

### Gaining XP
```csharp
// In PlayerController or any other script
SharedXPManager.Instance.GainSharedXp(25);
```

### Getting Current Progress
```csharp
var progress = SharedXPManager.Instance.GetSharedXpProgress();
int currentXp = progress["current_xp"].AsInt32();
int level = progress["current_level"].AsInt32();
float percentage = progress["xp_percentage"].AsSingle();
```

### Listening for Changes
```csharp
// Connect to signals
SharedXPManager.Instance.SharedLevelUp += OnLevelUp;
SharedXPManager.Instance.SharedXpChanged += OnXpChanged;

private void OnLevelUp(int newLevel)
{
    GD.Print($"Everyone leveled up to {newLevel}!");
}
```

## Benefits

1. **Cooperative Gameplay**: Players progress together, encouraging teamwork
2. **Simplified Balancing**: No individual power gaps between players
3. **Shared Achievements**: Everyone celebrates level ups together
4. **Anti-Griefing**: No way for one player to "steal" XP from others
5. **Synchronized State**: All players always have identical progression

## Autoload Configuration

The system requires these autoloads in `project.godot`:
```ini
[autoload]
GameData="*res://features/global/GameData.gd"
SharedXPManager="*res://features/global/SharedXPManager.cs"
```

## Future Enhancements

- UI elements that display shared progression
- Shared level up effects and celebrations
- Team-based upgrade choices
- Shared skill trees or perks