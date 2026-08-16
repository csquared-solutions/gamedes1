using System.Collections.Generic;
using System.Linq;
using Wick.Board;

namespace Wick.Data;

public sealed record TerminalCorridor(GridCoord CommitmentPoint, IReadOnlyList<GridCoord> Cells);

public static class LevelTopology
{
    private static readonly GridCoord[] Directions =
    {
        new(0, -1),
        new(0, 1),
        new(-1, 0),
        new(1, 0)
    };

    public static TerminalCorridor FindTerminalCorridor(BoardState board, GridCoord terminal)
    {
        var terminalNeighbors = GetWalkableNeighbors(board, terminal);
        if (terminalNeighbors.Count != 1)
        {
            return null;
        }

        var corridorFromTerminal = new List<GridCoord> { terminal };
        var previous = terminal;
        var current = terminalNeighbors[0];
        while (true)
        {
            var neighbors = GetWalkableNeighbors(board, current);
            if (neighbors.Count != 2)
            {
                if (neighbors.Count < 3 || corridorFromTerminal.Count < 2)
                {
                    return null;
                }

                corridorFromTerminal.Reverse();
                return new TerminalCorridor(current, corridorFromTerminal);
            }

            corridorFromTerminal.Add(current);
            var next = neighbors.Single(cell => cell != previous);
            previous = current;
            current = next;
        }
    }

    private static IReadOnlyList<GridCoord> GetWalkableNeighbors(BoardState board, GridCoord cell) =>
        Directions.Select(direction => cell + direction).Where(board.IsWalkable).ToArray();
}
