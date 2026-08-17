using System;
using System.Collections.Generic;
using System.Linq;
using Wick.Board;
using Wick.Core;

namespace Wick.Hazards;

public enum HazardMotionState
{
    Stalled,
    Slowed,
    Moving
}

public static class HazardReadability
{
    public static bool IsVisible(VisibilitySystem visibility, GridCoord hazardCell) =>
        visibility.IsVisible(hazardCell);
}

public sealed class HazardPathTieBreaker
{
    private const uint FallbackSeed = 0x6D2B79F5;
    private uint _seed;

    public HazardPathTieBreaker(uint seed = FallbackSeed)
    {
        _seed = seed == 0 ? FallbackSeed : seed;
    }

    public int ChooseIndex(int optionCount, GridCoord origin, GridCoord target)
    {
        if (optionCount <= 1)
        {
            return 0;
        }

        var hash = _seed;
        hash ^= unchecked((uint)(origin.X * 73_856_093));
        hash ^= unchecked((uint)(origin.Y * 19_349_663));
        hash ^= unchecked((uint)(target.X * 83_492_791));
        hash ^= unchecked((uint)(target.Y * 2_654_435_761));
        hash ^= hash >> 16;
        hash *= 0x7FEB352D;
        hash ^= hash >> 15;
        hash *= 0x846CA68B;
        hash ^= hash >> 16;
        return (int)(hash % (uint)optionCount);
    }

    public uint CaptureState() => _seed;

    public void RestoreState(uint state) => _seed = state == 0 ? FallbackSeed : state;
}

public sealed class HazardContext
{
    public HazardContext(
        BoardState board,
        VisibilitySystem visibility,
        GridCoord lampCell,
        int lampRadius)
        : this(board, visibility, lampCell, lampRadius, new HazardPathTieBreaker())
    {
    }

    public HazardContext(
        BoardState board,
        VisibilitySystem visibility,
        GridCoord lampCell,
        int lampRadius,
        HazardPathTieBreaker tieBreaker)
    {
        Board = board;
        Visibility = visibility;
        LampCell = lampCell;
        LampRadius = lampRadius;
        TieBreaker = tieBreaker;
    }

    public BoardState Board { get; }
    public VisibilitySystem Visibility { get; }
    public GridCoord LampCell { get; }
    public int LampRadius { get; }
    public HazardPathTieBreaker TieBreaker { get; }

    public bool IsLit(GridCoord cell) => Visibility.IsVisible(cell);
}

public abstract class HazardBase
{
    private static readonly GridCoord[] NeighborOrder =
    {
        new(0, -1),
        new(0, 1),
        new(-1, 0),
        new(1, 0)
    };

    protected HazardBase(GridCoord startCell)
    {
        Cell = startCell;
    }

    public GridCoord Cell { get; protected set; }
    public bool HasReachedLamp { get; protected set; }

    public abstract GridCoord TakeTurn(HazardContext context);

    protected GridCoord FindNextStep(HazardContext context, bool randomizeTies = true)
    {
        if (Cell == context.LampCell)
        {
            HasReachedLamp = true;
            return Cell;
        }

        var frontier = new Queue<GridCoord>();
        var distances = new Dictionary<GridCoord, int> { [context.LampCell] = 0 };
        frontier.Enqueue(context.LampCell);

        while (frontier.Count > 0)
        {
            var current = frontier.Dequeue();
            foreach (var direction in NeighborOrder)
            {
                var next = current + direction;
                if (!context.Board.IsWalkable(next) || distances.ContainsKey(next))
                {
                    continue;
                }

                distances[next] = distances[current] + 1;
                frontier.Enqueue(next);
            }
        }

        if (!distances.TryGetValue(Cell, out var currentDistance))
        {
            return Cell;
        }

        var candidates = NeighborOrder
            .Select(direction => Cell + direction)
            .Where(next => distances.TryGetValue(next, out var distance) && distance == currentDistance - 1)
            .ToArray();
        if (candidates.Length == 0)
        {
            return Cell;
        }

        var optionCount = Math.Min(candidates.Length, 2);
        var canRandomize = randomizeTies && candidates.Take(optionCount).All(next => !context.IsLit(next));
        var selectedIndex = canRandomize
            ? context.TieBreaker.ChooseIndex(optionCount, Cell, context.LampCell)
            : 0;
        return candidates[selectedIndex];
    }

    protected GridCoord MoveTowardLamp(HazardContext context, bool randomizeTies = true)
    {
        Cell = FindNextStep(context, randomizeTies);
        HasReachedLamp = Cell == context.LampCell;
        return Cell;
    }
}
