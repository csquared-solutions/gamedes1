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

## Fix Round 1

### Changes and Root Causes

1. **Practice isolation:** `SetPracticeMode` previously toggled wick freezing on the live campaign objects, so free movement, hazards, collected flasks, and restored wick survived when practice was disabled. `CampaignSessionState` now owns separate main/practice progression. Entering or leaving practice emits a fresh-level transition and `LevelManager` reconstructs the current level. `WickState.StartFreshLevel` resets wick/flare/freeze state, so a practice flask gain from 12 to 18 returns to the authored 12 in campaign mode.
2. **Level 1 teaching:** The original open layout had no reachable degree-one branch, and the teaching proof did not exercise placement. Level 1 now has an actual reachable dead-end at the fork. Separate finite winning routes prove the flare/crawler-stall/dark-lift lesson and a late lamp-placement corridor lesson. Keeping these as separate teaching routes is intentional: forcing flare, stall, two lifts, and late placement into one exact-economy script made the tutorial fatal rather than instructive.
3. **Route-choice proof:** The first simulation bounded cycles by following flasks only in array order. The revised bounded algorithm enumerates every flask subset and every permutation within that subset, while exploring every shortest-path move toward each selected waypoint. Each plan retains real wick, hazard, flare, placement, automatic pickup, and target-band rules. The per-plan action bound is `starting wick + selected flasks * 6 - target minimum`; cyclic detours remain excluded, preventing the level-4 frontier blow-up.
4. **Individual flask review:** The scenario review now records literal win counts for every subset bitmask and visit order, rather than inferring individual choices from all-versus-none runs.
5. **Outcome-based lamp necessity:** `StairEntryRule` is consumed by both production and simulation. Levels 3-7 reject stair entry while the lamp is carried. Simulations with placement actions disabled now fail under this real rule across every flask subset/order; success is no longer rejected only by `RequiresLampPlacement` goal metadata.
6. **Terminal flask lift:** A synchronous smother/depletion callback can now stop a lift immediately. No later spend or `+6` restoration occurs, and `OilFlaskInputPolicy` prevents the controller from unlocking input while terminal. Practice terminal state remains locked while the pre-lock `Z`, `P`, and `R` controls remain available.
7. **Restart semantics:** `R` now asks `CampaignSessionState` for a current-level fresh-copy transition. Campaign progress is preserved. A level-1 new run occurs only through `RestartRun` or an `R` request after main campaign completion; practice completion restarts its active practice level rather than resetting the main run. Main-mode loss also reloads the current level instead of level 1.
8. **Runtime contracts:** Source substring checks were replaced by `GodotTextSceneContract`, `CampaignSceneManifest`, and `CampaignRuntimeContract`. The production-consumed manifest supplies all campaign paths; the parser resolves declared resources, nodes, scripts, properties, indices, and root structure. A malformed parented level root is rejected by behavior, while all real level, GameRoot, and HUD scenes validate.

### RED Evidence

- Initial focused command: `dotnet test tests\Wick.Tests\Wick.Tests.csproj --no-restore --filter "FullyQualifiedName~CampaignProgressionTests|FullyQualifiedName~OilFlaskStateTests|FullyQualifiedName~LevelSolvabilityTests|FullyQualifiedName~CampaignSimulationTests|FullyQualifiedName~GameRootSceneTests" --blame-hang --blame-hang-timeout 30s --verbosity minimal`
  - Failed compilation with missing `CampaignSessionState`, `GodotTextSceneContract`, `CampaignRuntimeContract`, `CampaignSceneManifest`, `StairEntryRule`, terminal-aware flask overload/policy, `AllowLampPlacement`, and route-variant review APIs.
- First behavioral run of the same group completed without hanging but failed 11 tests. It exposed a coordinate-only outer simulation success check that bypassed the new stair rule; levels 3-7 were reported as winning without placement. Removing that duplicate check made every no-placement outcome fail under `StairEntryRule`.
- Combined Level 1 teaching experiment: `dotnet test ... --filter "FullyQualifiedName~LevelOne_HasAWinningTeachingRoute" --blame-hang --blame-hang-timeout 30s --verbosity minimal`
  - Failed after 1,367 bounded states (peak frontier 369; 348 depleted; 334 smothered), demonstrating that requiring every teaching verb in one route was incompatible with the exact economy/hazard lesson. Splitting the GDD beats into two observable winning routes resolved the design error without changing exact resource values.
