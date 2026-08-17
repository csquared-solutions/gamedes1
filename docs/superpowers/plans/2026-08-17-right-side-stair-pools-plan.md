# Right-Side Stair Pools Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (- [ ]) syntax for tracking.

**Goal:** Expand all seven campaign stair pools from three cells to every simulation-approved cell in the playable rightmost third while preserving retry movement and Undo stability.

**Architecture:** Keep LevelDefinition.StairCandidates, StairPlacementSelector, and LevelManager as the established immutable-data and per-attempt runtime path. Replace only the campaign's three-cell arrays with the 212 simulation-approved right-side coordinates, strengthen loader validation, then validate every declared cell through bounded parallel campaign simulations.

**Tech Stack:** Godot 4.7 Mono, C#/.NET 8, xUnit, PowerShell Windows packaging

**Spec:** docs/superpowers/specs/2026-08-17-random-stair-placement-design.md

## Global Constraints

- The board is 19 columns wide; the rightmost third begins at X = (19 * 2) / 3 = 12, and boundary walls limit playable candidates to columns 12 through 17.
- Pool sizes for levels 1-7 must be exactly 8, 21, 23, 44, 32, 39, 45, totaling 212 validated candidates.
- The original fixed stair remains each level's authored/default StairCell and must be present in its pool.
- Every retry must choose a different cell from the immediately previous attempt for that level.
- Same-level and backward cross-level Undo must preserve the active stair.
- Do not randomize geometry, player, lamp, flask, crawler, moth, wick, darkness, or campaign rules.
- Every candidate must pass both crawler tie-break seeds with a 20,000-state finite bound per route; do not sample the pool.
- Add no external dependencies.
- Use NUGET_PACKAGES=C:\Users\Surface\.nuget\packages and --no-restore for local dotnet verification because the network vulnerability feed is unavailable.
- The main checkout contains approved uncommitted audio work; stage only files named by each task and do not revert or overwrite those audio changes.

---

### Task 1: Replace Three-Cell Data With Validated Right-Side Pools

**Files:**
- Modify: scripts/data/LevelLoader.cs
- Modify: tests/Wick.Tests/LevelSolvabilityTests.cs

**Interfaces:**
- Consumes: existing LevelDefinition.StairCell, GridCoord[] StairCandidates, and LevelDefinition.WithStairCell(GridCoord).
- Produces: LevelLoader.CreateLevel(..., GridCoord defaultStair, GridCoord[] stairCandidates, ...) with variable-size validated pools and unchanged immutable level definitions.

- [ ] **Step 1: Replace the three-candidate topology assertions with the right-side pool contract**

In Level_HasValidStartExitAndAuthoredTopology, replace the exact-three assertions with:

~~~csharp
var expectedPoolSizes = new[] { 8, 21, 23, 44, 32, 39, 45 };
var firstRightColumn = (level.Width * 2) / 3;

Assert.Equal(expectedPoolSizes[levelIndex - 1], level.StairCandidates.Length);
Assert.Equal(level.StairCandidates.Length, level.StairCandidates.Distinct().Count());
Assert.Contains(level.StairCell, level.StairCandidates);
Assert.All(level.StairCandidates, candidate =>
{
    Assert.InRange(candidate.X, firstRightColumn, level.Width - 2);
    Assert.True(board.IsWalkable(candidate));
    Assert.NotNull(FindShortestPath(board, level.PlayerStart, candidate));
});
Assert.Equal(
    level.StairCandidates.OrderBy(candidate => candidate.X).ThenBy(candidate => candidate.Y),
    level.StairCandidates);
~~~

Add a separate default-stair regression:

~~~csharp
[Fact]
public void Campaign_RetainsOriginalStairsInsideExpandedPools()
{
    var expected = new[]
    {
        new GridCoord(14, 9),
        new GridCoord(14, 3),
        new GridCoord(16, 6),
        new GridCoord(16, 2),
        new GridCoord(16, 10),
        new GridCoord(16, 2),
        new GridCoord(16, 6)
    };
    var levels = new LevelLoader().LoadCampaign();

    Assert.Equal(expected, levels.Select(level => level.StairCell));
    Assert.All(levels, level => Assert.Contains(level.StairCell, level.StairCandidates));
}
~~~

- [ ] **Step 2: Run the topology tests to verify the old three-cell data fails**

Run:

