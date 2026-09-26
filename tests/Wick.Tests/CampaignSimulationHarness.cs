using System;
using System.Collections.Generic;
using System.Linq;
using Wick.Actors;
using Wick.Board;
using Wick.Core;
using Wick.Data;
using Wick.Hazards;
using Wick.Pickups;

namespace Wick.Tests;

internal sealed record SimulationRequirements(
    bool RequireFlare = false,
    bool RequireCrawlerStall = false,
    int MinimumFlasksCollected = 0,
    bool SkipAllFlasks = false,
    bool CollectAllFlasks = false,
    int MinimumLampPlacementAction = 0,
    int MaximumLampPlacementAction = 45,
    bool FollowAuthoredRoute = false,
    bool RequireLampPlacement = false,
    bool AllowLampPlacement = true,
    int FlaskMask = -1,
    int[]? FlaskVisitOrder = null);

internal sealed record FlaskRouteReview(
    int FlaskMask,
    IReadOnlyList<int> FlaskVisitOrder,
    SimulationResult Result);

internal sealed record SimulationTraceStep(
    string Action,
    GridCoord PlayerCell,
    LampSnapshot Lamp);

internal sealed record SimulationResult(
    bool Won,
    int RemainingWick,
    int ActionCount,
    bool PlacedLamp,
    int FirstLampPlacementAction,
    bool FiredFlare,
    bool ObservedCrawlerStall,
    int FlasksCollected,
    int ForcedDarkFlaskTurns,
    string Route,
    IReadOnlyList<SimulationTraceStep> Trace,
    string Summary);

internal static class CampaignSimulationHarness
{
    private static readonly (GridCoord Direction, string Name)[] Moves =
    {
        (new GridCoord(0, -1), "U"),
        (new GridCoord(0, 1), "D"),
        (new GridCoord(-1, 0), "L"),
        (new GridCoord(1, 0), "R")
    };

    public static SimulationResult FindWinningPlan(
        LevelDefinition level,
        SimulationRequirements requirements,
        int maxActions = 45,
        int maxStates = 350_000,
        uint tieBreakerSeed = 1)
    {
        SimulationResult? lastFailure = null;
        foreach (var plan in CreateRoutePlans(level, requirements))
        {
            var result = FindWinningPlanForRoute(
                level,
                requirements with
                {
                    FollowAuthoredRoute = true,
                    FlaskMask = plan.Mask,
                    FlaskVisitOrder = plan.Order
                },
                GetFiniteActionBound(level, plan.Mask, maxActions),
                maxStates,
                tieBreakerSeed);
            if (result.Won)
            {
                return result;
            }

            lastFailure = result;
        }

        return lastFailure ?? throw new InvalidOperationException("No bounded campaign route plans were generated.");
    }

    public static IReadOnlyList<FlaskRouteReview> ReviewFlaskRouteVariants(
        LevelDefinition level,
        SimulationRequirements requirements,
        int maxActions = 45,
        int maxStatesPerPlan = 20_000,
        uint tieBreakerSeed = 1) => CreateRoutePlans(level, requirements)
        .Select(plan => new FlaskRouteReview(
            plan.Mask,
            plan.Order,
            FindWinningPlanForRoute(
                level,
                requirements with
                {
                    FollowAuthoredRoute = true,
                    FlaskMask = plan.Mask,
                    FlaskVisitOrder = plan.Order
                },
                GetFiniteActionBound(level, plan.Mask, maxActions),
                maxStatesPerPlan,
                tieBreakerSeed)))
        .ToArray();

