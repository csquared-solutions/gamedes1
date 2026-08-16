using System.Collections.Generic;
using Wick.Board;
using Wick.Core;

namespace Wick.Hazards;

public sealed class HazardContext
{
    public HazardContext(BoardState board, VisibilitySystem visibility, GridCoord lampCell, int lampRadius)
    {
        Board = board;
        Visibility = visibility;
        LampCell = lampCell;
        LampRadius = lampRadius;
    }

    public BoardState Board { get; }
    public VisibilitySystem Visibility { get; }
    public GridCoord LampCell { get; }
    public int LampRadius { get; }

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

    protected GridCoord FindNextStep(HazardContext context)
    {
        if (Cell == context.LampCell)
        {
            HasReachedLamp = true;
            return Cell;
        }

        var frontier = new Queue<GridCoord>();
        var visited = new HashSet<GridCoord> { Cell };
        var previous = new Dictionary<GridCoord, GridCoord>();
        frontier.Enqueue(Cell);

        while (frontier.Count > 0)
        {
            var current = frontier.Dequeue();
            if (current == context.LampCell)
            {
                break;
            }

            foreach (var direction in NeighborOrder)
            {
                var next = current + direction;
                if (!context.Board.IsWalkable(next) || !visited.Add(next))
                {
                    continue;
                }

                previous[next] = current;
                frontier.Enqueue(next);
            }
        }

        if (!visited.Contains(context.LampCell))
        {
            return Cell;
        }

        var step = context.LampCell;
        while (previous[step] != Cell)
        {
            step = previous[step];
        }

        return step;
    }

    protected GridCoord MoveTowardLamp(HazardContext context)
    {
        Cell = FindNextStep(context);
        HasReachedLamp = Cell == context.LampCell;
        return Cell;
    }
}
