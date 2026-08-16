using Wick.Board;
using Wick.Core;
using Wick.Hazards;
using Xunit;

namespace Wick.Tests;

public sealed class HazardSystemTests
{
    [Fact]
    public void Crawler_MovesEveryTurnInDarkness()
    {
        var crawler = new CrawlerController(new GridCoord(1, 1));
        var context = CreateContext(lampCell: new GridCoord(6, 1), radius: 1);

        crawler.TakeTurn(context);
        crawler.TakeTurn(context);

        Assert.Equal(new GridCoord(3, 1), crawler.Cell);
    }

    [Fact]
    public void Crawler_MovesEveryOtherTurnInLight()
    {
        var crawler = new CrawlerController(new GridCoord(2, 1));
        var context = CreateContext(lampCell: new GridCoord(4, 1), radius: 3);

        crawler.TakeTurn(context);
        crawler.TakeTurn(context);

        Assert.Equal(new GridCoord(3, 1), crawler.Cell);
    }

    [Fact]
    public void Crawler_StallsOnce_WhenFirstEnteringLitSpace()
    {
        var crawler = new CrawlerController(new GridCoord(1, 1));
        var context = CreateContext(lampCell: new GridCoord(4, 1), radius: 2);

        crawler.TakeTurn(context);
        crawler.TakeTurn(context);

        Assert.Equal(new GridCoord(2, 1), crawler.Cell);
    }

    [Fact]
    public void Moth_IsDormant_WhenRadiusBelowFive()
    {
        var moth = new MothController(new GridCoord(1, 1));
        var context = CreateContext(lampCell: new GridCoord(4, 1), radius: 4);

        moth.TakeTurn(context);

        Assert.Equal(new GridCoord(1, 1), moth.Cell);
    }

    [Fact]
    public void Moth_MovesTowardLamp_WhenRadiusIsFive()
    {
        var moth = new MothController(new GridCoord(1, 1));
        var context = CreateContext(lampCell: new GridCoord(4, 1), radius: 5);

        moth.TakeTurn(context);

        Assert.Equal(new GridCoord(2, 1), moth.Cell);
    }

    [Fact]
    public void Moth_StallsOnce_WhenFirstEnteringLitSpace()
    {
        var moth = new MothController(new GridCoord(1, 1));
        var context = CreateContext(lampCell: new GridCoord(8, 1), radius: 5);

        moth.TakeTurn(context);
        moth.TakeTurn(context);

        Assert.Equal(new GridCoord(2, 1), moth.Cell);
    }

    [Fact]
    public void Hazard_RetargetsToCurrentLampCell_UsingDeterministicNeighborOrder()
    {
        var crawler = new CrawlerController(new GridCoord(1, 1));
        var firstContext = CreateContext(lampCell: new GridCoord(3, 1), radius: 0);
        var movedLampContext = CreateContext(lampCell: new GridCoord(2, 3), radius: 0);

        crawler.TakeTurn(firstContext);
        crawler.TakeTurn(movedLampContext);

        Assert.Equal(new GridCoord(2, 2), crawler.Cell);
    }

    [Fact]
    public void Hazard_UsesDownBeforeRight_ForEqualCostPathChoices()
    {
        var crawler = new CrawlerController(new GridCoord(1, 1));
        var context = CreateContext(lampCell: new GridCoord(3, 3), radius: 0);

        crawler.TakeTurn(context);

        Assert.Equal(new GridCoord(1, 2), crawler.Cell);
    }

    [Fact]
    public void Hazard_HasReachedLamp_AfterMovingOntoLampTile()
    {
        var crawler = new CrawlerController(new GridCoord(1, 1));
        var context = CreateContext(lampCell: new GridCoord(2, 1), radius: 0);

        crawler.TakeTurn(context);
        crawler.TakeTurn(context);

        Assert.True(crawler.HasReachedLamp);
    }

    private static HazardContext CreateContext(GridCoord lampCell, int radius)
    {
        var board = new BoardState(19, 13);
        var visibility = new VisibilitySystem(board);
        visibility.Recompute(lampCell, radius);
        return new HazardContext(board, visibility, lampCell, radius);
    }
}