    private static SimulationResult FindWinningPlanForRoute(
        LevelDefinition level,
        SimulationRequirements requirements,
        int maxActions,
        int maxStates,
        uint tieBreakerSeed)
    {
        var board = new BoardState(level.Width, level.Height, level.Walls);
        var routeDistances = CreateRouteDistanceMaps(level, board);
        var authoredRouteDistance = GetAuthoredRouteDistance(level, routeDistances);
        var authoredSegments = GetAuthoredRouteSegments(level, routeDistances);
        var root = CreateRoot(level, board, tieBreakerSeed);
        var frontier = new PriorityQueue<SimulationNode, int>();
        var visited = new HashSet<string> { GetKey(root) };
        frontier.Enqueue(root, GetPriority(root, level));
        var explored = 0;
        var peakFrontier = 1;
        var placementBranches = 0;
        var duplicateStates = 0;
        var diagnostics = new SearchDiagnostics();

        while (frontier.Count > 0 && explored < maxStates)
        {
            var node = frontier.Dequeue();
            explored++;
            if (node.ActionCount >= maxActions)
            {
                continue;
            }

            foreach (var move in Moves)
            {
                if (requirements.FollowAuthoredRoute && !MovesTowardNextWaypoint(level, node, move.Direction, routeDistances, requirements))
                {
                    continue;
                }

                var next = TryMove(level, board, node, move.Direction, move.Name, requirements, diagnostics);
                if (next is null)
                {
                    continue;
                }

                if (StairEntryRule.CanEnter(level, next.PlayerCell, next.Lamp.IsCarried) &&
                    MeetsGoal(level, next, requirements))
                {
                    return ToResult(next, explored);
                }

                if (!Enqueue(next, level, frontier, visited))
                {
                    duplicateStates++;
                }
            }

            var shouldPlaceLamp = requirements.AllowLampPlacement && node.Lamp.IsCarried;
            var placed = shouldPlaceLamp ? TryPlaceLamp(level, board, node, requirements, diagnostics) : null;
            if (placed is not null)
            {
                placementBranches++;
                if (!Enqueue(placed, level, frontier, visited))
                {
                    duplicateStates++;
                }
            }

            if (requirements.RequireFlare && !node.FiredFlare)
            {
                var flared = TryFireFlare(level, board, node);
                if (flared is not null)
                {
                    if (!Enqueue(flared, level, frontier, visited))
                    {
                        duplicateStates++;
                    }
                }
            }

            peakFrontier = Math.Max(peakFrontier, frontier.Count);
        }

        return new SimulationResult(
            false, 0, 0, false, 0, false, false, 0, 0, string.Empty, Array.Empty<SimulationTraceStep>(),
            $"Level {level.Index}: no qualifying plan after {explored} deterministic states; " +
            $"peak frontier {peakFrontier}, placement branches {placementBranches}, duplicate states {duplicateStates}, " +
            $"visited {visited.Count}, action bound {maxActions}, authored movement distance {authoredRouteDistance} " +
            $"({authoredSegments}); depleted {diagnostics.Depleted}, smothered {diagnostics.Smothered}, " +
            $"invalid {diagnostics.InvalidAction}.");
    }

    private static SimulationNode CreateRoot(LevelDefinition level, BoardState board, uint tieBreakerSeed)
    {
        var wick = new WickState();
        wick.Initialize(level.StartingWick);
        var turns = new TurnState();
        var lamp = new LampState(level.LampStart);
        var visibility = new VisibilitySystem(board);
        visibility.Recompute(lamp.Cell, wick.CurrentRadius);
        var hazards = new HazardSimulationState(board, level.Crawlers, level.Moths, tieBreakerSeed);
        var flasks = new OilFlaskCollectionState(wick, turns, level.Flasks);
        return new SimulationNode(
            level.PlayerStart,
            lamp.CaptureSnapshot(),
            wick.CaptureSnapshot(),
            turns.CurrentTurn,
            hazards.CaptureSnapshot(),
            flasks.CaptureSnapshot(),
            0,
            false,
            0,
            false,
            false,
            0,
            0,
            string.Empty,
            Array.Empty<SimulationTraceStep>());
    }

    private static SimulationNode? TryMove(
        LevelDefinition level,
        BoardState board,
        SimulationNode node,
        GridCoord direction,
        string routeName,
        SimulationRequirements requirements,
        SearchDiagnostics diagnostics)
    {
        var session = Restore(level, board, node);
        var destination = session.Player.Cell + direction;
        var flaskIndex = Array.IndexOf(level.Flasks, destination);
        if (flaskIndex >= 0 && (requirements.FlaskMask & (1 << flaskIndex)) == 0)
        {
            return null;
        }

        if (!session.Player.TryMove(direction))
        {
            diagnostics.InvalidAction++;
            return null;
        }

        var actionCount = node.ActionCount + 1;
        if (StairEntryRule.CanEnter(level, session.Player.Cell, session.Lamp.IsCarried))
        {
            return Capture(session, node, actionCount, routeName);
        }

        if (session.Wick.IsDepleted)
        {
            diagnostics.Depleted++;
            return null;
        }

        if (AdvanceHazards(session, radiusOverride: null, out var stalled))
        {
            diagnostics.Smothered++;
            return null;
        }

        session.Wick.AdvanceTurn();
        session.Visibility.Recompute(session.Lamp.Cell, session.Wick.CurrentRadius);
        var darkTurns = 0;
        var smotheredDuringLift = false;
        if (session.Flasks.IsAvailableAt(session.Player.Cell))
        {
            session.Visibility.Recompute(session.Lamp.Cell, 0);
            session.Flasks.TurnSpent += radiusOverride =>
            {
                darkTurns++;
                smotheredDuringLift |= AdvanceHazards(session, radiusOverride, out _);
            };
            session.Flasks.TryBeginLift(session.Player.Cell);
            session.Visibility.Recompute(session.Lamp.Cell, session.Wick.CurrentRadius);
        }

        if (session.Wick.IsDepleted || smotheredDuringLift)
        {
            if (session.Wick.IsDepleted)
            {
                diagnostics.Depleted++;
            }
            if (smotheredDuringLift)
            {
                diagnostics.Smothered++;
            }
            return null;
        }

        return Capture(session, node, actionCount, routeName, stalled, darkTurns);
    }