~~~powershell
$env:NUGET_PACKAGES='C:\Users\Surface\.nuget\packages'
dotnet test tests\Wick.Tests\Wick.Tests.csproj --no-restore --filter "FullyQualifiedName~Level_HasValidStartExitAndAuthoredTopology|FullyQualifiedName~Campaign_RetainsOriginalStairsInsideExpandedPools" --verbosity minimal
~~~

Expected: the seven topology cases fail on pool size and right-side placement because the current data still contains three candidates, including left-side cells.

- [ ] **Step 3: Generalize CreateLevel without changing runtime consumers**

Change the signature to accept the default separately:

~~~csharp
private static LevelDefinition CreateLevel(
    int index,
    int startingWick,
    GridCoord playerStart,
    GridCoord defaultStair,
    GridCoord[] stairCandidates,
    GridCoord[] crawlers,
    GridCoord[] moths,
    GridCoord[] flasks,
    GridCoord[] interiorWalls,
    bool requiresLampPlacement = false)
~~~

Replace the exact-three guard with:

~~~csharp
ArgumentNullException.ThrowIfNull(stairCandidates);
var firstRightColumn = (LevelTuning.GridWidth * 2) / 3;
if (stairCandidates.Length < 2 || stairCandidates.Distinct().Count() != stairCandidates.Length)
{
    throw new ArgumentException(
        "Campaign levels require at least two distinct stair candidates.",
        nameof(stairCandidates));
}

if (stairCandidates.Any(candidate =>
        candidate.X < firstRightColumn ||
        candidate.X >= LevelTuning.GridWidth - 1))
{
    throw new ArgumentException(
        $"Campaign stair candidates must be in playable right-side columns {firstRightColumn}-{LevelTuning.GridWidth - 2}.",
        nameof(stairCandidates));
}

if (!stairCandidates.Contains(defaultStair))
{
    throw new ArgumentException(
        $"Level {index} default stair {defaultStair} must belong to its candidate pool.",
        nameof(defaultStair));
}
~~~

Set StairCell = defaultStair and retain StairCandidates = [.. stairCandidates].

- [ ] **Step 4: Replace all seven campaign pools with the simulation-approved coordinates**

Pass each original default stair separately and use these exact stable-order arrays:

~~~csharp
// Level 1: default new GridCoord(14, 9)
new[]
{
    new GridCoord(12, 5), new GridCoord(12, 6), new GridCoord(12, 7),
    new GridCoord(12, 9), new GridCoord(12, 11), new GridCoord(13, 6),
    new GridCoord(13, 9), new GridCoord(14, 9)
}

// Level 2: default new GridCoord(14, 3)
new[]
{
    new GridCoord(12, 1), new GridCoord(12, 2), new GridCoord(12, 3),
    new GridCoord(12, 4), new GridCoord(12, 5), new GridCoord(12, 6),
    new GridCoord(12, 9), new GridCoord(12, 10), new GridCoord(12, 11),
    new GridCoord(13, 2), new GridCoord(13, 3), new GridCoord(13, 4),
    new GridCoord(13, 5), new GridCoord(13, 6), new GridCoord(13, 10),
    new GridCoord(13, 11), new GridCoord(14, 3), new GridCoord(14, 4),
    new GridCoord(14, 5), new GridCoord(14, 11), new GridCoord(15, 4)
}

// Level 3: default new GridCoord(16, 6)
new[]
{
    new GridCoord(12, 1), new GridCoord(12, 11), new GridCoord(13, 2),
    new GridCoord(13, 3), new GridCoord(13, 4), new GridCoord(13, 8),
    new GridCoord(13, 9), new GridCoord(13, 10), new GridCoord(14, 3),
    new GridCoord(14, 4), new GridCoord(14, 5), new GridCoord(14, 7),
    new GridCoord(14, 8), new GridCoord(14, 9), new GridCoord(15, 4),
    new GridCoord(15, 5), new GridCoord(15, 6), new GridCoord(15, 7),
    new GridCoord(15, 8), new GridCoord(16, 5), new GridCoord(16, 6),
    new GridCoord(16, 7), new GridCoord(17, 6)
}

