# Task 7 Report: Seven Levels, Practice Mode, and Accessibility Completion

## Implementation Summary

Task 7 expands the prototype into a deterministic seven-level campaign backed by seven runtime-loaded `Level01.tscn` through `Level07.tscn` scenes and authoritative C# level definitions. Campaign progression reports remaining wick at each stair, advances through all seven levels, enters a defined completion state after level 7, and restarts a new run from level 1 with no saved progress.

The implementation also adds keyboard-only practice mode (`P`) and practice-only one-step undo (`Z`). Practice freezes wick decreases while turns and hazards continue. Undo restores the complete prior successful-action snapshot: player, lamp ownership/cell, wick/flare/freeze state, turn state, crawler and moth state, smother guard, flask collection/lift state, visibility, terminal state, campaign progression, and remaining-wick report. Main campaign behavior remains unchanged when practice is off.

Hazards and flasks now initialize from each authored level's arrays rather than fixed positions. Flask pickup and placed-lamp recovery occur automatically on movement without a second wick spend; lamp set-down remains an explicit action. Both depletion and smothering continue through the shared `LightOut` event with distinct cause strings.

## TDD: RED and GREEN

Focused tests were written before the corresponding production behavior.

### RED evidence

- Campaign/practice/hazard focused compile: `dotnet test tests\Wick.Tests\Wick.Tests.csproj --no-restore --filter "FullyQualifiedName~LevelSolvabilityTests|FullyQualifiedName~PracticeModeTests|FullyQualifiedName~CampaignProgressionTests|FullyQualifiedName~HazardSimulation|FullyQualifiedName~FlaskCollection" --verbosity minimal`
  - Failed to compile because `CampaignProgressState`, `PracticeModeState`, `GameplaySnapshot`, `HazardSimulationState`, campaign arrays, campaign loading, and wick-freeze APIs did not exist.
- Runtime scene path test: `dotnet test tests\Wick.Tests\Wick.Tests.csproj --no-restore --filter "FullyQualifiedName~Campaign_RuntimeScenePathsResolve" --verbosity minimal`
  - Failed because `res://scenes/levels/Level01.tscn` did not exist.
- HUD accessibility test: `dotnet test tests\Wick.Tests\Wick.Tests.csproj --no-restore --filter "FullyQualifiedName~Hud_ContainsVisiblePractice" --verbosity minimal`
  - Failed because the campaign/practice status controls were absent.
- Campaign simulation test: `dotnet test tests\Wick.Tests\Wick.Tests.csproj --no-restore --filter "FullyQualifiedName~CampaignSimulationTests" --verbosity minimal`
  - Failed because the campaign simulation harness and requirements did not exist.
- Radius-zero flask test: `dotnet test tests\Wick.Tests\Wick.Tests.csproj --no-restore --filter "FullyQualifiedName~FlaskCollection_ReportsRadiusZero" --verbosity minimal`
  - Failed at compile time because the collection event did not report a radius override.
- Level-4 termination regression: `dotnet test tests\Wick.Tests\Wick.Tests.csproj --no-restore --filter "FullyQualifiedName~LevelFour_SearchTerminates" --verbosity minimal`
  - Failed after approximately 22 seconds at the 20,000-state cap: peak frontier 19,171, placement branches 2,982, duplicate states 21,396, visited states 39,169, finite action bound 31.
  - Restricting placement to action 1 still took approximately 18 seconds with peak frontier 10,095, proving placement timing was not the root cause.

### Root cause and bounded correction

The level-4 blow-up was caused by free-form movement cycles. Hazard cadence made otherwise similar player positions distinct, so cyclic routes produced a rapidly expanding state frontier. It was not caused by practice/undo state, and constraining lamp timing did not resolve it.

The corrected simulation uses the finite action bound derived from authored economy (`starting wick + all flask restoration - 1`) and expands only moves that reduce the board-computed shortest-path distance to the next fixed authored flask/stair waypoint. Hazard turns, flask effects, flare, lamp placement, target wick bands, and success/failure branches remain simulated. This preserves the required GDD solvability proof while eliminating irrelevant cyclic exploration. The complete level-4 search reduced to 212 visited states with a peak frontier of 27, and the dedicated bounded regression subsequently completed in about 0.1 seconds.

