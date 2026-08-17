# Random Stair Placement Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Give every campaign level three validated stair exits and guarantee that each new attempt selects a different exit from the immediately previous attempt.

**Architecture:** Extend immutable authored level data with a three-cell stair candidate pool. A pure `StairPlacementSelector` chooses an active cell per attempt, remembers the previous cell by level, and returns a cloned active `LevelDefinition`; `LevelManager` consumes that active definition before initializing existing gameplay systems.

**Tech Stack:** Godot 4.7 Mono, C#/.NET 8, xUnit, existing hazard-aware campaign simulation harness, PowerShell packaging tools.

**Spec:** `docs/superpowers/specs/2026-08-17-random-stair-placement-design.md`

## Global Constraints

- Keep exactly seven authored campaign levels.
- Give every campaign level exactly three distinct authored stair candidates.
- Include each level's existing fixed stair as its first candidate.
- A new attempt must never repeat the immediately previous stair for that level.
- `Z` Undo must retain the active stair because it does not reload the level.
- Do not randomize geometry, player, lamp, flasks, crawlers, moths, wick values, or hazard behavior.
- Preserve visibility, lamp-placement, completion-wick, and campaign-progression rules.
- Add no external packages or licensed assets.
- The worktree contains approved audio changes; stage only the exact stair files named by each commit command.

## File Structure

- Create `scripts/goals/StairPlacementSelector.cs`: pure per-attempt selection and previous-cell memory.
- Modify `scripts/data/LevelDefinition.cs`: authored candidate data and immutable active-level cloning.
- Modify `scripts/data/LevelLoader.cs`: exact three-cell candidate pools for all seven levels.
- Modify `scripts/core/LevelManager.cs`: select the active exit before gameplay initialization.
- Create `tests/Wick.Tests/StairPlacementSelectorTests.cs`: selector behavior, repeat prevention, determinism, and immutability.
- Modify `tests/Wick.Tests/LevelSolvabilityTests.cs`: validate all 21 candidate cells.
- Modify `tests/Wick.Tests/CampaignSimulationTests.cs`: solve all 21 exits under both crawler seeds.
- Use the already committed design at `docs/superpowers/specs/2026-08-17-random-stair-placement-design.md` as the GDD amendment.

---

### Task 1: Immutable Candidate Data And Attempt Selector

**Files:**
- Create: `scripts/goals/StairPlacementSelector.cs`
- Modify: `scripts/data/LevelDefinition.cs`
- Create: `tests/Wick.Tests/StairPlacementSelectorTests.cs`

**Interfaces:**
- Consumes: `LevelDefinition`, `GridCoord`, `System.Random`.
- Produces: `GridCoord[] LevelDefinition.StairCandidates`, `LevelDefinition WithStairCell(GridCoord stairCell)`, and `LevelDefinition StairPlacementSelector.SelectForAttempt(LevelDefinition authoredLevel)`.

- [ ] **Step 1: Write failing selector tests**

Create `tests/Wick.Tests/StairPlacementSelectorTests.cs`:

