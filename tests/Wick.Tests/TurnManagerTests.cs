using Wick.Core;
using Xunit;

namespace Wick.Tests;

public sealed class TurnManagerTests
{
    [Fact]
    public void AdvanceTurn_IncrementsTurnCounter()
    {
        var turnManager = new TurnManager();

        turnManager.AdvanceTurn();

        Assert.Equal(1, turnManager.CurrentTurn);
    }
}
