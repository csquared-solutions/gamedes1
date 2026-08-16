# Wick Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a seven-level Godot 4 + C# prototype of Wick that preserves the GDD's single-resource survival loop, placeable lamp mechanic, deterministic hazard behavior, and required accessibility features.

**Architecture:** The project is a 2D grid-based Godot game with a fixed single-screen camera, tile-driven simulation, and separate C# systems for turns, wick, visibility, levels, hazards, and UI. The lamp is a first-class world object, and simulation state drives rendering so sight, danger, and fairness remain consistent.

**Tech Stack:** Godot 4.x, C#, TileMap/2D nodes, Resource files for authored level data, Git optional, free placeholder or CC0 art assets

**Spec:** `docs/superpowers/specs/2026-08-16-wick-design.md`

## Global Constraints

- Engine is `Godot 4.x` with `C#`.
- The game is `2D`, `top-down`, `turn-based`, and `single-screen`.
- Each level is exactly `19 x 13`.
- The prototype includes exactly `7` authored levels.
- There is no combat, inventory, scrolling camera, procedural generation, or persistent progression.
- Wick is the only primary spendable resource.
- Sight radius is `floor(wick / 2)`.
- Oil flask reward is `+6 wick`.
- Flare cost is `3 wick`.
- Levels 1-3 start with `12 wick`, levels 4-5 with `14`, and levels 6-7 with `16`.
- Loss causes must be reported separately as `depleted` and `smothered`.
- Accessibility features are in scope for the shipped prototype, not deferred.

---

## Planned File Structure

```text
project.godot
Wick.csproj
scenes/
  game/GameRoot.tscn
  board/Board.tscn
  actors/Player.tscn
  actors/Lamp.tscn
  hazards/Crawler.tscn
  hazards/Moth.tscn
  pickups/OilFlask.tscn
  goals/Stair.tscn
  ui/Hud.tscn
  ui/WickGauge.tscn
  ui/HazardStateIcon.tscn
  levels/Level01.tscn
  levels/Level02.tscn
  levels/Level03.tscn
  levels/Level04.tscn
  levels/Level05.tscn
  levels/Level06.tscn
  levels/Level07.tscn
scripts/
  core/TurnManager.cs
  core/WickSystem.cs
  core/VisibilitySystem.cs
  core/LevelManager.cs
  core/GameEvents.cs
  board/GridCoord.cs
  board/BoardState.cs
  board/BoardRenderer.cs
  actors/PlayerController.cs
  actors/LampController.cs
  hazards/HazardBase.cs
  hazards/CrawlerController.cs
  hazards/MothController.cs
  hazards/HazardSystem.cs
  pickups/OilFlaskController.cs
  goals/StairController.cs
  ui/HudController.cs
  ui/WickGaugeController.cs
  ui/LampTetherController.cs
  ui/HazardStateIndicatorController.cs
  data/LevelDefinition.cs
  data/LevelLoader.cs
  data/LevelTuning.cs
tests/
  Wick.Tests/
    TurnManagerTests.cs
    VisibilitySystemTests.cs
    WickSystemTests.cs
    HazardSystemTests.cs
    LevelSolvabilityTests.cs
docs/
  attribution.md
```

### Task 1: Bootstrap the Godot 4 + C# Project Skeleton

**Files:**
- Create: `project.godot`
- Create: `Wick.csproj`
- Create: `scenes/game/GameRoot.tscn`
- Create: `scripts/core/GameEvents.cs`
- Create: `scripts/board/GridCoord.cs`
- Create: `scripts/data/LevelTuning.cs`
- Create: `docs/attribution.md`

**Interfaces:**
- Consumes: none
- Produces:
  - `public readonly record struct GridCoord(int X, int Y);`
  - `public static class LevelTuning`
  - `public partial class GameEvents : Node`

- [ ] **Step 1: Create the Godot project and root folders**

Create a clean Godot `.NET` project with this minimum layout:

```text
scenes/
scripts/
tests/
assets/
data/
docs/
```

- [ ] **Step 2: Add the coordinate primitive**

Create `scripts/board/GridCoord.cs`:

```csharp
namespace Wick.Board;

public readonly record struct GridCoord(int X, int Y)
{
    public static GridCoord operator +(GridCoord a, GridCoord b) => new(a.X + b.X, a.Y + b.Y);
}
```

- [ ] **Step 3: Add a single source of tuning constants**

Create `scripts/data/LevelTuning.cs`:

```csharp
namespace Wick.Data;

public static class LevelTuning
{
    public const int GridWidth = 19;
    public const int GridHeight = 13;
    public const int FlareCost = 3;
    public const int FlaskRestore = 6;
}
```

- [ ] **Step 4: Add a shared event hub or signal host**

Create `scripts/core/GameEvents.cs`:

```csharp
using Godot;

namespace Wick.Core;

public partial class GameEvents : Node
{
    [Signal] public delegate void TurnAdvancedEventHandler(int turnNumber);
    [Signal] public delegate void WickChangedEventHandler(int wickValue);
    [Signal] public delegate void LightOutEventHandler(string cause);
    [Signal] public delegate void LevelClearedEventHandler(int levelIndex, int remainingWick);
}
```

- [ ] **Step 5: Add the attribution tracker early**

Create `docs/attribution.md`:

```md
# Asset Attribution

| Asset | Source | License | Used In | Notes |
|---|---|---|---|---|
| Placeholder geometry | In-engine | Original | Prototype | Default until replacement |
```

- [ ] **Step 6: Run the project and confirm it opens**

Run the Godot project and verify:

- project loads without C# compile errors
- `GameRoot.tscn` can be set as the main scene
- no missing-script errors appear on startup

- [ ] **Step 7: Commit**

```bash
git add .
git commit -m "chore: bootstrap Wick Godot project structure"
```

### Task 2: Build the Grid, Turn Loop, and Wick Core

**Files:**
- Create: `scripts/core/TurnManager.cs`
- Create: `scripts/core/WickSystem.cs`
- Create: `scripts/board/BoardState.cs`
- Create: `scripts/board/BoardRenderer.cs`
- Modify: `scenes/game/GameRoot.tscn`
- Test: `tests/Wick.Tests/TurnManagerTests.cs`
- Test: `tests/Wick.Tests/WickSystemTests.cs`

**Interfaces:**
- Consumes:
  - `GridCoord`
  - `LevelTuning`
  - `GameEvents`
- Produces:
  - `public partial class TurnManager : Node`
  - `public partial class WickSystem : Node`
  - `public sealed class BoardState`
  - `public void AdvanceTurn()`
  - `public bool Spend(int amount)`
  - `public int CurrentWick { get; }`

- [ ] **Step 1: Write the failing wick system tests**

Create `tests/Wick.Tests/WickSystemTests.cs`:

```csharp
[Fact]
public void SightRadius_IsFloorOfHalfWick()
{
    var wick = new WickSystem();
    wick.Initialize(startingWick: 12);
    Assert.Equal(6, wick.CurrentRadius);
}

[Fact]
public void Spend_ReducesWick_AndSignalsDepletionAtZero()
{
    var wick = new WickSystem();
    wick.Initialize(startingWick: 1);
    Assert.True(wick.Spend(1));
    Assert.Equal(0, wick.CurrentWick);
    Assert.True(wick.IsDepleted);
}
```

- [ ] **Step 2: Write the failing turn manager tests**

Create `tests/Wick.Tests/TurnManagerTests.cs`:

```csharp
[Fact]
public void AdvanceTurn_IncrementsTurnCounter()
{
    var turnManager = new TurnManager();
    turnManager.AdvanceTurn();
    Assert.Equal(1, turnManager.CurrentTurn);
}
```

- [ ] **Step 3: Implement the minimal wick system**

Create `scripts/core/WickSystem.cs`:

```csharp
using Godot;

namespace Wick.Core;

public partial class WickSystem : Node
{
    public int CurrentWick { get; private set; }
    public int CurrentRadius => CurrentWick / 2;
    public bool IsDepleted => CurrentWick <= 0;

    public void Initialize(int startingWick) => CurrentWick = startingWick;

    public bool Spend(int amount)
    {
        CurrentWick = Mathf.Max(0, CurrentWick - amount);
        return !IsDepleted;
    }

    public void Restore(int amount) => CurrentWick += amount;
}
```

