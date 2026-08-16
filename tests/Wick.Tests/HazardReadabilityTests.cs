using Wick.Board;
using Wick.Core;
using Wick.Hazards;
using Xunit;

namespace Wick.Tests;

public sealed class HazardReadabilityTests
{
    [Fact]
    public void HazardState_IsOnlyShownWhenTheHazardTileIsVisible()
    {
        var board = new BoardState(8, 8);
        var visibility = new VisibilitySystem(board);
        visibility.Recompute(new GridCoord(2, 2), radius: 1);
        Assert.True(HazardReadability.IsVisible(visibility, new GridCoord(3, 2)));
        Assert.False(HazardReadability.IsVisible(visibility, new GridCoord(6, 2)));
    }
}
