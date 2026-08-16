using System.Collections.Generic;
using Wick.Board;
using Wick.Data;
using Xunit;

namespace Wick.Tests;

public sealed class LevelLoaderTests
{
    [Fact]
    public void Load_ReturnsAuthoredSingleRoomDefinition_ByIndex()
    {
        var definition = new LevelDefinition
        {
            Index = 1,
            StartingWick = 12,
            PlayerStart = new GridCoord(1, 1),
            LampStart = new GridCoord(1, 1),
            StairCell = new GridCoord(17, 11)
        };
        var loader = new LevelLoader(new[] { definition });

        var loaded = loader.Load(1);

        Assert.Same(definition, loaded);
        Assert.Equal(19, loaded.Width);
        Assert.Equal(13, loaded.Height);
    }

    [Fact]
    public void DefaultRoom_ReachableStairLeavesWickBeforeDepletion()
    {
        var level = new LevelLoader().Load(1);
        var board = new BoardState(level.Width, level.Height, level.Walls);

        var distance = FindShortestPathLength(board, level.PlayerStart, level.StairCell);

        Assert.NotNull(distance);
        Assert.True(distance < level.StartingWick);
    }

    private static int? FindShortestPathLength(BoardState board, GridCoord start, GridCoord goal)
    {
        var queue = new Queue<(GridCoord Cell, int Distance)>();
        var visited = new HashSet<GridCoord> { start };
        queue.Enqueue((start, 0));

        while (queue.Count > 0)
        {
            var (cell, distance) = queue.Dequeue();
            if (cell == goal)
            {
                return distance;
            }

            foreach (var direction in new[]
                     {
                         new GridCoord(0, -1), new GridCoord(0, 1),
                         new GridCoord(-1, 0), new GridCoord(1, 0)
                     })
            {
                var next = cell + direction;
                if (board.IsWalkable(next) && visited.Add(next))
                {
                    queue.Enqueue((next, distance + 1));
                }
            }
        }

        return null;
    }
}
