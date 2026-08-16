using Wick.Actors;
using Wick.Board;
using Wick.Core;
using Wick.Data;
using Wick.Hazards;
using Wick.Pickups;
using Xunit;

namespace Wick.Tests;

public sealed class PracticeModeTests
{
    [Fact]
    public void FreshCampaignCopyAfterPracticeDiscardsFrozenFlaskGain()
    {
        var wick = new WickState();
        wick.StartFreshLevel(12, freezeDecrease: true);
        wick.Restore(LevelTuning.FlaskRestore);
        Assert.Equal(18, wick.CurrentWick);

        wick.StartFreshLevel(12, freezeDecrease: false);

        Assert.Equal(12, wick.CurrentWick);
        Assert.False(wick.IsDecreaseFrozen);
        Assert.False(wick.IsFlareActive);
    }

    [Fact]
    public void PracticeMode_FreezesWickDecreaseWhileTurnsStillAdvance()
    {
        var wick = new WickState();
        wick.Initialize(12);
        wick.SetDecreaseFrozen(true);
        var turns = new TurnState();
        var lamp = new LampState(new GridCoord(2, 2));
        var player = new PlayerMovementState(new BoardState(5, 5), wick, turns, lamp, new GridCoord(2, 2));

        Assert.True(player.TryMove(new GridCoord(1, 0)));

        Assert.Equal(12, wick.CurrentWick);
        Assert.Equal(1, turns.CurrentTurn);
        Assert.Equal(new GridCoord(3, 2), player.Cell);
    }

    [Fact]
    public void PracticeMode_OffPreservesCampaignWickCost()
    {
        var wick = new WickState();
        wick.Initialize(12);
        wick.SetDecreaseFrozen(false);
        var turns = new TurnState();
        var lamp = new LampState(new GridCoord(2, 2));
        var player = new PlayerMovementState(new BoardState(5, 5), wick, turns, lamp, new GridCoord(2, 2));

        Assert.True(player.TryMove(new GridCoord(1, 0)));

        Assert.Equal(11, wick.CurrentWick);
        Assert.Equal(1, turns.CurrentTurn);
    }

    [Fact]
    public void Undo_RestoresCompletePriorGameplaySnapshotAndIsConsumedAfterOneUse()
    {
        var board = new BoardState(7, 7);
        var visibility = new VisibilitySystem(board);
        var wick = new WickState();
        wick.Initialize(12);
        var turns = new TurnState();
        var lamp = new LampState(new GridCoord(1, 1));
        var player = new PlayerMovementState(board, wick, turns, lamp, new GridCoord(1, 1), visibility);
        var hazards = new HazardSimulationState(board, new[] { new GridCoord(5, 1) }, new[] { new GridCoord(5, 5) });
        var flasks = new OilFlaskCollectionState(wick, turns, new[] { new GridCoord(2, 1) });
        var practice = new PracticeModeState();
        practice.SetEnabled(true);
        var before = GameplaySnapshot.Capture(3, player, lamp, wick, turns, hazards, flasks, visibility, false, false);

        Assert.True(player.TryPlaceLamp());
        Assert.True(player.TryMove(new GridCoord(1, 0)));
        Assert.True(flasks.TryBeginLift(player.Cell));
        hazards.AdvanceTurn(visibility, lamp.Cell, wick.CurrentRadius);
        practice.RecordSuccessfulAction(before);

        Assert.True(practice.TryTakeUndo(out var undo));
        var flags = undo.Restore(player, lamp, wick, turns, hazards, flasks, visibility);

        Assert.Equal(3, flags.LevelIndex);
        Assert.False(flags.IsTerminal);
        Assert.False(flags.IsCampaignComplete);
        Assert.Equal(new GridCoord(1, 1), player.Cell);
        Assert.True(lamp.IsCarried);
        Assert.Equal(new GridCoord(1, 1), lamp.Cell);
        Assert.Equal(12, wick.CurrentWick);
        Assert.False(wick.IsFlareActive);
        Assert.Equal(0, turns.CurrentTurn);
        Assert.Equal(new GridCoord(5, 1), Assert.Single(hazards.CrawlerCells));
        Assert.Equal(new GridCoord(5, 5), Assert.Single(hazards.MothCells));
        Assert.False(flasks.IsCollected(new GridCoord(2, 1)));
        Assert.True(visibility.IsVisible(new GridCoord(1, 1)));
        Assert.False(practice.TryTakeUndo(out _));
    }

    [Fact]
    public void DisablingPracticeMode_ClearsPendingUndo()
    {
        var practice = new PracticeModeState();
        practice.SetEnabled(true);
        practice.RecordSuccessfulAction(GameplaySnapshot.Empty);

        practice.SetEnabled(false);

        Assert.False(practice.TryTakeUndo(out _));
    }

    [Fact]
    public void MainCampaign_DoesNotRecordOrExposeUndo()
    {
        var practice = new PracticeModeState();

        practice.RecordSuccessfulAction(GameplaySnapshot.Empty);

        Assert.False(practice.TryTakeUndo(out _));
    }
}
