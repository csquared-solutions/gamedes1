# Random Stair Placement Design

## Status

Approved on 2026-08-17.

## Context

Wick currently gives each of its seven authored campaign levels one fixed stair cell. The player has requested that the stair move between attempts to add route variety. This amendment replaces the fixed exit with a small authored pool while retaining the campaign's existing geometry, hazard rules, flask economy, lamp-placement requirement, and wick targets.

The change intentionally does not select from every open floor tile. Unrestricted placement could create trivial exits, unreachable exits, unintended overlaps, or routes that invalidate the authored resource balance.

## Goals

- Give every level exactly three authored and validated stair candidates.
- Select a stair position when each level attempt begins.
- Guarantee that a retry uses a different stair cell from the immediately previous attempt on that level.
- Preserve the seven-level authored progression and all existing game rules.
- Keep stair visibility, entry, lamp-placement, HUD, restart, and progression behavior synchronized with the selected cell.

## Non-Goals

- Procedurally generating level geometry.
- Randomizing the player, lamp, flask, crawler, or moth starts.
- Changing wick values, flask restoration, hazard movement, or level-clear rules.
- Moving the stair during an active attempt.
- Rerolling the stair when Practice Mode Undo restores an action.

## Authored Level Data

`LevelDefinition` will expose `StairCandidates`, containing exactly three distinct cells. The existing authored stair cell remains one candidate so the original route is retained as one possible attempt.

Each candidate must:

- be a walkable floor cell connected to the player's playable area;
- differ from the player and lamp start;
- not overlap a flask, crawler, moth, or another stair candidate;
- preserve a winning route within the level's wick completion band;
- preserve required lamp-placement behavior on levels three through seven;
- remain viable for both supported crawler tie-break seeds.

The authored definition remains immutable. A helper creates an active level definition with the selected `StairCell` while retaining the complete candidate pool and all other authored values.

## Selection Rules

A focused `StairPlacementSelector` owns stair selection. It receives a random source so deterministic seeds can be used in tests and normal randomness can be used during play.

For each level, the selector remembers the stair used by the previous attempt. When another attempt begins, it selects uniformly from the other two candidates. Therefore a death, automatic retry, or manual `R` restart always visibly moves the stair rather than merely having a chance to move it.

The selector runs when:

- a campaign level is entered for the first time;
- a death presentation completes and the level restarts;
- the player manually restarts with `R`;
- a level is loaded as a new Practice Mode attempt;
- a new campaign run loads a level again.

The selector does not run for `Z` Undo because Undo remains inside the same attempt. The active stair does not need to be added to action snapshots because it cannot change until a full level reload.

## Runtime Flow

`LevelManager.LoadActiveLevel` will load the authored definition, request a candidate from `StairPlacementSelector`, and create the active definition before initializing gameplay systems. The existing systems then continue to consume `_currentLevel.StairCell`:

1. `StairController` places and reveals the stair at the active cell.
2. `StairEntryRule` checks the same active cell and the existing carried-lamp restriction.
3. HUD exit-blocked messaging evaluates the same active definition.
4. Campaign completion records the existing remaining-wick result.
5. Restart creates a new active definition with a different selected cell.

No visual teleport occurs during play. A newly selected stair first appears only when the retried level is loaded and the cell enters sight, preserving the existing darkness and discovery rules.

## Fairness And Failure Handling

Candidate validation is a build-time responsibility. Invalid candidate data fails automated tests rather than silently falling back to arbitrary floor cells at runtime.

The selector rejects a level with fewer than two usable candidates because it could not guarantee movement on retry. Campaign data has a stronger contract of exactly three candidates. Duplicate candidates are invalid. Selection never loops while searching for a different value; it filters out the previous cell and chooses directly from the remaining candidates.

## Testing

Unit tests will prove that the selector:

- chooses only declared candidates;
- never repeats the immediately previous stair on a retry;
- remains deterministic when supplied a seeded random source;
- does not mutate the authored level definition.

Campaign-data tests will validate all 21 candidate cells for topology, reachability, and object overlap. Hazard-aware campaign simulations will solve every candidate with both crawler tie-break seeds and verify the existing completion-wick bands and lamp-placement requirements.

Runtime contract tests will verify that loading and restarting use the selected active cell, while Undo retains the current cell. Existing stair visibility, HUD, campaign progression, packaging, and seven-level tests remain in the regression suite.

## GDD Alignment Amendment

This approved change replaces the previous `fixed stair exit` requirement with:

> Each of the seven authored levels provides three authored, validated stair exits. One exit is selected per attempt, and a retry must select a different exit from the previous attempt. Stair position remains fixed during the attempt and is not changed by Undo.

This is a controlled post-playtest expansion. It preserves authored level design and deterministic player-facing rules while adding replay variety requested during hands-on review.