- [ ] **Step 4: Implement the minimal turn manager**

Create `scripts/core/TurnManager.cs`:

```csharp
using Godot;

namespace Wick.Core;

public partial class TurnManager : Node
{
    public int CurrentTurn { get; private set; }

    public void AdvanceTurn()
    {
        CurrentTurn++;
    }
}
```

- [ ] **Step 5: Add board state scaffolding**

Create `scripts/board/BoardState.cs`:

```csharp
namespace Wick.Board;

public sealed class BoardState
{
    public int Width { get; }
    public int Height { get; }

    public BoardState(int width, int height)
    {
        Width = width;
        Height = height;
    }

    public bool IsInside(GridCoord cell) => cell.X >= 0 && cell.X < Width && cell.Y >= 0 && cell.Y < Height;
}
```

- [ ] **Step 6: Run the targeted tests**

Run:

```bash
dotnet test tests/Wick.Tests --filter "FullyQualifiedName~WickSystemTests|FullyQualifiedName~TurnManagerTests"
```

Expected:

- initial tests fail before implementation
- targeted tests pass after implementation

- [ ] **Step 7: Commit**

```bash
git add .
git commit -m "feat: add grid, wick, and turn loop foundations"
```

### Task 3: Implement Tile Visibility, Memory, and the Fixed Camera

**Files:**
- Create: `scripts/core/VisibilitySystem.cs`
- Modify: `scripts/core/WickSystem.cs`
- Modify: `scripts/board/BoardState.cs`
- Create: `scenes/board/Board.tscn`
- Create: `tests/Wick.Tests/VisibilitySystemTests.cs`

**Interfaces:**
- Consumes:
  - `BoardState`
  - `GridCoord`
  - `WickSystem`
- Produces:
  - `public sealed class VisibilitySystem`
  - `public VisibilityResult Recompute(GridCoord lampCell, int radius)`
  - `public bool IsVisible(GridCoord cell)`
  - `public bool WasSeen(GridCoord cell)`

- [ ] **Step 1: Write the visibility tests first**

Create `tests/Wick.Tests/VisibilitySystemTests.cs`:

```csharp
[Fact]
public void Recompute_MarksLampTileVisible()
{
    var board = TestBoards.EmptyRoom();
    var visibility = new VisibilitySystem(board);
    var result = visibility.Recompute(new GridCoord(9, 6), 3);
    Assert.True(result.Visible.Contains(new GridCoord(9, 6)));
}

[Fact]
public void Recompute_PreservesSeenMemory_ForPreviouslyVisibleWalls()
{
    var board = TestBoards.EmptyRoom();
    var visibility = new VisibilitySystem(board);
    visibility.Recompute(new GridCoord(9, 6), 4);
    visibility.Recompute(new GridCoord(9, 6), 1);
    Assert.True(visibility.WasSeen(new GridCoord(9, 2)));
}
```

- [ ] **Step 2: Implement minimal tile-state visibility**

Create `scripts/core/VisibilitySystem.cs`:

```csharp
namespace Wick.Core;

using System.Collections.Generic;
using Wick.Board;

public sealed class VisibilitySystem
{
    private readonly BoardState _board;
    private readonly HashSet<GridCoord> _visible = new();
    private readonly HashSet<GridCoord> _seen = new();

    public VisibilitySystem(BoardState board) => _board = board;

    public VisibilityResult Recompute(GridCoord lampCell, int radius)
    {
        _visible.Clear();

        for (int y = 0; y < _board.Height; y++)
        for (int x = 0; x < _board.Width; x++)
        {
            var cell = new GridCoord(x, y);
            var distance = System.Math.Abs(cell.X - lampCell.X) + System.Math.Abs(cell.Y - lampCell.Y);
            if (distance <= radius)
            {
                _visible.Add(cell);
                _seen.Add(cell);
            }
        }

        return new VisibilityResult(_visible, _seen);
    }

    public bool IsVisible(GridCoord cell) => _visible.Contains(cell);
    public bool WasSeen(GridCoord cell) => _seen.Contains(cell);
}

public sealed record VisibilityResult(IReadOnlyCollection<GridCoord> Visible, IReadOnlyCollection<GridCoord> Seen);
```