- Individual subset diagnostic failed intentionally with the complete literal matrix: `dotnet test ... --filter "FullyQualifiedName~Campaign_IndividualFlaskSubsets" --blame-hang --blame-hang-timeout 30s --verbosity minimal`
  - Completed in 10 seconds and reported all 128 reviewed subset/order plans; those values were then converted into regression expectations.
- Fresh transition contract: `dotnet test ... --filter "FullyQualifiedName~CampaignProgressionTests" --blame-hang --blame-hang-timeout 30s --verbosity minimal`
  - Failed compilation because transitions did not yet expose `LevelIndex`, `StartsFreshLevelCopy`, `StartsNewRun`, or `HandleRestartRequest`.
- Practice wick isolation: `dotnet test ... --filter "FullyQualifiedName~FreshCampaignCopyAfterPractice" --blame-hang --blame-hang-timeout 30s --verbosity minimal`
  - Failed compilation because `WickState.StartFreshLevel` did not exist.
- Malformed scene contract: `dotnet test ... --filter "FullyQualifiedName~CampaignSceneContract_Rejects" --blame-hang --blame-hang-timeout 30s --verbosity minimal`
  - Failed because no exception was thrown for a level root incorrectly parented under `.`.

### GREEN Evidence

- Level 1 teaching routes: `dotnet test ... --filter "FullyQualifiedName~LevelOne_" --blame-hang --blame-hang-timeout 30s --verbosity minimal`
  - Passed 3/3 in 220 ms.
- Practice/progression isolation: `dotnet test ... --filter "FullyQualifiedName~PracticeModeTests|FullyQualifiedName~CampaignProgressionTests" --blame-hang --blame-hang-timeout 30s --verbosity minimal`
  - Passed 10/10 in 122 ms.
- Parsed runtime contracts: `dotnet test ... --filter "FullyQualifiedName~CampaignSceneContract_Rejects|FullyQualifiedName~Campaign_RuntimeScenePathsResolve|FullyQualifiedName~GameRootSceneTests" --blame-hang --blame-hang-timeout 30s --verbosity minimal`
  - Passed 4/4 in 65 ms.
- All Fix Round 1 coverage: `dotnet test tests\Wick.Tests\Wick.Tests.csproj --no-restore --filter "FullyQualifiedName~CampaignSimulationTests|FullyQualifiedName~CampaignScenarioReviewTests|FullyQualifiedName~CampaignProgressionTests|FullyQualifiedName~PracticeModeTests|FullyQualifiedName~OilFlaskStateTests|FullyQualifiedName~LevelSolvabilityTests|FullyQualifiedName~GameRootSceneTests" --blame-hang --blame-hang-timeout 30s --verbosity minimal`
  - Passed 62/62 in 13 seconds.
- Full suite: `dotnet test tests\Wick.Tests\Wick.Tests.csproj --no-restore --blame-hang --blame-hang-timeout 30s --verbosity minimal`
  - Passed 95/95 in 13 seconds; the blame collector confirmed all tests finished normally.
- Build: `dotnet build Wick.csproj --no-restore --verbosity minimal`
  - Succeeded in 1.26 seconds with 0 warnings and 0 errors.
- `git diff --check`
  - Completed with no whitespace errors. Git emitted only the repository's existing LF-to-CRLF notices.

### Updated Campaign and GDD Scenario Review

The exact GDD values remain unchanged.

| Level | Wick | Flasks | Crawlers | Moths | Target | Route plans | Winning subset/order outcomes | No-placement outcome |
|---|---:|---:|---:|---:|---:|---:|---|---|
| 1 | 12 | 2 | 2 | 0 | 2-4 | 5 | `11`: 2/2 | Not required; late teaching route wins |
| 2 | 12 | 2 | 2 | 1 | 2-4 | 5 | `11`: 2/2 | Not required |
| 3 | 12 | 2 | 3 | 1 | 2-4 | 5 | `11`: 2/2 | 0 wins |
| 4 | 14 | 3 | 3 | 2 | 3-6 | 16 | `111`: 6/6 | 0 wins |
| 5 | 14 | 3 | 3 | 2 | 3-6 | 16 | `111`: 6/6 | 0 wins |
| 6 | 16 | 3 | 4 | 3 | 3-6 | 16 | `111`: 6/6 | 0 wins |
| 7 | 16 | 4 | 4 | 3 | 3-6 | 65 | `0001` 1/1; `0011` 1/2; `0101` 2/2; `0111` 1/6; `1001` 1/2; `1011` 1/6; `1101` 3/6; `1111` 1/24 | 0 wins |

