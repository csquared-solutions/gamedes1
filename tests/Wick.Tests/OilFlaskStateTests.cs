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

    [Fact]
    public void FlaskCollection_InitializesAndTracksEveryAuthoredFlaskIndependently()
    {
        var wick = new WickState();
        wick.Initialize(10);
        var turns = new TurnState();
        var first = new GridCoord(2, 2);
        var second = new GridCoord(4, 2);
        var flasks = new OilFlaskCollectionState(wick, turns, new[] { first, second });

        Assert.True(flasks.TryBeginLift(first));

        Assert.True(flasks.IsCollected(first));
        Assert.False(flasks.IsCollected(second));
        Assert.False(flasks.TryBeginLift(new GridCoord(3, 2)));
    }

    [Fact]
    public void FlaskCollection_ReportsRadiusZeroForBothHazardTurns()
    {
        var wick = new WickState();
        wick.Initialize(6);
        var turns = new TurnState();
        var pickup = new GridCoord(2, 2);
        var flasks = new OilFlaskCollectionState(wick, turns, new[] { pickup });
        var hazardRadii = new List<int>();
        flasks.TurnSpent += hazardRadii.Add;

        Assert.True(flasks.TryBeginLift(pickup));

        Assert.Equal(new[] { 0, 0 }, hazardRadii);
    }

    [Fact]
    public void FlaskLift_TerminalCallbackStopsLiftAndKeepsInputLocked()
    {
        var wick = new WickState();
        wick.Initialize(6);
        var turns = new TurnState();
        var pickup = new GridCoord(2, 2);
        var flasks = new OilFlaskCollectionState(wick, turns, new[] { pickup });
        var terminal = false;
        flasks.TurnSpent += _ => terminal = true;

        var completed = flasks.TryBeginLift(pickup, () => !terminal);

        Assert.True(completed);
        Assert.True(flasks.IsCollected(pickup));
        Assert.Equal(5, wick.CurrentWick);
        Assert.Equal(1, turns.CurrentTurn);
        Assert.False(OilFlaskInputPolicy.ShouldUnlockAfterLift(terminal));
    }
}
