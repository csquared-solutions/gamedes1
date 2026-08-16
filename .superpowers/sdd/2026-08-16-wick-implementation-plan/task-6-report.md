# Task 6 Report: Oil Flask, Flare, and Tactical Readability UI

## Implementation Summary

- Added `OilFlaskState`, a pure collaborator that spends two wick and advances two turns before restoring exactly `LevelTuning.FlaskRestore` (6) wick.
- Added `OilFlaskController` and wired it through `LevelManager`; `G` locks player input, forces temporary darkness, advances hazards for both lift turns, restores wick, and refreshes tactical visibility.
- Added `WickState.TryFireFlare()` and one-turn flare modifier behavior. `F` spends exactly `LevelTuning.FlareCost` (3) wick, doubles sight radius during its turn, advances hazards, then expires.
- Added root-integrated HUD, wick gauge, warning, moth-threshold pulse, return-cost readout, hazard state labels, lamp tether, and loss-cause text.
- Added runtime scene wiring for `OilFlask`, `LampTether`, and `Hud` under `GameRoot`.

## TDD Evidence

### RED

Command:

```powershell
dotnet test tests\Wick.Tests\Wick.Tests.csproj --filter "FullyQualifiedName~OilFlaskStateTests|FullyQualifiedName~WickSystemTests.Flare"
```

Output after a network-enabled restore:

```text
error CS1061: 'WickState' does not contain a definition for 'TryFireFlare'
error CS1061: 'WickState' does not contain a definition for 'AdvanceTurn'
```

The first sandboxed invocation could not access `https://api.nuget.org/v3/index.json`; the same command was retried with network permission and produced the expected missing-feature failure above.

Self-review regression RED command:

```powershell
dotnet test tests\Wick.Tests\Wick.Tests.csproj --filter "FullyQualifiedName~WickSystemTests.Initialize_ClearsAnActiveFlareModifier"
```

Output:

```text
Assert.Equal() Failure: Values differ
Expected: 6
Actual:   12
Failed: 1, Passed: 0
```

### GREEN

Command:

```powershell
dotnet test tests\Wick.Tests\Wick.Tests.csproj --filter "FullyQualifiedName~OilFlaskStateTests|FullyQualifiedName~WickSystemTests.Flare|FullyQualifiedName~GameRootSceneTests"
```

Output:

```text
Passed! - Failed: 0, Passed: 3, Skipped: 0, Total: 3
```

Regression GREEN command:

```powershell
dotnet test tests\Wick.Tests\Wick.Tests.csproj --filter "FullyQualifiedName~WickSystemTests.Initialize_ClearsAnActiveFlareModifier"
```

Output:

```text
Passed! - Failed: 0, Passed: 1, Skipped: 0, Total: 1
```

## Full Verification

```powershell
dotnet test tests\Wick.Tests\Wick.Tests.csproj
```

```text
Passed! - Failed: 0, Passed: 29, Skipped: 0, Total: 29
```

```powershell
dotnet build Wick.csproj --no-restore
```

```text
Build succeeded.
0 Warning(s)
0 Error(s)
```

## Files Changed

- `scenes/game/GameRoot.tscn`
- `scenes/pickups/OilFlask.tscn`
- `scenes/ui/HazardStateIcon.tscn`
- `scenes/ui/Hud.tscn`
- `scenes/ui/WickGauge.tscn`
- `scripts/actors/PlayerController.cs`
- `scripts/board/BoardRenderer.cs`
- `scripts/core/LevelManager.cs`
- `scripts/core/WickState.cs`
- `scripts/core/WickSystem.cs`
- `scripts/hazards/HazardBase.cs`
- `scripts/hazards/HazardSystem.cs`
- `scripts/pickups/OilFlaskController.cs`
- `scripts/ui/HazardStateIndicatorController.cs`
- `scripts/ui/HudController.cs`
- `scripts/ui/LampTetherController.cs`
- `scripts/ui/WickGaugeController.cs`
- `tests/Wick.Tests/GameRootSceneTests.cs`
- `tests/Wick.Tests/OilFlaskStateTests.cs`
- `tests/Wick.Tests/WickSystemTests.cs`

## Self-Review Findings

- Found and fixed a flare lifecycle defect: level initialization did not clear an active flare modifier, allowing a restarted level to retain doubled radius. Added a focused red/green regression test.
- No unresolved correctness, integration, or diff-whitespace findings remain. Tactical UI reveals only current state and Manhattan return cost; it does not calculate or display a route.

## Concerns and Environment Limitations

- `godot` and `godot4` are not installed or on `PATH`, so no headless Godot scene-load/manual runtime verification could be performed in this environment.
- The first test restore required network permission because the default sandbox blocked NuGet access. Subsequent focused and full test runs passed.
