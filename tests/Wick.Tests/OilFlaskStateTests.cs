using System.Reflection;
using Wick.Core;
using Xunit;

namespace Wick.Tests;

public sealed class OilFlaskStateTests
{
    [Fact]
    public void FlaskLift_ConsumesTwoTurns_ThenRestoresSixWick()
    {
        var flaskType = typeof(WickState).Assembly.GetType("Wick.Pickups.OilFlaskState");

        Assert.NotNull(flaskType);

        var wick = new WickState();
        wick.Initialize(4);
        var turns = new TurnState();
        var flask = Activator.CreateInstance(flaskType!, wick, turns);

        flaskType!.GetMethod("BeginLift")!.Invoke(flask, null);

        Assert.Equal(8, wick.CurrentWick);
        Assert.Equal(2, turns.CurrentTurn);
    }
}
