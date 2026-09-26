# Wick Design Spec

**Date:** 2026-08-16

## Project Summary

Wick is a single-screen, turn-based, top-down survival puzzle game built in Godot 4 with C#. The player must cross a maintenance sublevel while carrying a lamp whose wick is simultaneously the turn budget, the sight radius, and the pressure lever that changes hazard behavior.

The prototype target is a seven-level vertical slice that preserves the GDD's core identity:

- One screen per level on a fixed `19 x 13` grid
- Keyboard-only input
- Deterministic turn advancement
- No combat, inventory, scrolling camera, or persistent progression
- Two hazard types with opposing light rules
- A placeable lamp that can detach from the player
- Accessibility features implemented as shipped features, not future intentions

## Build Goals

The playable prototype must prove five things:

1. The wick-to-visibility relationship reads clearly at a glance.
2. Setting down the lamp creates meaningful route decisions rather than confusion.
3. Hazard behavior feels deterministic and fair under low-visibility pressure.
4. Seven authored levels are enough to teach and escalate the full ruleset.
5. Accessibility aids improve readability without removing the game's intended tension.

## Fixed Constraints

- Engine: Godot 4.x with C#
- Presentation: 2D top-down
- Camera: fixed, single-screen, no scrolling, no zoom during play
- Simulation: turn-based only; nothing moves unless the player acts
- Level size: `19 x 13`
- Prototype scope: exactly seven authored levels
- Failure states:
  - wick depletes to zero
  - hazard reaches and smothers the lamp
- Win state: player reaches the stair tile
- Audio scope: three gameplay cues plus one quiet looping ambience; moth wake, low wick, and light extinguished communicate state changes, while the ambience has no gameplay effect
- Menus: minimal start/restart flow only

## Core Systems

### Post-Playtest Revision

The first hands-on review found that sealed hazard pockets made the campaign too passive. Hazard starts remain authored and path length remains optimal, but equal-cost route choices now use the GDD's permitted seeded tie-break randomness. Every crawler, moth, flask, and stair must connect to the playable floor area, and late-game flask reviews intentionally include unsafe take-all routes so refuelling remains a decision rather than a collection task.

## 1. Turn System

Every player action advances the simulation by one turn. This includes:

- moving one tile
- placing or recovering the lamp when valid
- firing a flare
- waiting, if waiting is exposed as a debug or accessibility aid

The turn system must be the authoritative driver for:

- wick consumption
- hazard updates
- visibility recomputation
- loss and win checks

## 2. Wick System

Wick is the central game variable.

- Base cost: `1 wick` per action
- Flare cost: `3 wick`
- Oil flask reward: `+6 wick`
- Starting wick:
  - levels 1-3: `12`
  - levels 4-5: `14`
  - levels 6-7: `16`

Derived behavior:

- `sight_radius = floor(wick / 2)`
- at `wick == 0`, the level ends immediately with depletion as the cause

The wick value must drive:

- HUD display
- sight radius
- moth activation threshold
- perceived pressure curve

## 3. Visibility and Memory

Visibility is tile-based, not shader-first.

- visibility is evaluated from `Lamp.Position`, not `Player.Position`
- walls block line of sight
- visible tiles render fully
- previously seen static geometry remains in dim memory
- unseen tiles render as hidden

Implementation target:

- keep visibility state in simulation data
- render fog from that state so rules and presentation cannot drift apart

If strict Bresenham line tracing creates asymmetric edge cases, switch to a symmetric permissive approach rather than preserve incorrect fairness.

## 4. Lamp System

The lamp is a distinct world object and not a visual child of the player.

Required behaviors:

- when carried, the lamp follows the player tile
- the player can place it on the current floor tile
- if the player later steps back onto the lamp tile, it is recovered automatically or by contextual input
- visibility remains centered on the lamp even when the player walks away
- hazards target the lamp rather than the player whenever their rules call for the light source

The lamp system is the structural reason the project should avoid tightly coupling visibility or hazard targeting to the player controller.

## 5. Hazard Systems

Two hazards are required for the prototype.

### Crawler

- moves one tile per turn in darkness
- moves one tile every two turns inside the lit radius
- is intended to become more threatening as the wick shrinks

### Moth

- dormant unless `sight_radius >= 5`
- when active, moves toward the lamp
- ignores darkness and becomes more threatening when the player refuels

### Shared Hazard Rules

- deterministic movement and pathing
- first entry into a lit radius stalls for one turn
- smothering the lamp ends the level with a distinct loss cause
- hazards are never attacked, removed, or damaged

Tie-break randomness may be allowed only for equal-cost path choices and should remain limited, observable, and non-destructive to fairness.

## 6. Pickups and Special Actions

### Oil Flask

