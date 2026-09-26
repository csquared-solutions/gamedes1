using Wick.Actors;
using Wick.Board;
using Wick.Core;
using Xunit;

namespace Wick.Tests;

public sealed class PlayerMovementStateTests
{
    [Fact]
    public void TryMove_ConsumesOneWickAndAdvancesTurn_OnSuccessfulOrthogonalMove()
    {
        var wick = new WickState();
        wick.Initialize(5);
        var turns = new TurnState();
        var lamp = new LampState(new GridCoord(2, 2));
        var state = new PlayerMovementState(new BoardState(5, 5), wick, turns, lamp, new GridCoord(2, 2));

        var moved = state.TryMove(new GridCoord(1, 0));

        Assert.True(moved);
        Assert.Equal(new GridCoord(3, 2), state.Cell);
        Assert.Equal(4, wick.CurrentWick);
        Assert.Equal(1, turns.CurrentTurn);
    }

    [Fact]
    public void TryMove_DoesNotSpendWickOrAdvanceTurn_ForDiagonalOrBlockedMove()
    {
        var wick = new WickState();
        wick.Initialize(5);
        var turns = new TurnState();
        var lamp = new LampState(new GridCoord(2, 2));
        var board = new BoardState(5, 5, new[] { new GridCoord(3, 2) });
        var state = new PlayerMovementState(board, wick, turns, lamp, new GridCoord(2, 2));

        Assert.False(state.TryMove(new GridCoord(1, 1)));
        Assert.False(state.TryMove(new GridCoord(1, 0)));

        Assert.Equal(new GridCoord(2, 2), state.Cell);
        Assert.Equal(5, wick.CurrentWick);
        Assert.Equal(0, turns.CurrentTurn);
    }

    [Fact]
    public void TryToggleLamp_LeavesLampBehind_WhenPlayerMovesAway()
    {
        var wick = new WickState();
        wick.Initialize(5);
        var turns = new TurnState();
        var lamp = new LampState(new GridCoord(2, 2));
        var board = new BoardState(5, 5);
        var visibility = new VisibilitySystem(board);
        var state = new PlayerMovementState(board, wick, turns, lamp, new GridCoord(2, 2), visibility);

        Assert.True(state.TryToggleLamp());
        Assert.True(state.TryMove(new GridCoord(1, 0)));
        Assert.True(state.TryMove(new GridCoord(1, 0)));

        Assert.False(lamp.IsCarried);
        Assert.Equal(new GridCoord(2, 2), lamp.Cell);
        Assert.Equal(new GridCoord(4, 2), state.Cell);
        Assert.True(visibility.IsVisible(lamp.Cell));
        Assert.False(visibility.IsVisible(state.Cell));
    }

    [Fact]
    public void TryToggleLamp_ConsumesOneWickAndAdvancesTurn()
    {
        var wick = new WickState();
        wick.Initialize(5);
        var turns = new TurnState();
        var lamp = new LampState(new GridCoord(2, 2));
        var state = new PlayerMovementState(new BoardState(5, 5), wick, turns, lamp, new GridCoord(2, 2));

        Assert.True(state.TryToggleLamp());

        Assert.Equal(4, wick.CurrentWick);
        Assert.Equal(1, turns.CurrentTurn);
    }

    [Fact]
    public void TryToggleLamp_RecoversPlacedLamp_WhenStandingOnIt()
    {
        var wick = new WickState();
        wick.Initialize(5);
        var turns = new TurnState();
        var lamp = new LampState(new GridCoord(2, 2));
        var state = new PlayerMovementState(new BoardState(5, 5), wick, turns, lamp, new GridCoord(2, 2));

        Assert.True(state.TryToggleLamp());
        Assert.False(lamp.IsCarried);
        Assert.True(state.TryToggleLamp());

        Assert.True(lamp.IsCarried);
        Assert.Equal(state.Cell, lamp.Cell);
        Assert.Equal(3, wick.CurrentWick);
        Assert.Equal(2, turns.CurrentTurn);
    }

    [Fact]
    public void TryToggleLamp_DoesNothing_WhenLampIsElsewhere()
    {
        var wick = new WickState();
        wick.Initialize(5);
        var turns = new TurnState();
        var lamp = new LampState(new GridCoord(2, 2));
        var state = new PlayerMovementState(new BoardState(5, 5), wick, turns, lamp, new GridCoord(2, 2));

        Assert.True(state.TryToggleLamp());
        Assert.True(state.TryMove(new GridCoord(1, 0)));

        Assert.False(state.TryToggleLamp());
        Assert.False(lamp.IsCarried);
        Assert.Equal(3, wick.CurrentWick);
        Assert.Equal(2, turns.CurrentTurn);
    }

    [Fact]
    public void TryMove_AttachesLamp_WhenPlayerReturnsToItsCell()
    {
        var wick = new WickState();
        wick.Initialize(5);
        var turns = new TurnState();
        var lamp = new LampState(new GridCoord(2, 2));
        var state = new PlayerMovementState(new BoardState(5, 5), wick, turns, lamp, new GridCoord(2, 2));

        state.TryToggleLamp();
        state.TryMove(new GridCoord(1, 0));
        state.TryMove(new GridCoord(-1, 0));

        Assert.True(lamp.IsCarried);
        Assert.Equal(state.Cell, lamp.Cell);
    }

    [Fact]
    public void TryMove_AutomaticallyRecoversPlacedLampWithoutDoubleSpending()
    {
        var wick = new WickState();
        wick.Initialize(5);
        var turns = new TurnState();
        var lamp = new LampState(new GridCoord(2, 2));
        var state = new PlayerMovementState(new BoardState(5, 5), wick, turns, lamp, new GridCoord(2, 2));

        Assert.True(state.TryToggleLamp());
        Assert.True(state.TryMove(new GridCoord(1, 0)));
        Assert.True(state.TryMove(new GridCoord(-1, 0)));

        Assert.True(lamp.IsCarried);
        Assert.Equal(state.Cell, lamp.Cell);
        Assert.Equal(2, wick.CurrentWick);
        Assert.Equal(3, turns.CurrentTurn);
    }

    [Fact]
    public void TryMove_AutoRecoveryAtFinalWick_ResetsTurnWithoutLeavingStaleAction()
    {
        var wick = new WickState();
        wick.Initialize(3);
        var turns = new TurnState();
        wick.Depleted += turns.Reset;
        var lamp = new LampState(new GridCoord(2, 2));
        var state = new PlayerMovementState(new BoardState(5, 5), wick, turns, lamp, new GridCoord(2, 2));

        Assert.True(state.TryToggleLamp());
        Assert.True(state.TryMove(new GridCoord(1, 0)));
        Assert.True(state.TryMove(new GridCoord(-1, 0)));

        Assert.Equal(0, wick.CurrentWick);
        Assert.Equal(0, turns.CurrentTurn);
        Assert.True(lamp.IsCarried);
    }

    [Fact]
    public void TryMove_FinalWickDepletionReset_DoesNotLeaveStaleAdvancedTurn()
    {
        var wick = new WickState();
        wick.Initialize(1);
        var turns = new TurnState();
        wick.Depleted += turns.Reset;
        var lamp = new LampState(new GridCoord(2, 2));
        var state = new PlayerMovementState(new BoardState(5, 5), wick, turns, lamp, new GridCoord(2, 2));

        Assert.True(state.TryMove(new GridCoord(1, 0)));

        Assert.Equal(0, wick.CurrentWick);
        Assert.Equal(0, turns.CurrentTurn);
    }
}