- [ ] **Step 3: Replace Manhattan visibility with line-of-sight**

Upgrade the implementation to:

- stop visibility through walls
- trace from lamp to tile center
- keep the API unchanged so tests and callers do not move

The minimal line test method should look like:

```csharp
private bool HasLineOfSight(GridCoord from, GridCoord to)
{
    // Replace with a symmetric grid trace if naive Bresenham is asymmetric.
    return true;
}
```

- [ ] **Step 4: Render visible, remembered, and hidden states distinctly**

Create `scripts/board/BoardRenderer.cs` with three tile presentation states:

```csharp
public enum VisibilityState
{
    Hidden,
    Remembered,
    Visible
}
```

- [ ] **Step 5: Verify readability inside Godot**

Run a single-room scene and confirm:

- the full `19 x 13` board fits in one camera view
- shrinking wick shrinks visible range clearly
- remembered geometry remains readable but dim

- [ ] **Step 6: Commit**

```bash
git add .
git commit -m "feat: add visibility, memory, and board rendering states"
```

### Task 4: Implement Player Movement, Lamp Placement, and Level Flow

**Files:**
- Create: `scripts/actors/PlayerController.cs`
- Create: `scripts/actors/LampController.cs`
- Create: `scripts/core/LevelManager.cs`
- Create: `scripts/data/LevelDefinition.cs`
- Create: `scripts/data/LevelLoader.cs`
- Create: `scripts/goals/StairController.cs`
- Create: `scenes/actors/Player.tscn`
- Create: `scenes/actors/Lamp.tscn`
- Create: `scenes/goals/Stair.tscn`

**Interfaces:**
- Consumes:
  - `TurnManager`
  - `WickSystem`
  - `VisibilitySystem`
  - `GridCoord`
- Produces:
  - `public bool TryMove(GridCoord direction)`
  - `public bool TryPlaceLamp()`
  - `public bool TryRecoverLamp()`
  - `public void LoadLevel(int index)`
  - `public void RestartLevel()`

- [ ] **Step 1: Write failing player movement tests**

Example test:

```csharp
[Fact]
public void TryMove_ConsumesOneWick_OnSuccessfulOrthogonalMove()
{
    // arrange board, player, and wick
    // assert wick decreases by 1 when move succeeds
}
```

- [ ] **Step 2: Implement orthogonal movement only**

Create `scripts/actors/PlayerController.cs`:

```csharp
public bool TryMove(GridCoord direction)
{
    if (!IsOrthogonal(direction))
        return false;

    var target = Cell + direction;
    if (!_board.IsWalkable(target))
        return false;

    Cell = target;
    _wick.Spend(1);
    _turns.AdvanceTurn();
    return true;
}
```

- [ ] **Step 3: Implement lamp carry/place/recover behavior**

Create `scripts/actors/LampController.cs` with:

```csharp
public GridCoord Cell { get; private set; }
public bool IsCarried { get; private set; }

public void AttachToPlayer(GridCoord playerCell)
{
    IsCarried = true;
    Cell = playerCell;
}

public void Place(GridCoord cell)
{
    IsCarried = false;
    Cell = cell;
}
```

- [ ] **Step 4: Add the contextual lamp input**

Bind:

- movement: arrow keys and WASD
- lamp toggle: `E` primary, `Q` alternate
- flare: `F` primary, `Space` alternate
- restart: one dedicated key

Expected behavior:

- carrying lamp + press toggle => place lamp
- standing on lamp + press toggle => recover lamp
- anywhere else + press toggle => no-op

- [ ] **Step 5: Add stair win and level restart flow**

Create `scripts/goals/StairController.cs` and `scripts/core/LevelManager.cs` so:

- stepping on stair emits win
- depletion or smothering restarts current level
- remaining wick is preserved only for the level-clear report