- triggered by stepping onto the flask tile
- locks input for two turns
- during the lift, the player cannot move
- the lamp is effectively unavailable and visibility drops to zero for the lift window
- the wick still burns during those turns
- on completion, the wick increases by `6`

### Flare

- optional player action
- costs `3 wick`
- doubles visibility radius for one turn only
- exists to trade long-term survival for short-term information

## 7. Level Structure

Level design should support teaching, routing, and commitment rather than decorative variety.

Shared rules:

- single screen
- fixed stair exit
- authored placement of hazards and flasks
- authored branches and dead ends
- at least some levels require placing the lamp to clear safely

Escalation targets from the GDD:

- levels 1-3: moth-free or low-moth onboarding
- level 3 onward: placing the lamp becomes required
- later levels increase both generosity and danger so extra wick does not read as a simple buff

## 8. UI and Accessibility

These are build requirements, not stretch goals:

- high readability wick gauge
- distinct silhouettes for crawler and moth
- hazard state indicators when visible
- tether line from player to placed lamp
- return-cost readout for recovering the lamp
- remembered geometry in low light
- visual pulse when moth threshold is crossed
- visual warning in final three wick
- optional practice mode:
  - wick frozen
  - one-step undo allowed

The main mode must preserve the intended tension. Practice mode is a teaching and accessibility tool, not the default ruleset.

## Godot Architecture

Recommended scene ownership:

- `GameRoot`
  - `LevelManager`
  - `TurnManager`
  - `WickSystem`
  - `VisibilitySystem`
  - `HazardSystem`
  - `Board`
  - `Player`
  - `Lamp`
  - `UI`

Recommended principles:

- keep board logic tile-driven and deterministic
- separate simulation state from visuals where possible
- use C# events or Godot signals for turn advancement and state changes
- keep level data external to the player and systems so authored tuning is easy

## Recommended Project Layout

```text
project.godot
scenes/
  game/GameRoot.tscn
  board/Board.tscn
  actors/Player.tscn
  actors/Lamp.tscn
  hazards/Crawler.tscn
  hazards/Moth.tscn
  pickups/OilFlask.tscn
  goals/Stair.tscn
  ui/Hud.tscn
  ui/WickGauge.tscn
  levels/Level01.tscn
  levels/Level02.tscn
  ...
scripts/
  core/TurnManager.cs
  core/WickSystem.cs
  core/VisibilitySystem.cs
  core/LevelManager.cs
  board/GridCoord.cs
  board/BoardState.cs
  board/TileCell.cs
  actors/PlayerController.cs
  actors/LampController.cs
  hazards/HazardBase.cs
  hazards/CrawlerController.cs
  hazards/MothController.cs
  pickups/OilFlaskController.cs
  ui/HudController.cs
  ui/LampTetherController.cs
data/
  levels/
  tuning/
assets/
  art/
  audio/
  fonts/
docs/
```

## Asset Direction

The prototype should begin as a graybox:

- flat-color tiles
- geometric hazard silhouettes
- simple icons for wick, flask, stair, and flare

Asset adoption rules:

- prefer `CC0` or similarly permissive assets
- record every external asset in an attribution sheet before shipping
- do not mix incompatible licenses into the same prototype
- choose readability over detail, especially for darkness and silhouettes

Free asset sources suitable for later replacement:

- Kenney
- OpenGameArt
- itch.io packs with clear permissive licenses

## Technical Risks

1. Visibility fairness
   - risk: asymmetric line-of-sight edge cases
   - mitigation: validate on a test board early and replace the tracing method if needed

2. Lamp trivialization
   - risk: placeable lamp bypasses intended pressure
   - mitigation: solve with authored layouts and return-cost pressure before changing core rules

3. Moth/flask deadlocks
   - risk: some authored positions become fatal whether the player takes a flask or not
   - mitigation: run intentional solvability checks on every level

4. Engine unfamiliarity
   - risk: delays caused by Godot/C# workflow setup
   - mitigation: pin one Godot 4.x version at project start and compile small increments immediately

## Milestones

### Milestone 1: Core Readability Prototype

- grid
- turn loop
- wick gauge
- shrinking visibility
- fixed camera
- one test room

### Milestone 2: Full Verb Prototype

- lamp placement/recovery
- crawler
- moth
- oil flask
- flare
- both loss causes

### Milestone 3: Content Prototype

- seven levels
- full level transitions
- tuned hazard and wick values
- restart flow

### Milestone 4: Shipping Pass

- accessibility layer
- placeholder art pass
- audio cues
- attribution sheet
- bug fixing

## Success Criteria

The prototype is successful when:

- players understand that movement spends sight within the first room
- lamp placement becomes intentional rather than forgotten
- deaths feel attributable to decisions rather than ambiguity
- the seven levels escalate without requiring new resource systems
- the game is playable and readable with placeholder art
