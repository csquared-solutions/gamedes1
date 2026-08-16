# Task 3 Report: Tile Visibility, Memory, and Fixed Camera

## Delivered

- Added a pure `VisibilitySystem` that evaluates sight from the lamp cell with Manhattan radius bounds and center-to-center grid line-of-sight.
- Added persistent seen-memory state. Current visibility is recomputed each call while remembered cells remain available through `WasSeen` and the returned `VisibilityResult`.
- Extended `BoardState` with validated static wall geometry and `IsWall`, enabling visibility to stop at walls while keeping the blocking wall visible.
- Added a test-only `TestBoards` fixture with the required `19 x 13` empty-room layout.
- Added `scenes/board/Board.tscn` with a fixed `Camera2D` centered on the configured `1216 x 832` viewport and no position smoothing, then registered that board scene under `GameRoot`.
- Preserved the existing minimal `VisibilityState` renderer scaffold. It already exposes the required `Hidden`, `Remembered`, and `Visible` states, so no renderer behavior was invented before tiles exist.
- Left `WickSystem` unchanged. Its existing `CurrentRadius` is the required wick-to-radius source; `VisibilitySystem.Recompute` accepts that radius directly, so this slice needs no additional wick behavior.

## TDD Evidence

### RED

Created `tests/Wick.Tests/VisibilitySystemTests.cs` before adding production visibility code. The focused test command initially could not restore because sandbox network access to NuGet was denied. After permitted restore access, it failed for the expected missing feature reasons:

- `VisibilitySystem` did not exist.
- `BoardState` did not accept wall geometry.

### GREEN

Implemented the smallest pure-state visibility and board geometry support needed by the tests. The focused test run passed all three visibility tests:

```text
Passed: 3, Failed: 0
```

## Verification

- `dotnet test tests\Wick.Tests --no-restore --logger "console;verbosity=normal"`: passed, 7 tests total.
- `dotnet build Wick.csproj --no-restore`: passed with 0 warnings and 0 errors.
- `git diff --check`: passed with no whitespace errors.
- Godot 4.7 Mono headless editor load completed project filesystem scan and script-class registration with exit code 0. The environment reported unrelated root-certificate and editor-settings write errors.

## Readability Review

The board scene uses a non-smoothed fixed camera at `(608, 416)`, the center of the configured `1216 x 832` viewport. With rendering intentionally limited to presentation states and no tile size or visual tile content established yet, a full visual inspection of the 19 x 13 room, wick shrink animation, and dim remembered geometry is not possible in this harness. The scene and script references were accepted by the headless Godot editor.

## Self-Review

No correctness, security, performance, or maintainability findings in the Task 3 changes. The line trace is symmetric for a center-to-center path, excludes the source from wall blocking, and includes a blocking target wall as visible. Visibility results copy their collections so later recomputes cannot mutate prior caller results. The remaining risk is visual-only: real tile geometry and rendering are intentionally deferred, so the camera fit and remembered-tile dimness need an in-editor check once tile rendering is introduced.