```csharp
using Wick.Board;
using Wick.Data;
using Wick.Goals;
using Xunit;

namespace Wick.Tests;

public sealed class StairPlacementSelectorTests
{
    [Fact]
    public void SelectForAttempt_UsesCandidatesWithoutRepeatingThePreviousAttempt()
    {
        var level = CreateLevel();
        var selector = new StairPlacementSelector(new Random(17));
        GridCoord? previous = null;

        for (var attempt = 0; attempt < 30; attempt++)
        {
            var active = selector.SelectForAttempt(level);

            Assert.Contains(active.StairCell, level.StairCandidates);
            if (previous.HasValue)
            {
                Assert.NotEqual(previous.Value, active.StairCell);
            }

            previous = active.StairCell;
        }
    }

    [Fact]
    public void SelectForAttempt_ProducesTheSameSequenceForTheSameSeed()
    {
        var level = CreateLevel();
        var first = new StairPlacementSelector(new Random(29));
        var second = new StairPlacementSelector(new Random(29));

        var firstSequence = Enumerable.Range(0, 12)
            .Select(_ => first.SelectForAttempt(level).StairCell)
            .ToArray();
        var secondSequence = Enumerable.Range(0, 12)
            .Select(_ => second.SelectForAttempt(level).StairCell)
            .ToArray();

        Assert.Equal(firstSequence, secondSequence);
    }

    [Fact]
    public void SelectForAttempt_ReturnsAnActiveCopyWithoutMutatingAuthoredData()
    {
        var level = CreateLevel();
        var originalStair = level.StairCell;
        var originalCandidates = level.StairCandidates.ToArray();
        var selector = new StairPlacementSelector(new Random(3));

        var active = selector.SelectForAttempt(level);

        Assert.NotSame(level, active);
        Assert.Equal(originalStair, level.StairCell);
        Assert.Equal(originalCandidates, level.StairCandidates);
        Assert.Equal(originalCandidates, active.StairCandidates);
    }

    [Fact]
    public void SelectForAttempt_RejectsARepeatedOrSingleCandidatePool()
    {
        var level = CreateLevel(
            2,
            new GridCoord(8, 6),
            new GridCoord(8, 6));
        var selector = new StairPlacementSelector(new Random(1));

        Assert.Throws<InvalidOperationException>(() => selector.SelectForAttempt(level));
    }

    private static LevelDefinition CreateLevel(int index = 2, params GridCoord[] candidates)
    {
        var defaultCandidates = new[]
        {
            new GridCoord(14, 3),
            new GridCoord(1, 1),
            new GridCoord(7, 11)
        };
        var stairCandidates = candidates.Length == 0 ? defaultCandidates : candidates;
        return new LevelDefinition
        {
            Index = index,
            StartingWick = 12,
            PlayerStart = new GridCoord(2, 9),
            LampStart = new GridCoord(2, 9),
            StairCell = stairCandidates[0],
            StairCandidates = stairCandidates
        };
    }
}
```

- [ ] **Step 2: Run the selector tests and verify RED**

Run:

```powershell
dotnet test tests\Wick.Tests\Wick.Tests.csproj --no-restore --filter "FullyQualifiedName~StairPlacementSelectorTests" --verbosity minimal
```

Expected: compilation fails because `StairCandidates`, `WithStairCell`, and `StairPlacementSelector` do not exist.

- [ ] **Step 3: Add candidate data and immutable cloning**

Modify `scripts/data/LevelDefinition.cs` so the complete class is:

```csharp
using System;
using Wick.Board;

namespace Wick.Data;

public sealed class LevelDefinition
{
    public int Index { get; init; }
    public int StartingWick { get; init; }
    public GridCoord PlayerStart { get; init; }
    public GridCoord LampStart { get; init; }
    public GridCoord StairCell { get; init; }
    public GridCoord[] StairCandidates { get; init; } = Array.Empty<GridCoord>();
    public GridCoord[] Walls { get; init; } = Array.Empty<GridCoord>();
    public GridCoord[] Crawlers { get; init; } = Array.Empty<GridCoord>();
    public GridCoord[] Moths { get; init; } = Array.Empty<GridCoord>();
    public GridCoord[] Flasks { get; init; } = Array.Empty<GridCoord>();
    public bool RequiresLampPlacement { get; init; }
    public int TargetCompletionWickMin { get; init; }
    public int TargetCompletionWickMax { get; init; }
    public int Width => LevelTuning.GridWidth;
    public int Height => LevelTuning.GridHeight;

    public LevelDefinition WithStairCell(GridCoord stairCell) => new()
    {
        Index = Index,
        StartingWick = StartingWick,
        PlayerStart = PlayerStart,
        LampStart = LampStart,
        StairCell = stairCell,
        StairCandidates = [.. StairCandidates],
        Walls = [.. Walls],
        Crawlers = [.. Crawlers],
        Moths = [.. Moths],
        Flasks = [.. Flasks],
        RequiresLampPlacement = RequiresLampPlacement,
        TargetCompletionWickMin = TargetCompletionWickMin,
        TargetCompletionWickMax = TargetCompletionWickMax
    };
}
```

- [ ] **Step 4: Implement the selector**

Create `scripts/goals/StairPlacementSelector.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using Wick.Board;
using Wick.Data;

namespace Wick.Goals;

public sealed class StairPlacementSelector
{
    private readonly Random _random;
    private readonly Dictionary<int, GridCoord> _previousByLevel = new();

    public StairPlacementSelector(Random random = null)
    {
        _random = random ?? Random.Shared;
    }

    public LevelDefinition SelectForAttempt(LevelDefinition authoredLevel)
    {
        ArgumentNullException.ThrowIfNull(authoredLevel);
        var candidates = authoredLevel.StairCandidates.Distinct().ToArray();
        if (candidates.Length < 2)
        {
            throw new InvalidOperationException(
                $"Level {authoredLevel.Index} requires at least two distinct stair candidates.");
        }

        var options = _previousByLevel.TryGetValue(authoredLevel.Index, out var previous)
            ? candidates.Where(candidate => candidate != previous).ToArray()
            : candidates;
        var selected = options[_random.Next(options.Length)];
        _previousByLevel[authoredLevel.Index] = selected;
        return authoredLevel.WithStairCell(selected);
    }
}
```

