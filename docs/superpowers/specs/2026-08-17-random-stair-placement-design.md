# Right-Side Random Stair Placement Design

## Status

Revised design approved on 2026-08-17.

## Context

Wick has seven authored campaign levels and currently selects each attempt's stair from three validated cells. Playtesting showed that the retry movement is effective, but the small pools remain predictable. This revision broadens each pool to every validated floor cell in the rightmost third of the map while retaining the existing geometry, hazard rules, flask economy, lamp-placement requirement, and wick targets.

The board is 19 columns wide with zero-based coordinates. The rightmost third begins at column 12, expressed by the general rule `cell.X >= (level.Width * 2) / 3`. Column 18 is the permanent boundary wall, so generated playable candidates occupy columns 12 through 17.

## Goals

- Build each level's stair pool from safe, reachable floor cells in the rightmost third of its map.
- Select a stair position when each level attempt begins.
- Guarantee that a retry uses a different stair cell from the immediately previous attempt on that level.
- Preserve the original authored stair as an eligible location when it satisfies the right-side safety rules.
- Preserve the seven-level authored progression and all existing game rules.
- Keep stair visibility, entry, lamp-placement, HUD, restart, and progression behavior synchronized with the selected cell.

## Non-Goals

- Procedurally generating or changing level geometry.
- Randomizing the player, lamp, flask, crawler, or moth starts.
- Changing wick values, flask restoration, hazard movement, or level-clear rules.
- Placing stairs outside the rightmost third.
- Moving the stair during an active attempt.
- Rerolling the stair when Practice Mode Undo restores an action.

## Candidate Generation

A focused candidate builder derives `StairCandidates` from immutable authored level data. It enumerates the rightmost third in a stable order and includes a cell only when it:

- is inside the board and satisfies the right-third formula, producing playable candidates in columns 12 through 17;
- is a walkable floor cell connected to the player's playable area;
- differs from the player and lamp start;
- does not overlap a wall, flask, crawler, or moth; and
- is not in the level's authored `StairExclusions`.

Every cell that passes these structural rules belongs to the runtime pool. There is no three-candidate cap. `StairExclusions` may contain only right-side cells proven unsafe by the full wick, lamp, route, or hazard simulations; it is not used to impose a small arbitrary pool. Campaign construction rejects duplicate or out-of-region exclusions and rejects a level with fewer than two candidates because that level could not guarantee movement on retry.

The original fixed stair for each level remains the authored/default `StairCell` and must appear in the generated pool. This retains the original route as one possible attempt and provides a stable default for simulations that do not request random selection.

The authored definition remains immutable. `WithStairCell` creates an active level definition with the selected cell while defensively copying the complete candidate pool and all other authored arrays.

## Selection Rules

`StairPlacementSelector` owns runtime selection. It receives a random source so deterministic seeds can be used in tests and normal randomness can be used during play.

For each level, the selector remembers the stair used by the previous attempt. When another attempt begins, it selects uniformly from every candidate except that previous cell. A death, automatic retry, or manual `R` restart therefore always visibly moves the stair rather than merely having a chance to move it.

The selector runs when:

- a campaign level is entered for the first time;
- a death presentation completes and the level restarts;
- the player manually restarts with `R`;
- a level is loaded as a new Practice Mode attempt;
- a new campaign run loads a level again.

Same-level `Z` Undo restores gameplay in place and does not run the selector. When Undo crosses a completed-level boundary, `LevelManager` restores that level's cached active definition so the exact previous stair is retained.

## Runtime Flow

`LevelLoader` creates the immutable authored level and its complete right-side candidate pool. `LevelManager.LoadActiveLevel` asks `StairPlacementSelector` for an active definition before initializing gameplay systems. Existing systems continue to consume `_currentLevel.StairCell`:

1. `StairController` places and reveals the stair at the active cell.
2. `StairEntryRule` checks the same active cell and the existing carried-lamp restriction.
3. HUD exit-blocked messaging evaluates the same active definition.
4. Campaign completion records the existing remaining-wick result.
5. A genuine restart creates a new active definition with a different selected cell.

No visual teleport occurs during play. A newly selected stair first appears only when the retried level is loaded and the cell enters sight, preserving the existing darkness and discovery rules.

## Fairness And Failure Handling

Candidate generation is deterministic; randomization selects only from the generated result. Invalid campaign data fails construction or automated tests rather than silently falling back to arbitrary floor cells.

Duplicate candidates are invalid. Selection never loops while searching for a different value; it filters out the previous cell and chooses directly from the remaining pool.

The full safety suite remains the release gate. If a generated right-side cell fails route, wick, lamp, overlap, or supported hazard-seed validation, the level cannot ship until its geometry is corrected or that cell is explicitly documented in `StairExclusions`. Test performance may be improved through harness optimization or safe parallel execution, but coverage must not be reduced by sampling random candidates.

## Testing

Candidate-builder tests will prove that every generated cell:

- lies in the rightmost third;
- is a distinct, walkable, reachable floor cell;
- does not overlap starts, walls, flasks, crawlers, or moths;
- includes the original authored stair;
- honors only distinct, in-region authored exclusions; and
- is generated deterministically from the same level data.

Selector tests will prove that selection:

- chooses only generated candidates;
- never repeats the immediately previous stair on a retry;
- remains deterministic when supplied a seeded random source;
- works with candidate pools larger than three;
- does not mutate the authored level definition.

Campaign simulations will solve every generated candidate with both crawler tie-break seeds and verify the existing completion-wick bands and lamp-placement requirements. Runtime checks will verify that loading and restarting use the selected active cell, while same-level and cross-level Undo retain the current cell. Existing stair visibility, HUD, campaign progression, packaging, and seven-level tests remain in the regression suite.

## GDD Alignment Amendment

This approved playtest revision replaces the previous `three authored exits` amendment with:

> Each of the seven authored levels generates a pool from all validated, reachable stair locations in the rightmost third of the map. One location is selected per attempt, and a retry must select a different location from the previous attempt. The stair remains fixed during the attempt and is not changed by Undo.

This remains a controlled expansion of the original fixed-exit requirement. It adds greater route variety without changing authored layouts, resource rules, hazards, darkness, lamp handling, or campaign progression.