Bit `1` means that authored flask is taken; each denominator is the number of visit-order permutations reviewed for that subset. Every omitted and included individual flask case is represented. Levels 1-6 still require their full flask sets in the bounded winning outcomes, but they now prove multiple visit orders rather than one array-ordered script. Level 7 supports several individual/mixed choices and demonstrates order-sensitive outcomes.

The earlier take-all/skip-all/early/late matrix remains green: take-all finishes at wick `2, 2, 3, 3, 3, 5, 3`; skip-all loses all seven; early placement loses all seven; late placement wins levels 3-7 at the same wick values. Level 1 additionally has a placement-only teaching route after action 12; combining it with the separate flare lesson is intentionally not required.

### Fix Round 1 Self-Review

- Confirmed exact starting wick, flask, crawler, moth, target-band, grid-size, and `+6` restoration tests are unchanged and green.
- Confirmed main and practice progression are separate, every mode switch reloads all gameplay collaborators, and the fresh-wick state test catches practice restoration leakage.
- Confirmed levels 3-7 use the same production stair rule in runtime and simulation, and exhaustive no-placement reviews have zero wins.
- Confirmed the search remains finite: at most 65 waypoint plans for level 7, each bounded by selected-flask economy and 20,000 states; no cyclic free movement was restored.
- Confirmed flask terminal callbacks stop the lift and keep input locked before any later restoration.
- Confirmed `R` no longer calls whole-run restart during active campaign play.
- Confirmed no scene/HUD test reads raw source text to assert substrings; all use parsed runtime contracts.
- Reviewed all changed files and found no unrelated refactoring or new scope.

### Fix Round 1 Concerns

- Godot is still unavailable on PATH, so live engine startup and human visual/playtest validation remain external follow-up. The parser validates this project's consumed serialization contract but is not a replacement for Godot's complete scene loader.
- Levels 1-6 have route-order choice but still require every authored flask in the bounded winning scenarios. This is now measured content behavior rather than a test limitation; human playtesting should confirm that timing/path decisions provide sufficient agency.
- The finite proof covers all flask subsets, all visit permutations, all shortest-path alternatives within each segment, and all bounded lamp timings. It intentionally excludes arbitrary cyclic detours that caused the original nontermination and cannot improve the finite wick budget.

## Fix Round 2

### Findings and Corrections

1. **Observable Level 1 corridor teaching route:** The previous test proved only a late placement action. `LevelTopology.FindTerminalCorridor` now derives a terminal corridor exclusively from floor connectivity: the stair must have degree 1, corridor cells degree 2, and the commitment point degree 3 or greater. Seven Level 1 wall cells create this topology without changing starts, flasks, hazards, stair, movement distance, or wick economy. The finite simulation now returns an action-by-action trace containing player cell and lamp ownership/cell. The winning teaching test structurally discovers commitment `(11,9)` and corridor cells `(12,9)`, `(13,9)`, `(14,9)`, then proves action 13 places the lamp at commitment, all subsequent moves traverse that corridor with the lamp left at `(11,9)`, and the final move reaches the stair.
2. **Runtime-consumed HUD presentation:** Accessibility copy was removed from `CampaignRuntimeContract.ValidateHud`; the parsed scene contract now validates only required HUD nodes/types. `HudPresentationState` is a pure presentation boundary consumed directly by `HudController._Process`. Behavior tests supply campaign/practice/completion/undo state and assert the visible campaign status, practice/frozen state, undo readiness, and keyboard controls produced by that same runtime code.

### TDD RED Evidence

- Initial focused command:
  - `dotnet test tests\Wick.Tests\Wick.Tests.csproj --no-restore --filter "FullyQualifiedName~LevelOne_HasAWinningLateLampPlacementTeachingRoute|FullyQualifiedName~HudPresentationStateTests" --blame-hang --blame-hang-timeout 30s --verbosity minimal`
  - Failed compilation because `LevelTopology`, `SimulationResult.Trace`, `HudPresentationState`, and `HudPresentationInput` did not exist.
- After adding the pure boundaries and runtime HUD wiring, the same command completed in 41 ms with 4 HUD tests passing and the corridor test failing at `Assert.NotNull`: the current Level 1 stair had no structurally valid terminal corridor. This isolated the remaining content defect before geometry changed.

### GREEN and Verification Evidence