    private static SimulationNode? TryPlaceLamp(
        LevelDefinition level,
        BoardState board,
        SimulationNode node,
        SimulationRequirements requirements,
        SearchDiagnostics diagnostics)
    {
        if (!node.Lamp.IsCarried || node.ActionCount + 1 < requirements.MinimumLampPlacementAction ||
            node.ActionCount + 1 > requirements.MaximumLampPlacementAction)
        {
            return null;
        }

        var session = Restore(level, board, node);
        if (!session.Player.TryToggleLamp())
        {
            diagnostics.InvalidAction++;
            return null;
        }

        if (session.Wick.IsDepleted)
        {
            diagnostics.Depleted++;
            return null;
        }

        if (AdvanceHazards(session, null, out var stalled))
        {
            diagnostics.Smothered++;
            return null;
        }

        session.Wick.AdvanceTurn();
        session.Visibility.Recompute(session.Lamp.Cell, session.Wick.CurrentRadius);
        var placed = Capture(session, node, node.ActionCount + 1, "P", stalled);
        return placed with
        {
            PlacedLamp = true,
            FirstLampPlacementAction = node.FirstLampPlacementAction == 0
                ? node.ActionCount + 1
                : node.FirstLampPlacementAction
        };
    }

    private static SimulationNode? TryFireFlare(LevelDefinition level, BoardState board, SimulationNode node)
    {
        var session = Restore(level, board, node);
        if (!session.Wick.TryFireFlare())
        {
            return null;
        }

        session.Turns.AdvanceTurn();
        session.Visibility.Recompute(session.Lamp.Cell, session.Wick.CurrentRadius);
        if (session.Wick.IsDepleted || AdvanceHazards(session, null, out var stalled))
        {
            return null;
        }

        session.Wick.AdvanceTurn();
        session.Visibility.Recompute(session.Lamp.Cell, session.Wick.CurrentRadius);
        return Capture(session, node, node.ActionCount + 1, "F", stalled) with { FiredFlare = true };
    }

    private static bool AdvanceHazards(SimulationSession session, int? radiusOverride, out bool stalled)
    {
        var smothered = session.Hazards.AdvanceTurn(
            session.Visibility,
            session.Lamp.Cell,
            radiusOverride ?? session.Wick.CurrentRadius);
        stalled = session.Hazards.CrawlerMotion.Any(state => state == HazardMotionState.Stalled);
        return smothered;
    }

    private static SimulationSession Restore(LevelDefinition level, BoardState board, SimulationNode node)
    {
        var wick = new WickState();
        wick.Initialize(level.StartingWick);
        wick.RestoreSnapshot(node.Wick);
        var turns = new TurnState();
        turns.Restore(node.Turn);
        var lamp = new LampState(level.LampStart);
        lamp.RestoreSnapshot(node.Lamp);
        var visibility = new VisibilitySystem(board);
        visibility.Recompute(lamp.Cell, wick.CurrentRadius);
        var player = new PlayerMovementState(board, wick, turns, lamp, node.PlayerCell, visibility);
        var hazards = new HazardSimulationState(board, level.Crawlers, level.Moths);
        hazards.RestoreSnapshot(node.Hazards);
        var flasks = new OilFlaskCollectionState(wick, turns, level.Flasks);
        flasks.RestoreSnapshot(node.Flasks);
        return new SimulationSession(player, lamp, wick, turns, visibility, hazards, flasks);
    }

    private static SimulationNode Capture(
        SimulationSession session,
        SimulationNode previous,
        int actionCount,
        string routeName,
        bool stalled = false,
        int darkTurns = 0) => new(
            session.Player.Cell,
            session.Lamp.CaptureSnapshot(),
            session.Wick.CaptureSnapshot(),
            session.Turns.CurrentTurn,
            session.Hazards.CaptureSnapshot(),
            session.Flasks.CaptureSnapshot(),
            actionCount,
            previous.PlacedLamp,
            previous.FirstLampPlacementAction,
            previous.FiredFlare,
            previous.ObservedCrawlerStall || stalled,
            session.Flasks.CaptureSnapshot().Collected.Count(collected => collected),
            previous.ForcedDarkFlaskTurns + darkTurns,
            previous.Route + routeName,
            previous.Trace.Append(new SimulationTraceStep(
                routeName,
                session.Player.Cell,
                session.Lamp.CaptureSnapshot())).ToArray());

