# Task 2 Report: Grid, Turn Loop, and Wick Core

## Delivered

- Added `TurnManager` with a zero-based turn counter and `AdvanceTurn()`.
- Added `WickSystem` with current wick, floor-of-half sight radius, depletion state, spending, and restoration.
- Added `BoardState` bounds checks and the minimal `VisibilityState` / `BoardRenderer` scaffold.
- Added `TurnManager`, `WickSystem`, and `BoardRenderer` nodes to `GameRoot.tscn`.
- Added a standard net8.0 xUnit project at `tests/Wick.Tests` that directly references `Wick.csproj`.
- Excluded test source files from the Godot production project, which otherwise includes every C# source file recursively.

## TDD Evidence

### RED

The specified xUnit tests were written before the gameplay classes existed.

Command:

```powershell
dotnet test tests\Wick.Tests --filter "FullyQualifiedName~WickSystemTests|FullyQualifiedName~TurnManagerTests"
```

Observed result after package restore and test-project isolation: compilation failed with `CS0246` for the missing `WickSystem` and `TurnManager` types. This is the expected RED failure.

### GREEN Attempt

After the minimal implementations were added, the same command compiled both `Wick` and `Wick.Tests`, but the plain xUnit test host aborted with `System.AccessViolationException` at `Godot.Node..ctor()`. A standard `dotnet test` process does not initialize Godot's native runtime; directly constructing the required `Node` subclasses therefore cannot execute in it.

The full current suite (`dotnet test tests\Wick.Tests`) reproduces the same deterministic host crash. Consequently, a passing xUnit GREEN run cannot be truthfully recorded under the brief's simultaneous requirements for a direct `Wick.csproj` reference, plain xUnit, and tests that instantiate `Node` subclasses.

## Additional Verification

- `dotnet build Wick.csproj --no-restore`: passed with 0 warnings and 0 errors.
- Headless Godot 4.7 editor load: passed. The engine completed project initialization and script-class registration for the modified scene and scripts.
- `git diff --check`: passed; no whitespace errors.

## Brief Reconciliation

The supplied `Spend_ReducesWick_AndSignalsDepletionAtZero` test expects `Spend(1)` to return `true` after consuming the final wick. The supplied implementation sample instead returns `!IsDepleted`, which would return `false` in that exact case. The implementation treats the boolean as whether the requested spend was accepted: a final affordable spend returns `true`, sets wick to zero, and exposes `IsDepleted == true`; an unaffordable or negative spend returns `false` without changing wick.

## Self-Review

No correctness, security, performance, or maintainability findings in the changed runtime code. Board bounds use the intended inclusive lower and exclusive upper limits, and the board renderer adds only the required visibility-state scaffold. The outstanding risk is test infrastructure: use a Godot-aware test runner such as GDUnit4Net with runtime-enabled tests, or extract testable pure state from the `Node` wrappers, in a follow-up task if passing automated tests are required.