- [ ] **Step 5: Run selector tests and verify GREEN**

Run the Step 2 command.

Expected: all `StairPlacementSelectorTests` pass.

- [ ] **Step 6: Run focused existing data tests**

Run:

```powershell
dotnet test tests\Wick.Tests\Wick.Tests.csproj --no-restore --filter "FullyQualifiedName~LevelLoaderTests|FullyQualifiedName~StairPlacementSelectorTests" --verbosity minimal
```

Expected: all focused tests pass without warnings.

- [ ] **Step 7: Commit selector and data model**

```powershell
git add -- scripts/goals/StairPlacementSelector.cs scripts/data/LevelDefinition.cs tests/Wick.Tests/StairPlacementSelectorTests.cs
git commit -m "feat: add per-attempt stair selection"
```

---

### Task 2: Author And Validate All 21 Exit Candidates

**Files:**
- Modify: `scripts/data/LevelLoader.cs`
- Modify: `tests/Wick.Tests/LevelSolvabilityTests.cs`
- Modify: `tests/Wick.Tests/CampaignSimulationTests.cs`

**Interfaces:**
- Consumes: `LevelDefinition.StairCandidates` and `LevelDefinition.WithStairCell(GridCoord)` from Task 1.
- Produces: exactly three campaign stair candidates per level, with the first candidate retained as `StairCell` for authored/default simulations.

- [ ] **Step 1: Expand topology tests to validate every candidate**

In `Level_HasValidStartExitAndAuthoredTopology`, replace the single stair assertions with:

```csharp
Assert.Equal(3, level.StairCandidates.Length);
Assert.Equal(3, level.StairCandidates.Distinct().Count());
Assert.Equal(level.StairCandidates[0], level.StairCell);
Assert.All(level.StairCandidates, candidate =>
{
    Assert.True(board.IsWalkable(candidate));
    Assert.NotNull(FindShortestPath(board, level.PlayerStart, candidate));
});
```

In `Level_AllAuthoredObjectsConnectToThePlayableArea`, replace `.Append(level.StairCell)` with `.Concat(level.StairCandidates)`.

In `Level_AuthoredObjectsDoNotOverlapThePlayerOrEachOther`, replace `.Append(level.StairCell)` with `.Concat(level.StairCandidates)` so all candidates are distinct from every object and from one another.

Change `Level_HasAWinningResourceRouteWithoutTopologyDeadlock` to loop over every candidate:

```csharp
foreach (var candidate in level.StairCandidates)
{
    var path = FindShortestPath(board, level.PlayerStart, candidate);
    Assert.NotNull(path);
    var requiredActionCost = path!.Count - 1 + (level.RequiresLampPlacement ? 1 : 0);
    var availableWick = level.StartingWick + (level.Flasks.Length * LevelTuning.FlaskRestore);
    Assert.True(requiredActionCost < availableWick,
        $"Level {levelIndex} candidate {candidate} has no wick margin.");
}
```

- [ ] **Step 2: Expand hazard-aware seed validation to every candidate**

Replace `Campaign_BothRandomTieBreakOutcomesPreserveWinningRoutes` with:

```csharp
[Theory]
[InlineData(1u)]
[InlineData(2u)]
public void Campaign_EveryStairCandidatePreservesAWinningRoute(uint tieBreakerSeed)
{
    foreach (var authoredLevel in new LevelLoader().LoadCampaign())
    {
        foreach (var candidate in authoredLevel.StairCandidates)
        {
            var level = authoredLevel.WithStairCell(candidate);
            var result = CampaignSimulationHarness.FindWinningPlan(
                level,
                new SimulationRequirements(RequireFlare: level.Index == 1),
                tieBreakerSeed: tieBreakerSeed);

            Assert.True(result.Won,
                $"Level {level.Index}, stair {candidate}, seed {tieBreakerSeed}: {result.Summary}");
            Assert.InRange(
                result.RemainingWick,
                level.TargetCompletionWickMin,
                level.TargetCompletionWickMax);
            if (level.RequiresLampPlacement)
            {
                Assert.True(result.PlacedLamp,
                    $"Level {level.Index}, stair {candidate} bypassed required lamp placement.");
            }
        }
    }
}
```