    private static bool MeetsGoal(LevelDefinition level, SimulationNode node, SimulationRequirements requirements)
    {
        if (node.Wick.CurrentWick < level.TargetCompletionWickMin || node.Wick.CurrentWick > level.TargetCompletionWickMax)
        {
            return false;
        }

        if (requirements.RequireLampPlacement && !node.PlacedLamp ||
            requirements.RequireFlare && !node.FiredFlare ||
            requirements.RequireCrawlerStall && !node.ObservedCrawlerStall ||
            node.FlasksCollected < requirements.MinimumFlasksCollected)
        {
            return false;
        }

        return !requirements.CollectAllFlasks || node.FlasksCollected == level.Flasks.Length;
    }

    private static bool Enqueue(
        SimulationNode node,
        LevelDefinition level,
        PriorityQueue<SimulationNode, int> frontier,
        HashSet<string> visited)
    {
        var key = GetKey(node);
        if (visited.Add(key))
        {
            frontier.Enqueue(node, GetPriority(node, level));
            return true;
        }

        return false;
    }

    private static int GetPriority(SimulationNode node, LevelDefinition level) =>
        node.ActionCount + Math.Abs(node.PlayerCell.X - level.StairCell.X) + Math.Abs(node.PlayerCell.Y - level.StairCell.Y);

    private static Dictionary<GridCoord, Dictionary<GridCoord, int>> CreateRouteDistanceMaps(LevelDefinition level, BoardState board)
    {
        var maps = new Dictionary<GridCoord, Dictionary<GridCoord, int>>();
        foreach (var waypoint in level.Flasks.Append(level.StairCell).Distinct())
        {
            var distances = new Dictionary<GridCoord, int> { [waypoint] = 0 };
            var queue = new Queue<GridCoord>();
            queue.Enqueue(waypoint);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var move in Moves)
                {
                    var next = current + move.Direction;
                    if (board.IsWalkable(next) && !distances.ContainsKey(next))
                    {
                        distances[next] = distances[current] + 1;
                        queue.Enqueue(next);
                    }
                }
            }