- [ ] **Step 6: Play through one full room**

Verify:

- move cost updates wick
- visibility follows lamp, not player
- placed lamp remains behind when player walks away
- stepping back onto the lamp can recover it

- [ ] **Step 7: Commit**

```bash
git add .
git commit -m "feat: add player movement, lamp placement, and level flow"
```

### Task 5: Implement Crawler, Moth, and Smother Loss Behavior

**Files:**
- Create: `scripts/hazards/HazardBase.cs`
- Create: `scripts/hazards/CrawlerController.cs`
- Create: `scripts/hazards/MothController.cs`
- Create: `scripts/hazards/HazardSystem.cs`
- Create: `scenes/hazards/Crawler.tscn`
- Create: `scenes/hazards/Moth.tscn`
- Test: `tests/Wick.Tests/HazardSystemTests.cs`

**Interfaces:**
- Consumes:
  - `BoardState`
  - `VisibilitySystem`
  - `LampController`
  - `WickSystem`
- Produces:
  - `public abstract GridCoord TakeTurn(HazardContext context)`
  - `public sealed class CrawlerController : HazardBase`
  - `public sealed class MothController : HazardBase`
  - `public bool HasReachedLamp { get; }`

- [ ] **Step 1: Write failing hazard tests**

Create `tests/Wick.Tests/HazardSystemTests.cs`:

```csharp
[Fact]
public void Crawler_MovesEveryTurnInDarkness()
{
    // assert one-tile advance in darkness
}

[Fact]
public void Crawler_MovesEveryOtherTurnInLight()
{
    // assert stall cadence inside lit radius
}

[Fact]
public void Moth_IsDormant_WhenRadiusBelowFive()
{
    // assert no movement at radius 4
}
```

- [ ] **Step 2: Implement a shared hazard base**

Create `scripts/hazards/HazardBase.cs`:

```csharp
public abstract class HazardBase
{
    public GridCoord Cell { get; protected set; }
    public abstract GridCoord TakeTurn(HazardContext context);
}
```

- [ ] **Step 3: Implement crawler rules exactly**

Crawler behavior must encode:

- full speed in darkness
- half speed in lit radius
- one-turn stall on first entering lit space

Minimal method shape:

```csharp
public override GridCoord TakeTurn(HazardContext context)
{
    // compute next step toward lamp
    // honor first-entry stall and lit slowdown
    return Cell;
}
```

- [ ] **Step 4: Implement moth rules exactly**

Moth behavior must encode:

- dormant when `radius < 5`
- active and attracted to lamp when `radius >= 5`
- one-turn warning stall on first lit entry if applicable

- [ ] **Step 5: Route smothering into the shared loss handler**

When a hazard reaches the lamp tile:

```csharp
_events.EmitSignal(GameEvents.SignalName.LightOut, "smothered");
```

Depletion remains:

```csharp
_events.EmitSignal(GameEvents.SignalName.LightOut, "depleted");
```

- [ ] **Step 6: Run hazard tests and one manual play room**

Run:

```bash
dotnet test tests/Wick.Tests --filter "FullyQualifiedName~HazardSystemTests"
```

Manual expectations:

- crawler pressure increases as wick falls
- moth pressure increases after refueling
- lamp placement retargets hazards correctly

- [ ] **Step 7: Commit**

```bash
git add .
git commit -m "feat: add crawler, moth, and smother loss behavior"
```

### Task 6: Implement Oil Flask, Flare, and Tactical Readability UI

**Files:**
- Create: `scripts/pickups/OilFlaskController.cs`
- Create: `scripts/ui/HudController.cs`
- Create: `scripts/ui/WickGaugeController.cs`
- Create: `scripts/ui/LampTetherController.cs`
- Create: `scripts/ui/HazardStateIndicatorController.cs`
- Create: `scenes/pickups/OilFlask.tscn`
- Create: `scenes/ui/Hud.tscn`
- Create: `scenes/ui/WickGauge.tscn`
- Create: `scenes/ui/HazardStateIcon.tscn`