- Focused RED-to-GREEN command:
  - `dotnet test tests\Wick.Tests\Wick.Tests.csproj --no-restore --filter "FullyQualifiedName~LevelOne_HasAWinningLateLampPlacementTeachingRoute|FullyQualifiedName~HudPresentationStateTests" --blame-hang --blame-hang-timeout 30s --verbosity minimal`
  - Passed 5/5 in 142 ms.
- Covering finite command:
  - `dotnet test tests\Wick.Tests\Wick.Tests.csproj --no-restore --filter "FullyQualifiedName~CampaignSimulationTests|FullyQualifiedName~HudPresentationStateTests|FullyQualifiedName~GameRootSceneTests" --blame-hang --blame-hang-timeout 30s --verbosity minimal`
  - Passed 29/29 in 10 seconds; all tests finished normally under the hang collector.
- Full suite:
  - `dotnet test tests\Wick.Tests\Wick.Tests.csproj --no-restore --blame-hang --blame-hang-timeout 30s --verbosity minimal`
  - Passed 99/99 in 11 seconds; all tests finished normally.
- Build:
  - `dotnet build Wick.csproj --no-restore --verbosity minimal`
  - Succeeded in 1.13 seconds with 0 warnings and 0 errors.
- `git diff --check` completed with no whitespace errors; Git emitted only LF-to-CRLF notices.

### Updated Level 1 Teaching and GDD Check

| Check | Result |
|---|---|
| Exact Level 1 values | 12 starting wick, 2 flasks, 2 crawlers, 0 moths, target 2-4; unchanged |
| Route economy | Existing 15-move flask route preserved; placement adds one explicit action and remains within target band |
| Fork/dead-end | Existing reachable degree-one teaching branch remains |
| Corridor commitment | Structurally derived degree-3 cell `(11,9)` |
| Corridor | Degree-2 `(12,9)`, `(13,9)` leading to degree-1 stair `(14,9)` |
| Placement behavior | Lamp placed at `(11,9)` on action 13 |
| Post-placement behavior | Player traverses every corridor cell; lamp remains placed at `(11,9)` |
| Completion | Trace ends at the authored stair with a winning target-band outcome |
| Finite bound | Existing selected-flask wick/action and state bounds retained; no cyclic search restored |

### HUD Presentation Behavior

| Input state | Runtime-generated visible output |
|---|---|
| Campaign level 4, last clear 5 | `LEVEL 4/7 | LAST CLEAR: 5 WICK`; `CAMPAIGN MODE | P PRACTICE` |
| Practice level 2, no undo | `LEVEL 2/7`; `PRACTICE MODE | WICK FROZEN | Z UNDO EMPTY` |
| Practice level 2, undo ready | `LEVEL 2/7`; `PRACTICE MODE | WICK FROZEN | Z UNDO READY` |
| Completed level 7, wick 3 | `RUN COMPLETE | LEVEL 7 CLEAR: 3 WICK` |
| Controls in all states | `P PRACTICE | Z UNDO | E SET LAMP | F FLARE | R RESTART` |

### Files Changed

- Added `scripts/data/LevelTopology.cs` for structural terminal-corridor discovery.
- Added `scripts/ui/HudPresentationState.cs` and runtime consumption in `HudController.cs`.
- Extended the finite test simulation trace in `CampaignSimulationHarness.cs`.
- Tightened Level 1 corridor geometry in `LevelLoader.cs` without changing exact economy/counts.
- Removed accessibility-copy validation from `CampaignSceneManifest.cs`; scene validation remains structural.
- Added `HudPresentationStateTests.cs` and strengthened the Level 1 teaching route test.

### Fix Round 2 Self-Review and Concerns

- Confirmed the corridor assertion is based on board connectivity, not a teaching flag or authored corridor metadata.
- Confirmed the trace records actual simulated action outcomes and lamp snapshots; the test does not infer behavior from route text.
- Confirmed `HudController` calls `HudPresentationState.Create` and assigns all three returned strings to visible labels every frame.
- Confirmed neither `GameRootSceneTests` nor `CampaignRuntimeContract.ValidateHud` asserts `P PRACTICE` or `Z UNDO` literals.
- Confirmed all seven-level counts, `+6` flask restoration, target bands, finite route variants, and runtime scene participation remain unchanged.
- Live Godot startup and visual/human validation remain unavailable in this harness; the pure runtime boundary and parsed node contract do not replace an engine launch.