            maps[waypoint] = distances;
        }

        return maps;
    }

    private static bool MovesTowardNextWaypoint(
        LevelDefinition level,
        SimulationNode node,
        GridCoord direction,
        IReadOnlyDictionary<GridCoord, Dictionary<GridCoord, int>> distanceMaps,
        SimulationRequirements requirements)
    {
        var targetIndex = requirements.FlaskVisitOrder?.FirstOrDefault(
            index => !node.Flasks.Collected[index],
            -1) ?? -1;
        var target = targetIndex >= 0 ? level.Flasks[targetIndex] : level.StairCell;
        var distances = distanceMaps[target];
        var next = node.PlayerCell + direction;
        return distances.TryGetValue(node.PlayerCell, out var currentDistance) &&
               distances.TryGetValue(next, out var nextDistance) &&
               nextDistance == currentDistance - 1;
    }

    private static int GetAuthoredRouteDistance(
        LevelDefinition level,
        IReadOnlyDictionary<GridCoord, Dictionary<GridCoord, int>> distanceMaps)
    {
        var distance = 0;
        var current = level.PlayerStart;
        foreach (var target in level.Flasks.Append(level.StairCell))
        {
            if (!distanceMaps[target].TryGetValue(current, out var segment))
            {
                return int.MaxValue;
            }

            distance += segment;
            current = target;
        }

        return distance;
    }

    private static string GetAuthoredRouteSegments(
        LevelDefinition level,
        IReadOnlyDictionary<GridCoord, Dictionary<GridCoord, int>> distanceMaps)
    {
        var segments = new List<int>();
        var current = level.PlayerStart;
        foreach (var target in level.Flasks.Append(level.StairCell))
        {
            if (!distanceMaps[target].TryGetValue(current, out var segment))
            {
                return "unreachable";
            }

            segments.Add(segment);
            current = target;
        }

        return string.Join('+', segments);
    }

    private static IReadOnlyList<(int Mask, int[] Order)> CreateRoutePlans(
        LevelDefinition level,
        SimulationRequirements requirements)
    {
        if (requirements.FlaskVisitOrder is not null)
        {
            var mask = requirements.FlaskMask >= 0
                ? requirements.FlaskMask
                : requirements.FlaskVisitOrder.Aggregate(0, (value, index) => value | (1 << index));
            return new[] { (mask, requirements.FlaskVisitOrder.ToArray()) };
        }

        var fullMask = (1 << level.Flasks.Length) - 1;
        IEnumerable<int> masks = requirements.SkipAllFlasks
            ? new[] { 0 }
            : requirements.CollectAllFlasks
                ? new[] { fullMask }
                : Enumerable.Range(0, fullMask + 1);
        var plans = new List<(int Mask, int[] Order)>();
        foreach (var mask in masks)
        {
            var selected = Enumerable.Range(0, level.Flasks.Length)
                .Where(index => (mask & (1 << index)) != 0)
                .ToArray();
            foreach (var order in Permute(selected))
            {
                plans.Add((mask, order));
            }
        }

        return plans;
    }

    private static IEnumerable<int[]> Permute(int[] values)
    {
        if (values.Length == 0)
        {
            yield return Array.Empty<int>();
            yield break;
        }

        for (var index = 0; index < values.Length; index++)
        {
            var remaining = values.Where((_, candidate) => candidate != index).ToArray();
            foreach (var suffix in Permute(remaining))
            {
                yield return new[] { values[index] }.Concat(suffix).ToArray();
            }
        }
    }

    private static int GetFiniteActionBound(LevelDefinition level, int flaskMask, int requestedMaximum)
    {
        var selectedFlasks = Enumerable.Range(0, level.Flasks.Length)
            .Count(index => (flaskMask & (1 << index)) != 0);
        var resourceBound = level.StartingWick + (selectedFlasks * LevelTuning.FlaskRestore) - level.TargetCompletionWickMin;
        return Math.Min(requestedMaximum, resourceBound);
    }

    private static string GetKey(SimulationNode node)
    {
        var crawlers = string.Join(';', node.Hazards.Crawlers.Select(item => $"{item.Cell.X},{item.Cell.Y},{item.HasReachedLamp},{item.EnteredLitSpace},{item.SkipNextLitTurn}"));
        var moths = string.Join(';', node.Hazards.Moths.Select(item => $"{item.Cell.X},{item.Cell.Y},{item.HasReachedLamp},{item.EnteredLitSpace}"));
        var flasks = string.Join(',', node.Flasks.Collected.Select(value => value ? '1' : '0'));
        return $"{node.PlayerCell.X},{node.PlayerCell.Y}|{node.Lamp.Cell.X},{node.Lamp.Cell.Y},{node.Lamp.IsCarried}|" +
               $"{node.Wick.CurrentWick},{node.Wick.FlareTurnsRemaining}|{crawlers}|{moths}|{flasks}|" +
               $"{node.Hazards.TieBreakerState}|{node.PlacedLamp},{node.FirstLampPlacementAction},{node.FiredFlare},{node.ObservedCrawlerStall},{node.ForcedDarkFlaskTurns}";
    }

    private static SimulationResult ToResult(SimulationNode node, int explored) => new(
        true,
        node.Wick.CurrentWick,
        node.ActionCount,
        node.PlacedLamp,
        node.FirstLampPlacementAction,
        node.FiredFlare,
        node.ObservedCrawlerStall,
        node.FlasksCollected,
        node.ForcedDarkFlaskTurns,
        node.Route,
        node.Trace,
        $"Won in {node.ActionCount} actions with {node.Wick.CurrentWick} wick after exploring {explored} states. Route: {node.Route}");

    private sealed record SimulationNode(
        GridCoord PlayerCell,
        LampSnapshot Lamp,
        WickSnapshot Wick,
        int Turn,
        HazardSimulationSnapshot Hazards,
        OilFlaskCollectionSnapshot Flasks,
        int ActionCount,
        bool PlacedLamp,
        int FirstLampPlacementAction,
        bool FiredFlare,
        bool ObservedCrawlerStall,
        int FlasksCollected,
        int ForcedDarkFlaskTurns,
        string Route,
        IReadOnlyList<SimulationTraceStep> Trace);

    private sealed record SimulationSession(
        PlayerMovementState Player,
        LampState Lamp,
        WickState Wick,
        TurnState Turns,
        VisibilitySystem Visibility,
        HazardSimulationState Hazards,
        OilFlaskCollectionState Flasks);

    private sealed class SearchDiagnostics
    {
        public int Depleted { get; set; }
        public int Smothered { get; set; }
        public int InvalidAction { get; set; }
    }
}
