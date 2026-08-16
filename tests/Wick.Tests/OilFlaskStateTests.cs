using Wick.Board;
using Wick.Core;
using Wick.Pickups;
using Xunit;

namespace Wick.Tests;

public sealed class OilFlaskStateTests
{
    [Fact]
    public void FlaskLift_ConsumesTwoTurns_ThenRestoresSixWick()
    {
        var wick = new WickState();
        wick.Initialize(4);
        var turns = new TurnState();
        var pickupCell = new GridCoord(3, 2);
        var flask = new OilFlaskState(wick, turns, pickupCell);

        Assert.True(flask.TryBeginLift(pickupCell));

        Assert.Equal(8, wick.CurrentWick);
        Assert.Equal(2, turns.CurrentTurn);
    }

    [Fact]
    public void FlaskLift_AtFinalWick_AbortsWithoutRestoringWick()
    {
        var wick = new WickState();
        wick.Initialize(1);
        var turns = new TurnState();
        var pickupCell = new GridCoord(3, 2);
        var flask = new OilFlaskState(wick, turns, pickupCell);

        Assert.True(flask.TryBeginLift(pickupCell));

        Assert.Equal(0, wick.CurrentWick);
        Assert.Equal(0, turns.CurrentTurn);
    }

    [Fact]
    public void FlaskLift_IsOnlyAvailableOnceFromItsPickupCell()
    {
        var pickupCell = new GridCoord(3, 2);
        var playerCell = new GridCoord(3, 2);
        var wick = new WickState();
        wick.Initialize(6);
        var turns = new TurnState();

        var flask = new OilFlaskState(wick, turns, pickupCell);

        Assert.False(flask.TryBeginLift(new GridCoord(2, 2)));
        Assert.True(flask.TryBeginLift(playerCell));
        Assert.False(flask.TryBeginLift(playerCell));
        Assert.Equal(10, wick.CurrentWick);
    }
}