- [ ] **Step 3: Run candidate tests and verify RED**

Run:

```powershell
dotnet test tests\Wick.Tests\Wick.Tests.csproj --no-restore --filter "FullyQualifiedName~LevelSolvabilityTests|FullyQualifiedName~Campaign_EveryStairCandidate" --verbosity minimal
```

Expected: failures report that campaign levels have no three-cell candidate pools.

- [ ] **Step 4: Replace each fixed stair argument with its verified candidate array**

Change `CreateLevel` to accept `GridCoord[] stairCandidates` instead of `GridCoord stairCell`. Set:

```csharp
StairCell = stairCandidates[0],
StairCandidates = [.. stairCandidates],
```

Use these exact solver-verified arrays in `CreateCampaign`:

```csharp
// Level 1
new[] { new GridCoord(14, 9), new GridCoord(6, 1), new GridCoord(4, 11) }

// Level 2
new[] { new GridCoord(14, 3), new GridCoord(1, 1), new GridCoord(7, 11) }

// Level 3
new[] { new GridCoord(16, 6), new GridCoord(4, 1), new GridCoord(4, 11) }

// Level 4
new[] { new GridCoord(16, 2), new GridCoord(8, 11), new GridCoord(4, 5) }

// Level 5
new[] { new GridCoord(16, 10), new GridCoord(8, 1), new GridCoord(1, 8) }

// Level 6
new[] { new GridCoord(16, 2), new GridCoord(1, 4), new GridCoord(8, 11) }

// Level 7
new[] { new GridCoord(16, 6), new GridCoord(6, 1), new GridCoord(6, 11) }
```

At the start of `CreateLevel`, enforce the authored-data contract:

```csharp
if (stairCandidates is null || stairCandidates.Length != 3 || stairCandidates.Distinct().Count() != 3)
{
    throw new ArgumentException("Campaign levels require exactly three distinct stair candidates.", nameof(stairCandidates));
}
```

- [ ] **Step 5: Run all candidate and simulation tests**

Run the Step 3 command.

Expected: all topology tests pass; all 21 candidates win within their target wick bands for seeds `1` and `2`.

- [ ] **Step 6: Commit authored candidates and validation**

```powershell
git add -- scripts/data/LevelLoader.cs tests/Wick.Tests/LevelSolvabilityTests.cs tests/Wick.Tests/CampaignSimulationTests.cs
git commit -m "feat: author validated campaign exits"
```

---

### Task 3: Select A New Exit For Every Runtime Attempt

**Files:**
- Modify: `scripts/core/LevelManager.cs`
- Test: `tests/Wick.Tests/StairPlacementSelectorTests.cs`

**Interfaces:**
- Consumes: `StairPlacementSelector.SelectForAttempt(LevelDefinition)` and authored definitions with three candidates.
- Produces: `_currentLevel` as the selected active definition before stair, HUD, board, hazards, and pickups initialize.

- [ ] **Step 1: Add an attempt-lifecycle regression test**

Add this test to `StairPlacementSelectorTests.cs`:

```csharp
[Fact]
public void SelectForAttempt_RemembersPreviousStairsIndependentlyPerLevel()
{
    var levelTwo = CreateLevel(index: 2);
    var levelThree = CreateLevel(index: 3);
    var selector = new StairPlacementSelector(new Random(41));

    var levelTwoFirst = selector.SelectForAttempt(levelTwo).StairCell;
    selector.SelectForAttempt(levelThree);
    var levelTwoRetry = selector.SelectForAttempt(levelTwo).StairCell;

    Assert.NotEqual(levelTwoFirst, levelTwoRetry);
}
```

The helper from Task 1 already accepts the optional level index. This test proves moving to another level does not erase the remembered retry position.

- [ ] **Step 2: Run the lifecycle test**

Run:

```powershell
dotnet test tests\Wick.Tests\Wick.Tests.csproj --no-restore --filter "FullyQualifiedName~SelectForAttempt_RemembersPreviousStairsIndependentlyPerLevel" --verbosity minimal
```

Expected: PASS because Task 1 stores previous cells by level index.

- [ ] **Step 3: Wire the selector into level loading**

In `LevelManager`, add the field beside `_loader`:

