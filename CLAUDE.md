# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**BraveMushroom** is a 2D top-down auto-shooter built in Unity (URP 2D) with roguelike shop progression. All scripts live in `Assets/Scripts/` (flat, no subdirectories). The game design document is at `GameDesignDocument.md` in the repo root.

## Unity Workflow

There are no CLI build or test commands. All building, running, and testing is done through the **Unity Editor**. To test a change:
1. Open the project in Unity
2. Play from `MainMenu` scene for full flow, or `Level_1` scene directly for combat testing
3. Delete the save file at `Application.persistentDataPath/savegame.json` to reset state

## Architecture

### Singleton Pattern

Four core managers use `static instance` singletons:

| Class | DontDestroyOnLoad | Scope |
|---|---|---|
| `AudioManager` | Yes | Cross-scene music/SFX |
| `GameLevelManager` | No | Per-scene level/shop flow |
| `PlayerStats` | No | Per-scene stat aggregation |
| `ObjectPool` | No | Per-scene bullet/coin pooling |

`SaveSystem` is a **static class** (not MonoBehaviour), accessed directly anywhere.

### Stat System (`PlayerStats`)

`PlayerStats` is the single source of truth for all player stats. It holds two dictionaries:
- `baseStatsDict` — filled from Inspector-configured `baseStats` list on the component
- `runtimeModifiers` — loaded from `SaveData.statBonuses` on `Awake`, then mutated by shop purchases

`GetStat(StatType)` returns `base + runtime`. After any modifier change, `WeaponController.UpdateStats()` and `PlayerHealth.Heal(0)` must be called to propagate to combat systems.

**Stat formulas** (implemented in `WeaponController.UpdateStats()`):
```
Attack:          baseDamage + (int)bonus          // additive
FireRate:        baseFireInterval / (1 + bonus)   // divisor — higher bonus = shorter interval
Health:          base + bonus                     // additive (read via PlayerStats.GetStat)
MoveSpeed:       base + bonus                     // additive
ProjectileCount: base + (int)bonus                // additive, integer
SpreadAngle:     Mathf.Max(0, base + bonus)       // additive, clamped ≥ 0
RotationSpeed:   Mathf.Max(2, base + bonus)       // additive, clamped ≥ 2
```

`StatType` enum is defined in `ShopItemData.cs`, not its own file.

### Save System (`SaveSystem` / `SaveData`)

- JSON file at `Application.persistentDataPath/savegame.json` via `JsonUtility`
- **All attribute bonuses are aggregated** — same `StatType` values are summed into a single `StatBonus` entry in `data.statBonuses`
- `SaveData.coins` = settled gold (persisted); `GameLevelManager.tempGold` = current-run gold (lost on quit without clearing a level)
- `isInShop = true` in save data triggers shop session recovery on next load (skips spawner, opens shop directly)
- Death → `SaveSystem.DeleteSave()`; victory → same. Every run is a fresh start.

### Level Progression

All levels use the **same scene** (`Level_1.unity`). `SaveData.currentLevelIndex` (starts at 1) selects which `LevelConfig` ScriptableObject `EnemySpawner` uses. When index ≥ `finalLevelIndex` (default 3), `GameLevelManager` loads Boss music and after clearing redirects to `EndingScene`.

### Weapon System (`WeaponController`)

Weapons are **not swapped as GameObjects** — only data fields are copied. `GameLevelManager.ApplyWeaponData()` copies `bulletPrefab`, `baseDamage`, `baseFireInterval`, `baseProjectileCount`, `spreadAngle`, `rotationSpeed`, `attackRange`, and gun sprite from a prefab's `WeaponController` onto the player's `WeaponController`, then calls `UpdateStats()`.

The active weapon's prefab name is stored as `SaveData.currentWeaponID` and restored on scene load by searching `allItems` in `GameLevelManager`.

### Enemy Tracking

`EnemyHealth.allEnemies` is a **static `List<EnemyHealth>`** maintained automatically via `OnEnable`/`OnDisable`. `WeaponController` iterates this list every frame to find the nearest living target within `attackRange`.

### Object Pool (`ObjectPool`)

Use `ObjectPool.Spawn(prefab, pos, rot)` and `ObjectPool.Despawn(obj)` instead of `Instantiate`/`Destroy` for bullets and coins. The pool keys by `prefab.GetInstanceID()` and auto-attaches a `PoolTag` component on first instantiation. Falls back to `Instantiate`/`Destroy` gracefully if no `ObjectPool` instance exists.

Note: `EnemySpawner` uses plain `Instantiate` for enemies (enemies are not pooled).

### ScriptableObjects

- `LevelConfig` — per-level config (enemy count, spawn interval, prefab list). Menu path: `BraveMushroom/Level Config`
- `ShopItemData` — shop item (price, icon, optional `weaponPrefab`, optional `modifiers` list). Menu path: `Shop/Unified Item`. A single item can contain both a weapon swap and stat modifiers simultaneously.

### Shop Purchase Flow

`GameLevelManager.TryBuyItem()` is the single purchase entry point:
1. Load save → check coins → deduct
2. If `weaponPrefab` present → `ApplyWeaponData()`
3. If `modifiers` present → `PlayerStats.ApplyItem()` (mutates runtime dict + aggregates into `data.statBonuses`) → refresh `PlayerHealth`, `WeaponController`, `PlayerController`
4. Single `SaveSystem.Save(data)` call (Load → modify → Save pattern, one I/O per purchase)

### Audio

`AudioManager` (cross-scene singleton) manages BGM with coroutine-based fade in/out using `Time.unscaledDeltaTime` so fades work during pause (`timeScale = 0`). SFX are played via `PlaySFX(name)` or `PlaySFXWithPitch(name, min, max)` — pitch is reset the following frame via a one-frame coroutine.
