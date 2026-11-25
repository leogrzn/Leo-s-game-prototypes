# Hold the Mine – Build Plan

## Scene layout (one scene: `Assets/Scenes/HoldTheMine.unity`)
World is ~130 x 130 m, centred on the gold mine at (0, 0, 6).

```
                 north (open dirt, map edge)
   DESERT (x < -14)        MINE          FOREST (x > 14)
   sand, scattered rocks   ore piles     dense trees, short sight
   long sightlines         miners        enemies hidden until close
                           depot
                            |  road (x = 0, z -9 → -65)
                 south edge: trucks enter/leave here
```

- `World/Ground` – one big collider; coloured tiles for desert, centre and forest.
- `World/Mine` – mine building (has Health; losing it = game over), entrance, gold pile, 3 ore piles.
- `World/Road` – strip from the south edge to the depot in front of the mine; truck waypoints.
- `World/Desert` – rocks (colliders block bullets, carve the NavMesh).
- `World/Forest` – trees (trunks block bullets/movement, canopies block view).
- `NavMeshSurface` on `World`, baked from physics colliders.
- Spawn points: desert edge (west), forest edge (east), road ambush points (south-west / south-east).
- `Managers` – GameManager, WaveManager, TruckDispatcher.
- `Player` – blue blocky soldier, `IsoCamera` follows him.
- `Canvas` – HUD, build bar, message feed, shop, main menu, pause, victory, game over.

## Scripts (`Assets/Scripts`)
| Script | Role |
|---|---|
| `GameConfig` | ScriptableObject with **every balance value** (`Assets/Config/GameConfig.asset`) |
| `GameManager` | Game state, money/gold/food/morale, food consumption, win/lose, restart |
| `WaveManager` | 2-minute countdown, wave composition, spawning, wave-cleared bonus |
| `TruckDispatcher`, `Truck` | Gold and food trucks on the road, loading, payout, destruction |
| `Health` | Shared damage/death for every unit and structure, team + kind registry |
| `Combat`, `Tracer`, `Grenade`, `Explosion` | Hitscan bullets that rocks/trees block, grenades with area damage, FX |
| `PlayerController` | WASD, mouse aim, shoot, reload, build (1-3), rally (Q), order (F), shop keys |
| `SoldierAI` | Friendly soldiers and towers: acquire, shoot, move on orders |
| `MinerAI` | Mine ↔ ore pile loop, dig speed scaled by morale, flee when attacked |
| `EnemyAI` | Rifleman, Raider, Ambusher, Grenadier, Heavy behaviours; stop at and attack barricades; forest concealment |
| `BlockyAnimator` | Swings cube arms/legs while moving |
| `IsoCamera` | Isometric follow camera |
| `UIManager` | HUD, bars, messages, menus, shop and build bar |
| `Editor/HoldTheMineBuilder` | Menu **Hold the Mine → Build Game**: creates materials, prefabs, config, scene, NavMesh, UI |

## Build order
1. Packages (AI Navigation, Pipeline for live Editor control), PLAN.md.
2. Core: GameConfig, Health, Combat, GameManager. Builder makes ground + player + camera → move/shoot.
3. Map: desert, forest, mine, road, NavMesh bake.
4. Economy: miners, gold pile, trucks, food, morale.
5. Enemies (5 types) + WaveManager.
6. Building: barricades, soldiers, towers; rally and orders.
7. UI: HUD, build bar, messages, menus, win/lose, restart.
8. Play-mode verification through the Unity CLI, console clean, tuning.

## Update 2 – orders, upgrades, UI
| Script | Role |
|---|---|
| `SelectionManager` | One selected unit/building, selection ring, move-order marker, upgrade/release actions |
| `Upgradeable` | Level I–III, cost/stat lookup from `GameConfig` level tables, upgrade FX + message |
| `LevelVisuals` | Swaps per-level model parts, colours, guns/turrets and collider size |
| `SoldierAI` | Autonomous (approach & engage near post) / Moving / Holding modes |
| `UIManager` | Icon HUD, selection panel, build bar, alerts with off-screen arrows, minimap, level badges |
| `Editor/HoldTheMineBuilder.Levels` | Level models, pixel-art icons (`Assets/UI/Icons`), minimap picture (`Assets/UI/Minimap.png`) |
