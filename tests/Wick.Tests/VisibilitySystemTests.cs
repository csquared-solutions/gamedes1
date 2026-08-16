using Wick.Board;
using Wick.Core;
using Xunit;

namespace Wick.Tests;

public sealed class VisibilitySystemTests
{
    [Fact]
    public void Recompute_MarksLampTileVisible()
    {
        var board = TestBoards.EmptyRoom();
        var visibility = new VisibilitySystem(board);

        var result = visibility.Recompute(new GridCoord(9, 6), 3);

        Assert.Contains(new GridCoord(9, 6), result.Visible);
    }

    [Fact]
    public void Recompute_PreservesSeenMemory_ForPreviouslyVisibleWalls()
    {
        var board = TestBoards.EmptyRoom();
        var visibility = new VisibilitySystem(board);

        visibility.Recompute(new GridCoord(9, 6), 4);
        visibility.Recompute(new GridCoord(9, 6), 1);

        Assert.True(visibility.WasSeen(new GridCoord(9, 2)));
    }

    [Fact]
    public void Recompute_LeavesBlockingWallVisible_AndHidesTilesBehindIt()
    {
        var wall = new GridCoord(9, 5);
        var board = TestBoards.WithWalls(wall);
        var visibility = new VisibilitySystem(board);

        visibility.Recompute(new GridCoord(9, 6), 4);

        Assert.True(visibility.IsVisible(wall));
        Assert.False(visibility.IsVisible(new GridCoord(9, 4)));
    }
}

internal static class TestBoards
{
    public static BoardState EmptyRoom() => new(19, 13);

    public static BoardState WithWalls(params GridCoord[] walls) => new(19, 13, walls);
}