### GREEN evidence

- Focused bounded proof: `dotnet test tests\Wick.Tests\Wick.Tests.csproj --no-restore --filter "FullyQualifiedName~CampaignSimulationTests|FullyQualifiedName~CampaignScenarioReviewTests" --blame-hang --blame-hang-timeout 30s --verbosity minimal`
  - Passed 10/10 in 1 second. The blame collector reported that all tests finished normally.
- Full suite: `dotnet test tests\Wick.Tests\Wick.Tests.csproj --no-restore --blame-hang --blame-hang-timeout 30s --verbosity minimal`
  - Passed 73/73 in 1 second. The blame collector reported that all tests finished normally.
- Production build: `dotnet build Wick.csproj --no-restore --verbosity minimal`
  - Succeeded with 0 warnings and 0 errors in 1.13 seconds.

## Campaign Content and Playtest Review

| Level | Start wick | Flasks | Crawlers | Moths | Lamp required | Target wick | Simulated finish |
|---|---:|---:|---:|---:|---|---:|---:|
| 1 | 12 | 2 | 2 | 0 | No (teaches) | 2-4 | 2 |
| 2 | 12 | 2 | 2 | 1 | No | 2-4 | 2 |
| 3 | 12 | 2 | 3 | 1 | Yes | 2-4 | 3 |
| 4 | 14 | 3 | 3 | 2 | Yes | 3-6 | 3 |
| 5 | 14 | 3 | 3 | 2 | Yes | 3-6 | 3 |
| 6 | 16 | 3 | 4 | 3 | Yes | 3-6 | 5 |
| 7 | 16 | 4 | 4 | 3 | Yes | 3-6 | 3 |

### Round 1: visibility legibility

Deterministic visibility review confirms the initial radius is 6, radius changes immediately follow wick changes, remembered geometry remains distinguishable, hazard indicators are visibility-aware, and tether/return-cost/threshold/final-three feedback remains always enabled in both modes. Level 1's winning teaching route uses flare, observes the crawler's first-entry stall, and includes at least two forced-dark turns during a radius-zero flask lift. Live visual legibility still requires the planned Godot/human validation.

### Round 2: verb comprehension

State-level scenarios verify one action spends one wick, automatic flask pickup, automatic lamp recovery on entering its tile without a second spend, explicit lamp set-down, and practice-only undo. The HUD visibly identifies practice mode, frozen wick, `P`, `Z`, and whether undo is ready. Level 1 contains the fork/dead-end and final corridor teaching structure; its successful authored route demonstrates flare, crawler stall, and flask lift before the exit.

### Round 3: seven-level pacing and fairness

Hazard-aware simulations find a winning authored route in the target completion band for every level, with lamp placement observed for levels 3-7. The campaign scenario matrix was reviewed as follows:

| Level | Take authored flasks | Skip all flasks | Place early | Place late |
|---|---|---|---|---|
| 1 | Win, wick 2 | Loss | Loss | Loss |
| 2 | Win, wick 2 | Loss | Loss | Loss |
| 3 | Win, wick 3 | Loss | Loss | Win, wick 3 |
| 4 | Win, wick 3 | Loss | Loss | Win, wick 3 |
| 5 | Win, wick 3 | Loss | Loss | Win, wick 3 |
| 6 | Win, wick 5 | Loss | Loss | Win, wick 5 |
| 7 | Win, wick 3 | Loss | Loss | Win, wick 3 |

This confirms late lamp placement is required by the authored solutions from level 3 onward. It also shows that all authored flasks are route-critical in the reviewed routes; human testing should check whether individual flask decisions feel meaningfully optional rather than compulsory.

## GDD Alignment Checklist