// Level 4: default new GridCoord(16, 2)
new[]
{
    new GridCoord(12, 2), new GridCoord(12, 3), new GridCoord(12, 4),
    new GridCoord(12, 5), new GridCoord(12, 6), new GridCoord(12, 8),
    new GridCoord(12, 9), new GridCoord(12, 10), new GridCoord(12, 11),
    new GridCoord(13, 1), new GridCoord(13, 2), new GridCoord(13, 3),
    new GridCoord(13, 5), new GridCoord(13, 6), new GridCoord(13, 7),
    new GridCoord(13, 8), new GridCoord(13, 9), new GridCoord(13, 10),
    new GridCoord(13, 11), new GridCoord(14, 2), new GridCoord(14, 3),
    new GridCoord(14, 4), new GridCoord(14, 5), new GridCoord(14, 6),
    new GridCoord(14, 8), new GridCoord(14, 9), new GridCoord(14, 10),
    new GridCoord(15, 1), new GridCoord(15, 2), new GridCoord(15, 3),
    new GridCoord(15, 4), new GridCoord(15, 5), new GridCoord(15, 6),
    new GridCoord(15, 8), new GridCoord(15, 9), new GridCoord(16, 2),
    new GridCoord(16, 3), new GridCoord(16, 4), new GridCoord(16, 5),
    new GridCoord(16, 6), new GridCoord(16, 8), new GridCoord(17, 3),
    new GridCoord(17, 4), new GridCoord(17, 5)
}

// Level 5: default new GridCoord(16, 10)
new[]
{
    new GridCoord(12, 2), new GridCoord(12, 3), new GridCoord(12, 4),
    new GridCoord(12, 5), new GridCoord(12, 7), new GridCoord(12, 8),
    new GridCoord(12, 9), new GridCoord(12, 10), new GridCoord(13, 1),
    new GridCoord(13, 2), new GridCoord(13, 3), new GridCoord(13, 4),
    new GridCoord(13, 5), new GridCoord(13, 7), new GridCoord(13, 8),
    new GridCoord(13, 9), new GridCoord(14, 10), new GridCoord(15, 5),
    new GridCoord(15, 6), new GridCoord(15, 7), new GridCoord(15, 8),
    new GridCoord(15, 9), new GridCoord(15, 10), new GridCoord(15, 11),
    new GridCoord(16, 6), new GridCoord(16, 7), new GridCoord(16, 8),
    new GridCoord(16, 9), new GridCoord(16, 10), new GridCoord(17, 7),
    new GridCoord(17, 8), new GridCoord(17, 9)
}

// Level 6: default new GridCoord(16, 2)
new[]
{
    new GridCoord(12, 2), new GridCoord(12, 3), new GridCoord(12, 4),
    new GridCoord(12, 5), new GridCoord(12, 6), new GridCoord(12, 7),
    new GridCoord(12, 8), new GridCoord(12, 9), new GridCoord(12, 10),
    new GridCoord(12, 11), new GridCoord(13, 1), new GridCoord(13, 2),
    new GridCoord(13, 8), new GridCoord(13, 9), new GridCoord(13, 10),
    new GridCoord(13, 11), new GridCoord(14, 2), new GridCoord(14, 3),
    new GridCoord(14, 4), new GridCoord(14, 5), new GridCoord(14, 6),
    new GridCoord(14, 8), new GridCoord(15, 1), new GridCoord(15, 2),
    new GridCoord(15, 3), new GridCoord(15, 4), new GridCoord(15, 5),
    new GridCoord(15, 6), new GridCoord(16, 2), new GridCoord(16, 3),
    new GridCoord(16, 4), new GridCoord(16, 5), new GridCoord(16, 6),
    new GridCoord(17, 1), new GridCoord(17, 2), new GridCoord(17, 3),
    new GridCoord(17, 4), new GridCoord(17, 5), new GridCoord(17, 7)
}

// Level 7: default new GridCoord(16, 6)
new[]
{
    new GridCoord(12, 2), new GridCoord(12, 3), new GridCoord(12, 5),
    new GridCoord(12, 6), new GridCoord(12, 7), new GridCoord(12, 8),
    new GridCoord(12, 10), new GridCoord(12, 11), new GridCoord(13, 1),
    new GridCoord(13, 2), new GridCoord(13, 6), new GridCoord(13, 11),
    new GridCoord(14, 2), new GridCoord(14, 3), new GridCoord(14, 4),
    new GridCoord(14, 5), new GridCoord(14, 6), new GridCoord(14, 7),
    new GridCoord(14, 8), new GridCoord(14, 9), new GridCoord(14, 10),
    new GridCoord(15, 1), new GridCoord(15, 2), new GridCoord(15, 3),
    new GridCoord(15, 4), new GridCoord(15, 5), new GridCoord(15, 6),
    new GridCoord(15, 7), new GridCoord(15, 9), new GridCoord(15, 10),
    new GridCoord(15, 11), new GridCoord(16, 2), new GridCoord(16, 3),
    new GridCoord(16, 4), new GridCoord(16, 6), new GridCoord(16, 7),
    new GridCoord(16, 8), new GridCoord(16, 9), new GridCoord(16, 10),
    new GridCoord(17, 1), new GridCoord(17, 2), new GridCoord(17, 3),
    new GridCoord(17, 7), new GridCoord(17, 8), new GridCoord(17, 9)
}
~~~