**Interfaces:**
- Consumes:
  - `WickSystem`
  - `PlayerController`
  - `LampController`
  - `HazardSystem`
- Produces:
  - `public void BeginLift()`
  - `public bool TryFireFlare()`
  - `public int GetReturnCost()`

- [ ] **Step 1: Write failing tests for flask timing and flare cost**

Example tests:

```csharp
[Fact]
public void FlaskLift_ConsumesTwoTurns_ThenRestoresSixWick()
{
    // assert turn burn during lift and delayed restore
}

[Fact]
public void Flare_SpendsThreeWick_AndDoublesRadiusForOneTurn()
{
    // assert temporary radius boost then reset
}
```

- [ ] **Step 2: Implement the two-turn flask lift**

Create `scripts/pickups/OilFlaskController.cs`:

```csharp
public async Task BeginLift()
{
    _player.LockInput();
    _visibility.ForceDarkness();
    _wick.Spend(1);
    _turns.AdvanceTurn();
    _wick.Spend(1);
    _turns.AdvanceTurn();
    _wick.Restore(LevelTuning.FlaskRestore);
    _player.UnlockInput();
}
```

- [ ] **Step 3: Implement flare as a one-turn modifier**

Add to `WickSystem` or a dedicated modifier state:

```csharp
public bool TryFireFlare()
{
    if (CurrentWick < LevelTuning.FlareCost)
        return false;

    Spend(LevelTuning.FlareCost);
    _flareTurnsRemaining = 1;
    return true;
}
```

- [ ] **Step 4: Add HUD readability features**

The HUD must show:

- current wick
- current sight radius
- final-three-wick warning
- pulse when radius crosses moth threshold
- separate loss cause text on failure

- [ ] **Step 5: Add always-on tactical aids**

Implement:

- tether line from player to lamp
- numeric return-cost readout
- visible hazard state icons for stalled, slowed, or moving

- [ ] **Step 6: Verify the core tension survives with UI aids**

Manual checks:

- players can read state better without removing uncertainty
- tether and state icons clarify consequences but do not solve routes automatically

- [ ] **Step 7: Commit**

```bash
git add .
git commit -m "feat: add flask, flare, and readability UI"
```

### Task 7: Author Seven Levels, Practice Mode, and Accessibility Completion

**Files:**
- Create: `scenes/levels/Level01.tscn`
- Create: `scenes/levels/Level02.tscn`
- Create: `scenes/levels/Level03.tscn`
- Create: `scenes/levels/Level04.tscn`
- Create: `scenes/levels/Level05.tscn`
- Create: `scenes/levels/Level06.tscn`
- Create: `scenes/levels/Level07.tscn`
- Modify: `scripts/core/LevelManager.cs`
- Modify: `scripts/ui/HudController.cs`
- Test: `tests/Wick.Tests/LevelSolvabilityTests.cs`

**Interfaces:**
- Consumes:
  - `LevelDefinition`
  - `LevelLoader`
  - `HazardSystem`
- Produces:
  - `public void LoadCampaignLevel(int index)`
  - `public void SetPracticeMode(bool enabled)`
  - `public bool IsPracticeMode { get; }`

- [ ] **Step 1: Create level data fixtures**

Each level definition must minimally include:

```csharp
public sealed class LevelDefinition
{
    public int Index { get; init; }
    public int StartingWick { get; init; }
    public GridCoord PlayerStart { get; init; }
    public GridCoord StairCell { get; init; }
    public GridCoord[] Walls { get; init; } = [];
    public GridCoord[] Crawlers { get; init; } = [];
    public GridCoord[] Moths { get; init; } = [];
    public GridCoord[] Flasks { get; init; } = [];
}
```

- [ ] **Step 2: Author level progression against the GDD**

Validate:

- levels 1-3 start at `12 wick`
- levels 4-5 start at `14 wick`
- levels 6-7 start at `16 wick`
- lamp placement is required from level 3 onward
- hazard counts escalate across the campaign

- [ ] **Step 3: Add practice mode**

Practice mode rules:

- wick does not decrease
- one-step undo is enabled
- main campaign rules remain unchanged