```csharp
private readonly StairPlacementSelector _stairPlacement = new();
```

In `LoadActiveLevel`, replace:

```csharp
_currentLevel = _campaignScene.Definition;
```

with:

```csharp
_currentLevel = _stairPlacement.SelectForAttempt(_campaignScene.Definition);
```

Do not call the selector from `TryUndo`. Every full `LoadActiveLevel` call represents a new attempt, so initial entry, automatic death retry, manual `R`, Practice Mode entry, and new-run loads reroll automatically. Existing consumers continue using `_currentLevel.StairCell` and require no duplicated stair state.

- [ ] **Step 4: Run runtime-adjacent regression tests**

Run:

```powershell
dotnet test tests\Wick.Tests\Wick.Tests.csproj --no-restore --filter "FullyQualifiedName~StairPlacementSelectorTests|FullyQualifiedName~StairEntry_UsesActualLampOwnershipRule|FullyQualifiedName~PracticeModeTests|FullyQualifiedName~HudPresentationStateTests" --verbosity minimal
```

Expected: all selector, stair-entry, Practice Mode, and HUD tests pass.

- [ ] **Step 5: Build the Godot C# solution**

Run:

```powershell
dotnet build Wick.sln --no-restore --verbosity minimal
```

Expected: `Build succeeded`, `0 Warning(s)`, `0 Error(s)`.

- [ ] **Step 6: Commit runtime wiring**

```powershell
git add -- scripts/core/LevelManager.cs tests/Wick.Tests/StairPlacementSelectorTests.cs
git commit -m "feat: reroll stairs on level retries"
```

---

### Task 4: Full Verification, Windows Package, And Play Check

**Files:**
- Verify: `docs/superpowers/specs/2026-08-17-random-stair-placement-design.md`
- Verify: `build/Wick-Windows/`
- Verify: `build/Wick-Windows.zip`

**Interfaces:**
- Consumes: all implementation and validation from Tasks 1-3.
- Produces: a verified Windows package in which retries visibly select a different validated stair.

- [ ] **Step 1: Run the complete automated suite**

```powershell
dotnet test tests\Wick.Tests\Wick.Tests.csproj --no-restore --verbosity minimal
```

Expected: every test passes, including all 21 stair candidates under both crawler seeds. Record the passed/failed totals.

- [ ] **Step 2: Run a clean solution build**

```powershell
dotnet build Wick.sln --no-restore --verbosity minimal
```

Expected: `0 Warning(s)` and `0 Error(s)`.

- [ ] **Step 3: Export through Godot**

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\tools\package_windows.ps1
```

Expected: Godot completes `dotnet_publish_project` and `[ DONE ] savepack`. If the known console wrapper remains open after `savepack`, stop only that completed wrapper with `Ctrl+C`, then continue to Step 4.

- [ ] **Step 4: Rebuild and verify the ZIP from the completed export**

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\tools\package_windows.ps1 -SkipExport
```

Expected: `Created verified Windows package: ...\build\Wick-Windows.zip`.

- [ ] **Step 5: Verify package integrity**

```powershell
$zip = Resolve-Path 'build\Wick-Windows.zip'
$hash = Get-FileHash -Algorithm SHA256 -LiteralPath $zip
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [System.IO.Compression.ZipFile]::OpenRead($zip)
try {
    $entries = @($archive.Entries)
    $wickDlls = @($entries | Where-Object { $_.FullName -match '(^|[\\/])Wick\.dll$' })
    if ($wickDlls.Count -ne 1) { throw "Expected one Wick.dll, found $($wickDlls.Count)." }
    $hash
} finally {
    $archive.Dispose()
}
```

Expected: exactly one packaged `Wick.dll`; record the final ZIP byte size and SHA-256 hash.

- [ ] **Step 6: Launch the rebuilt game and verify retry behavior**

Launch `build\Wick-Windows\Wick.exe`. In a level, note the visible stair cell, press `R`, and reveal the stair again. Confirm the stair is at a different candidate cell. Repeat once to confirm consecutive attempts never repeat. Enter Practice Mode, make one successful move, press `Z`, and confirm Undo does not move the stair.

- [ ] **Step 7: Inspect the final diff without disturbing audio work**

```powershell
git status --short
git diff --check
git log -4 --oneline
```

Expected: stair implementation files are committed in focused commits; previously approved audio files may remain modified if they were not separately committed, and no unrelated file is reverted.
