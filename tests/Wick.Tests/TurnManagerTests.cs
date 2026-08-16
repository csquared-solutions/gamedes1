using Wick.Core;
using Xunit;

namespace Wick.Tests;

public sealed class TurnManagerTests
{
    [Fact]
    public void AdvanceTurn_IncrementsTurnCounter()
    {
        var turnManager = new TurnState();

        turnManager.AdvanceTurn();

        Assert.Equal(1, turnManager.CurrentTurn);
    }

    [Fact]
    public void Reset_SetsTurnCounterBackToZero()
    {
        var turnManager = new TurnState();
        turnManager.AdvanceTurn();

        turnManager.Reset();

        Assert.Equal(0, turnManager.CurrentTurn);
    }
}