- [ ] **Step 5: Run the complete topology and object-safety group**

Run:

~~~powershell
$env:NUGET_PACKAGES='C:\Users\Surface\.nuget\packages'
dotnet test tests\Wick.Tests\Wick.Tests.csproj --no-restore --filter "FullyQualifiedName~LevelSolvabilityTests" --verbosity minimal
~~~

Expected: every solvability, reachability, overlap, scene, wick-margin, and original-stair test passes.

- [ ] **Step 6: Commit the expanded data**

~~~powershell
git add -- scripts/data/LevelLoader.cs tests/Wick.Tests/LevelSolvabilityTests.cs
git commit -m "feat: expand stairs across right-side pools"
~~~

---

### Task 2: Exhaustively Validate All 212 Candidates

**Files:**
- Modify: tests/Wick.Tests/CampaignSimulationTests.cs
- Modify: tests/Wick.Tests/StairPlacementSelectorTests.cs

**Interfaces:**
- Consumes: the seven variable-size StairCandidates arrays from Task 1 and existing CampaignSimulationHarness.FindWinningPlan(..., int maxStates, uint tieBreakerSeed).
- Produces: exhaustive two-seed safety coverage for all 212 candidates and a selector regression for pools larger than three.

- [ ] **Step 1: Add the large-pool selector contract**

Add:

~~~csharp
[Fact]
public void SelectForAttempt_SupportsPoolsLargerThanThree()
{
    var candidates = Enumerable.Range(0, 8)
        .Select(offset => new GridCoord(12 + (offset / 4), 2 + (offset % 4)))
        .ToArray();
    var level = CreateLevel(2, candidates);
    var selector = new StairPlacementSelector(new Random(73));
    var selected = Enumerable.Range(0, 40)
        .Select(_ => selector.SelectForAttempt(level).StairCell)
        .ToArray();

    Assert.All(selected, cell => Assert.Contains(cell, candidates));
    Assert.True(selected.Distinct().Count() > 3);
    Assert.All(selected.Zip(selected.Skip(1)), pair => Assert.NotEqual(pair.First, pair.Second));
}
~~~

- [ ] **Step 2: Replace the sequential two-row candidate theory with bounded controlled parallel validation**

Add using System.Collections.Concurrent; and replace Campaign_EveryStairCandidatePreservesAWinningRoute with:

~~~csharp
[Fact]
public void Campaign_EveryRightSideStairCandidatePreservesAWinningRoute()
{
    var cases = new LevelLoader().LoadCampaign()
        .SelectMany(level => level.StairCandidates.Select(candidate => (level, candidate)))
        .ToArray();
    var failures = new ConcurrentQueue<string>();

    Assert.Equal(212, cases.Length);
    Parallel.ForEach(
        cases,
        new ParallelOptions { MaxDegreeOfParallelism = 10 },
        item =>
        {
            foreach (var tieBreakerSeed in new[] { 1u, 2u })
            {
                var active = item.level.WithStairCell(item.candidate);
                var result = CampaignSimulationHarness.FindWinningPlan(
                    active,
                    new SimulationRequirements(RequireFlare: active.Index == 1),
                    maxStates: 20_000,
                    tieBreakerSeed: tieBreakerSeed);
                var valid = result.Won &&
                    result.RemainingWick >= active.TargetCompletionWickMin &&
                    result.RemainingWick <= active.TargetCompletionWickMax &&
                    (!active.RequiresLampPlacement || result.PlacedLamp);
                if (!valid)
                {
                    failures.Enqueue(
                        $"Level {active.Index}, stair {item.candidate}, seed {tieBreakerSeed}: {result.Summary}");
                }
            }
        });

    Assert.Empty(failures);
}
~~~

- [ ] **Step 3: Run the selector suite**

~~~powershell
$env:NUGET_PACKAGES='C:\Users\Surface\.nuget\packages'
dotnet test tests\Wick.Tests\Wick.Tests.csproj --no-restore --filter "FullyQualifiedName~StairPlacementSelectorTests" --verbosity minimal
~~~

Expected: all selector tests pass, including more than three distinct observed selections and no consecutive repeat.

- [ ] **Step 4: Run the exhaustive campaign candidate proof**