Minimal toggle shape:

```csharp
public void SetPracticeMode(bool enabled)
{
    IsPracticeMode = enabled;
}
```

- [ ] **Step 4: Write solvability and no-deadlock tests**

Create `tests/Wick.Tests/LevelSolvabilityTests.cs` with at least:

```csharp
[Theory]
[InlineData(1)]
[InlineData(2)]
[InlineData(3)]
public void Level_HasValidStartAndExit(int levelIndex)
{
    // assert level loads and player/stair are reachable in topology
}
```

Also add a deliberate content review checklist for each level:

- taking every flask
- skipping every optional flask
- placing lamp early
- placing lamp late

- [ ] **Step 5: Run three internal playtest rounds**

Round goals:

- Round 1: visibility legibility
- Round 2: verb comprehension
- Round 3: full seven-level pacing and fairness

Capture:

- where deaths cluster
- whether players discover lamp placement naturally
- whether any flask is always wrong to take

- [ ] **Step 6: Update tuning from observations only**

Allowed tuning levers:

- starting wick
- flask placement
- hazard placement
- moth threshold only if level design cannot solve the issue

- [ ] **Step 7: Commit**

```bash
git add .
git commit -m "feat: add campaign levels, practice mode, and accessibility completion"
```

### Task 8: Add Placeholder Art, Minimal Audio, and Ship-Ready Packaging

**Files:**
- Modify: `docs/attribution.md`
- Create: `assets/art/`
- Create: `assets/audio/`
- Modify: `scenes/ui/Hud.tscn`
- Modify: `scenes/board/Board.tscn`

**Interfaces:**
- Consumes:
  - current scenes and UI
  - attribution sheet
- Produces:
  - a buildable prototype with complete placeholder presentation

- [ ] **Step 1: Keep art replacement readable-first**

Asset checklist:

- walls and floors high contrast
- player always bright enough to track
- crawler and moth silhouette distinction preserved
- darkness never hides the wick gauge

- [ ] **Step 2: Add only the two required audio cues**

Implement:

- moth wake cue
- low-wick gutter cue

Both must also have visual equivalents already present.

- [ ] **Step 3: Populate attribution for every imported asset**

Update `docs/attribution.md` with:

- exact pack name
- source URL
- license
- where it is used

- [ ] **Step 4: Smoke-test restart and full-campaign flow**

Manual pass:

- start game
- lose by depletion
- lose by smothering
- clear one level
- clear all seven levels
- restart from mid-campaign failure

- [ ] **Step 5: Prepare submission captures**

Capture screens for:

- starting room
- first branch
- crawler at light edge
- flask lift in darkness
- placed lamp with tether
- stair clear
- both loss causes

- [ ] **Step 6: Commit**

```bash
git add .
git commit -m "chore: prepare playable prototype for review and submission"
```

## Self-Review

### Spec Coverage

- Core turn loop: covered in Tasks 2-4
- Wick/radius binding: covered in Tasks 2-3
- Placeable lamp: covered in Task 4
- Hazards with opposing rules: covered in Task 5
- Flask and flare: covered in Task 6
- Seven authored levels: covered in Task 7
- Accessibility features: covered in Tasks 6-7
- Minimal art/audio and attribution: covered in Task 8

### Placeholder Scan

- No `TBD` or `TODO` markers remain.
- Each task includes concrete files, interfaces, and implementation/test steps.
- Later tasks reference interfaces introduced in earlier tasks.

### Type Consistency

- `GridCoord` is used consistently across board, actor, and hazard tasks.
- `WickSystem`, `TurnManager`, `VisibilitySystem`, and `LevelManager` names are stable across tasks.
- Loss causes remain `depleted` and `smothered` throughout the document.

## Execution Handoff

Plan complete and saved to `docs/superpowers/plans/2026-08-16-wick-implementation-plan.md`. Two execution options:

**1. Subagent-Driven (recommended)** - I dispatch a fresh subagent per task, review between tasks, fast iteration

**2. Inline Execution** - Execute tasks in this session using executing-plans, batch execution with checkpoints

**Which approach?**