- [x] Exact starting wick array: 12, 12, 12, 14, 14, 16, 16.
- [x] Exact flask array: 2, 2, 2, 3, 3, 3, 4; fixed placement, single consumption, no carry/bank inventory.
- [x] Exact crawler array: 2, 2, 3, 3, 3, 4, 4.
- [x] Exact moth array: 0, 1, 1, 2, 2, 3, 3.
- [x] Lamp placement required to clear levels 3-7.
- [x] Completion targets: 2-4 for levels 1-3 and 3-6 for levels 4-7.
- [x] Fixed 19x13 single-screen topology and deterministic pathing.
- [x] Level 1 teaches shrinking visibility, flare at a meaningful route decision, crawler first-entry stall, two dark flask-lift turns, and the lamp-placement corridor tactic without requiring it to clear level 1.
- [x] Automatic lamp recovery and flask pickup on entry; explicit lamp set-down; no movement double-spend.
- [x] Practice freezes wick, remains optional/non-shaming, visibly identifies itself, and exclusively gates one-step undo without gating campaign content.
- [x] Seven-level progression reports remaining wick, completes after level 7, and restarts a new run at level 1 without saved progress.
- [x] Depletion and smothering use the same light-out handler with distinct reported causes.
- [x] Visible hazard indicators, tether, return cost, threshold pulse, and final-three flicker remain always on; crawler/moth rendering does not depend on color alone.

GDD contradiction ruling: the exact starting radius is 6 while the moth threshold is 5, so levels 1-3 cannot literally begin below the threshold. The implementation preserves both exact values and interprets the onboarding statement as level 1 moth-free and levels 2-3 low-moth, not threshold-inactive.

The GDD's rough 30-45-step intent cannot mean 30-45 wick-consuming turns for levels 1-3 under the exact economy: 12 starting wick plus two four-wick flasks provides fewer than 30 paid actions before the required positive completion band and any flare cost. The simulations therefore treat 30-45 as an exploration/attempt envelope and enforce an upper bound of 45 successful actions.

## Files Changed

- Seven authored runtime scenes under `scenes/levels/Level01.tscn` through `Level07.tscn`, plus HUD content.
- Campaign data/runtime: `LevelDefinition`, `LevelLoader`, `CampaignLevelScene`, `CampaignProgressState`, `LevelManager`, and complete gameplay snapshots.
- Practice/accessibility: `PracticeModeState`, player input, HUD, wick freeze, visibility restore, and board redraw restore.
- Runtime hazards/pickups: dynamic authored crawler/moth/flask arrays, deterministic hazard snapshots, one-time smother state, and automatic radius-zero flask collection.
- Tests: campaign content/topology, progression, solvability and bounded search, scenario review, practice/undo, lamp recovery, hazard integration, flask behavior, runtime scene paths, and HUD controls.

## Self-Review Findings

- Reviewed every authored scene and confirmed each is addressed by `LevelLoader.GetScenePath` and instantiated by the runtime campaign loader rather than existing as a disconnected placeholder.
- Confirmed exact campaign counts, target bands, topology validity, hazard/flask initialization, escalation, practice freeze, complete one-step undo, automatic recovery, progression, completion, and one-time smother behavior have observable state-level coverage.
- Confirmed `git diff --check` reports no whitespace errors and no obsolete singular flask-cell source remains.
- Confirmed the bounded campaign tests model actual authored data and observable win/loss outcomes rather than source-text detectors.
- No additional code change was identified during the final review.

## Concerns and Environment Limitations

- The Godot executable is not available on PATH in this harness, so serialized scene/runtime startup and live visual/human validation could not be performed. Pure C# state tests are used because direct Godot Node construction is unsafe in the plain xUnit host.
- The internal playtest evidence is deterministic simulation plus content review, as permitted by the task ruling. Human launch validation is still needed for subjective visibility legibility, discovery, and pacing.
- The reviewed routes require taking all authored flasks and lose when all are skipped. This satisfies fixed placement and meaningful resource pressure, but human testing should determine whether the intended flask choices feel sufficiently optional.
- Distinct final hazard silhouettes remain a Task 8 art deliverable; Task 7 preserves shape/indicator information and does not rely on color alone.
