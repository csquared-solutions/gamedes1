# Task 5 Report: Crawler, Moth, and Smother Loss Behavior

## Status

DONE_WITH_CONCERNS

## Delivered

- Added pure, xUnit-testable hazard behavior in `scripts/hazards`.
- `HazardContext` supplies the board, current visibility, current lamp cell, and wick radius without requiring Godot node construction.
- Crawler behavior now moves every dark turn, moves every other lit turn, and stalls once before its first dark-to-lit transition.
- Moth behavior remains dormant below radius 5, moves toward the lamp at radius 5 or greater, and stalls once before its first dark-to-lit transition.
- Deterministic breadth-first pathing uses the fixed neighbor order up, down, left, right.
- Added Crawler and Moth marker scenes and registered them with `GameRoot`.
- `LevelManager` initializes hazards for each level and advances them after successful player movement, using the current lamp cell so hazards retarget after lamp placement or movement.
- `HazardSystem` emits `GameEvents.LightOut` with the exact cause `"smothered"` when either hazard reaches the lamp. Existing wick depletion remains `"depleted"` in `WickSystem`.

## Tests And Verification

- Added `HazardSystemTests` covering crawler dark speed, lit cadence, first-lit-entry stall, moth radius threshold, moth first-lit-entry stall, moved-lamp retargeting, deterministic equal-cost choices, and reaching the lamp.
- The initial test run was verified red after restoring NuGet access: hazard types and namespace were absent.
- The moth warning-stall test was mutation-checked by removing the warning branch; it failed as expected, then passed after restoration.
- Focused command passed: `dotnet test tests/Wick.Tests --filter "FullyQualifiedName~HazardSystemTests"` (9 passed).
- Full command passed: `dotnet test tests/Wick.Tests` (26 passed).
- `git diff --check` completed without whitespace errors.

## Self-Review

- Confirmed all hazard decision logic is independent of direct Godot node construction.
- Confirmed first-entry stalls consume the transition and do not repeat while the hazard remains outside the lit area.
- Confirmed smothering uses the shared `LightOut` event with a distinct cause and only emits once per level load.
- Confirmed runtime hazards use the live lamp cell and live wick radius on every player turn.

## Concern

The environment does not expose a Godot executable, so the requested interactive manual play-room check could not be performed here. Compiled runtime wiring and scene registration are covered by the full xUnit suite, but visual gameplay behavior still merits an editor play-through.