~~~powershell
$env:NUGET_PACKAGES='C:\Users\Surface\.nuget\packages'
dotnet test tests\Wick.Tests\Wick.Tests.csproj --no-restore --filter "FullyQualifiedName~Campaign_EveryRightSideStairCandidatePreservesAWinningRoute" --verbosity minimal
~~~

Expected: one exhaustive test passes after validating all 212 candidates under both seeds. The planning probe completed the same proof in approximately 25 minutes on this machine.

- [ ] **Step 5: Commit the expanded safety coverage**

~~~powershell
git add -- tests/Wick.Tests/CampaignSimulationTests.cs tests/Wick.Tests/StairPlacementSelectorTests.cs
git commit -m "test: validate expanded right-side stairs"
~~~

---

### Task 3: Full Build, Package, And Runtime Verification

**Files:**
- Verify: scripts/core/LevelManager.cs
- Verify: scripts/goals/StairPlacementSelector.cs
- Verify: build/Wick-Windows/
- Verify: build/Wick-Windows.zip

**Interfaces:**
- Consumes: all expanded data and safety tests from Tasks 1-2 plus the existing retry/Undo runtime path.
- Produces: a verified Windows package where every observed stair is in the rightmost third, retries move it, and Undo retains it.

- [ ] **Step 1: Run the complete automated suite**

~~~powershell
$env:NUGET_PACKAGES='C:\Users\Surface\.nuget\packages'
dotnet test tests\Wick.Tests\Wick.Tests.csproj --no-restore --verbosity minimal
~~~

Expected: every test passes with zero failures. Record total tests and duration; allow only the known offline NU1900 metadata warning.

- [ ] **Step 2: Build the solution**

~~~powershell
$env:NUGET_PACKAGES='C:\Users\Surface\.nuget\packages'
dotnet build Wick.sln --no-restore --verbosity minimal
~~~

Expected: Build succeeded with zero errors; allow only the known NU1900 warning.

- [ ] **Step 3: Ensure the ignored export prerequisites exist**

~~~powershell
New-Item -ItemType Directory -Force -Path 'build\Wick-Windows' | Out-Null
if (-not (Test-Path -LiteralPath 'build\Wick-Windows\README.txt')) {
    $common = Resolve-Path (git rev-parse --git-common-dir)
    $mainRoot = Resolve-Path (Join-Path $common '..')
    Copy-Item -LiteralPath (Join-Path $mainRoot 'build\Wick-Windows\README.txt') -Destination 'build\Wick-Windows\README.txt'
}
~~~

Expected: the export directory and the existing 506-byte player README are present without changing tracked source files.

- [ ] **Step 4: Export and rebuild the verified ZIP**

~~~powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\tools\package_windows.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\tools\package_windows.ps1 -SkipExport
~~~

Expected: Godot completes dotnet_publish_project and savepack, followed by Created verified Windows package.

- [ ] **Step 5: Verify package assembly integrity**

~~~powershell
$zip = Resolve-Path 'build\Wick-Windows.zip'
$hash = Get-FileHash -Algorithm SHA256 -LiteralPath $zip
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [System.IO.Compression.ZipFile]::OpenRead($zip)
try {
    $wickDlls = @($archive.Entries | Where-Object { $_.FullName -match '(^|[\\/])Wick\.dll$' })
    if ($wickDlls.Count -ne 1) {
        throw "Expected one Wick.dll, found $($wickDlls.Count)."
    }
    Get-Item -LiteralPath $zip | Select-Object FullName, Length
    $hash
    $wickDlls | Select-Object FullName, Length, CompressedLength
}
finally {
    $archive.Dispose()
}
~~~

Expected: exactly one data_Wick_windows_x86_64/Wick.dll; record ZIP size and SHA-256.

- [ ] **Step 6: Launch and play-check the rebuilt game**

Launch build\Wick-Windows\Wick.exe. For at least three attempts on level one:

1. Reveal the stair and confirm it appears only in the right-side region.
2. Press R, reveal it again, and confirm the next position differs.
3. Repeat R once more and confirm there is no consecutive repeat.
4. Enter Practice Mode, make one successful move, press Z, and confirm the stair does not move.

Expected: all observed stairs are in columns 12-17, consecutive retries move, and Undo retains the current stair.

- [ ] **Step 7: Inspect the final branch without disturbing main-checkout audio**

~~~powershell
git status --short
git diff --check
git log -6 --oneline
~~~

Expected: only focused committed stair-data and test changes exist on the feature branch; the main checkout's approved audio changes remain untouched.
