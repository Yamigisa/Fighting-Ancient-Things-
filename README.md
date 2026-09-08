# Fighting Ancient Things

## Game Overview

**Fighting Ancient Things** is a 2D grid-based wave defense game on PC. Ancient monsters of myth and legend are marching toward your base — and you're going to meet them with modern firepower.

The core loop alternates between two phases:

- **Build Phase** — Spend Gold to buy and place combat units on a grid. Units can attack, shield, or passively generate more Gold for your economy. You can also reposition any already-placed unit for free before committing to the next wave. When you're ready, hit *Start Wave*.
- **Combat Phase** — Enemies spawn from the grid edges and march toward the core at the center of the map. Your units fight back automatically. Surviving enemies deal damage to the core when they reach it; if core health drops to zero, it's game over.

After each wave is cleared, you enter an **Upgrade Phase** — a card-draw system that offers a random selection of upgrades (Attack, Attack Speed, or Vitality) purchasable with Diamonds. Diamonds are earned by clearing waves and optionally by killing certain enemies.

**Win condition:** Survive all predefined enemy waves.  
**Lose condition:** The core's health reaches zero.

---

## How to Run

### Option A — Play the Build (Recommended)
1. Download `FightingAncientThings.zip` from .
2. Extract the archive anywhere on your PC.
3. Run `FightingAncientThings.exe`.
4. No installation required. Windows only.

### Option B — Open in Unity Editor
1. Clone or download this repository.
2. Open **Unity Hub**, click **Add project from disk**, and select the repository root.
3. Make sure you are using Unity Editor **6000.0.75f1** (the exact version this project was built with).
4. Open the scene at `Assets/Scenes/` and press **Play**.

> **Controls**
> - **Left Click** — Select a unit card to buy / confirm placement / confirm repositioning
> - **Right Click** — Rotate the unit ghost 90° before placing
> - **Escape** — Cancel current placement
> - **Mouse Hover (over a placed unit)** — Preview that unit's attack range on the grid

---

## Technical Decisions

### Event-Driven Phase System over Deep Singleton Coupling

The central design choice was to coordinate all game systems through a **static C# event** rather than having systems call each other directly.

`GameManager` owns a single `static event Action<GamePhase> PhaseChanged` that fires whenever the game moves between `Build`, `Combat`, `Upgrade`, and `GameOver` states. Every system that cares about phase transitions — `EnemyManager`, `ResourceManager`, `GoldProducer`, `UpgradeManager` — subscribes to that event in `OnEnable` and unsubscribes in `OnDisable`.

The practical benefit: no system has to import or query another system to know what the game is doing. A `GoldProducer` sitting on a unit prefab doesn't need to know `GameManager` exists beyond the event; it just listens for `GamePhase.Combat` to start producing gold, and stops otherwise. This kept coupling low without needing a full message-bus or service-locator pattern.

The singletons that *do* exist (`GameManager.Instance`, `ResourceManager.Instance`, etc.) are intentionally few and only used when a system genuinely needs to perform a direct action on another — like spending gold on placement or registering destination damage. They are not used as a communication channel for state; that's the event's job.

### Shared `Health` and `Attack` Components for Units and Enemies

Units and enemies share the same `Health` and `Attack` MonoBehaviours. Rather than encoding stats directly into unit or enemy classes, both `UnitObject` and `EnemyObject` call `health.Initialize(maxHealth)` and `attack.Initialize(...)` at startup with values sourced from their respective ScriptableObjects.

This was a deliberate choice against hardcoding: the attack logic (projectile vs. melee, area size, targets per cycle, attack rate) is identical in structure for both sides — only the data differs. Reusing the same component means a bug fix or improvement in attack logic benefits both units and enemies automatically, and adding a new stat only requires touching one place.

`Attack` also handles target validation internally — it checks whether the owner is a `UnitObject` or `EnemyObject` and filters valid targets accordingly, so neither side needs to be aware of targeting rules.

### ScriptableObject-Driven Data

All unit and enemy definitions live in `UnitSO` and `EnemySO` ScriptableObjects. This decouples data from behaviour completely: tuning a unit's health, attack range, attack speed, gold production rate, or projectile parameters requires no code changes, only Inspector edits. The same prefab is reused for every unit type; the SO tells it what to look and act like.

Upgrades follow the same pattern via `UpgradeNode` ScriptableObjects, which define the effect type (`Health`, `Attack`, `AttackSpeed`), magnitude, diamond cost, and prerequisite nodes — giving the upgrade system a lightweight dependency graph without any hand-written tree logic.

### Grid-Based Placement and Attack Range

The grid is generated procedurally at runtime by `GridManager` from a configurable width and height. Tiles are tracked in a `Dictionary<Vector2, Tile>`, which makes world-to-grid lookups O(1).

Attack ranges are grid-aligned: `Attack` computes a rectangular area in grid-cell space based on the unit's forward direction, width, and depth. This makes ranges predictable, visually clear, and rotation-aware — rotating a unit in the build phase physically changes its attack cone, which the grid highlights in real time.

### What I Chose Not To Do

- **No hardcoded stats in unit/enemy MonoBehaviours.** All numbers live in ScriptableObjects. This kept the runtime classes clean and made balancing straightforward.
- **No NavMesh or pathfinding library.** Enemies use a simple two-waypoint path: move toward a corner, then straight to the destination. This was sufficient for the grid layout and removed a significant dependency.
- **No animation system.** Visuals are static sprites. Time was better spent on solid system architecture than on surface polish.
- **No audio.** Noted in Known Issues.

---

## What I Would Do With More Time

- **Music and sound effects.** The game is currently silent, which noticeably hurts the feel of combat. I would add at minimum a looping background track and attack, death, and wave-clear sounds.
- **Skill tree instead of card draw for upgrades.** The current system presents a random card selection each wave. I would replace it with a persistent visual skill tree — one where the player can see all possible upgrades, plan ahead, and feel progression build across the full run rather than being surprised each wave.
- **Enemy escalation tuning.** The wave system supports arbitrary enemy compositions per wave, but I would spend more time designing the difficulty curve so each wave demands meaningfully different defensive setups.
- **Unit sell mechanic.** Currently, a placed unit can be repositioned for free but not removed. A sell action returning partial gold would open up more interesting mid-run decisions.
- **Run summary screen.** A post-game screen showing waves survived, units lost, gold spent, and enemies killed would make the end of a run feel like a proper conclusion rather than a sudden panel.

---

## Known Issues

- **No audio.** The game has no music or sound effects.
- **`FindObjectsByType` in `Attack.cs`.** The ranged target-finding loop iterates over all `Health` instances in the scene every attack cycle. This is fine at prototype scale but would need to be replaced with a spatial query (e.g., `Physics2D.OverlapCircle` or a registered-entity list) for larger enemy counts.
- **Enemy collision-ignore is O(n²).** When a new enemy spawns, it calls `Physics2D.IgnoreCollision` against every other active enemy. Acceptable for the current wave sizes; would need a layer-based solution at scale.
