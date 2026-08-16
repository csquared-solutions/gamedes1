using Wick.Core;
using Xunit;

namespace Wick.Tests;

public sealed class CampaignProgressionTests
{
    [Fact]
    public void PracticeProgressionIsIsolatedAndEachModeSwitchRequestsAFreshMainLevelCopy()
    {
        var session = new CampaignSessionState(levelCount: 7);
        Assert.True(session.AdvanceAfterClear());
        Assert.Equal(2, session.MainLevelIndex);

        var enterPractice = session.SetPracticeMode(true);
        Assert.Equal(2, enterPractice.LevelIndex);
        Assert.True(enterPractice.StartsFreshLevelCopy);
        Assert.False(enterPractice.StartsNewRun);
        Assert.True(session.IsPracticeMode);
        Assert.Equal(2, session.ActiveLevelIndex);
        Assert.True(session.AdvanceAfterClear());
        Assert.Equal(3, session.ActiveLevelIndex);
        Assert.Equal(2, session.MainLevelIndex);

        var leavePractice = session.SetPracticeMode(false);
        Assert.Equal(2, leavePractice.LevelIndex);
        Assert.True(leavePractice.StartsFreshLevelCopy);
        Assert.False(leavePractice.StartsNewRun);
        Assert.False(session.IsPracticeMode);
        Assert.Equal(2, session.ActiveLevelIndex);
        Assert.Equal(2, session.MainLevelIndex);
    }

    [Fact]
    public void CurrentLevelRestartPreservesCampaignProgressWhileDeliberateNewRunResetsIt()
    {
        var session = new CampaignSessionState(levelCount: 7);
        Assert.True(session.AdvanceAfterClear());
        Assert.True(session.AdvanceAfterClear());

        var currentLevelRestart = session.HandleRestartRequest();
        Assert.Equal(3, currentLevelRestart.LevelIndex);
        Assert.True(currentLevelRestart.StartsFreshLevelCopy);
        Assert.False(currentLevelRestart.StartsNewRun);
        Assert.Equal(3, session.MainLevelIndex);

        var newRun = session.RestartRun();
        Assert.Equal(1, newRun.LevelIndex);
        Assert.True(newRun.StartsFreshLevelCopy);
        Assert.True(newRun.StartsNewRun);
        Assert.Equal(1, session.MainLevelIndex);
    }

    [Fact]
    public void RestartRequestAfterCampaignCompletionStartsDeliberateNewRun()
    {
        var session = new CampaignSessionState(levelCount: 2);
        Assert.True(session.AdvanceAfterClear());
        Assert.False(session.AdvanceAfterClear());

        var transition = session.HandleRestartRequest();

        Assert.True(transition.StartsNewRun);
        Assert.Equal(1, transition.LevelIndex);
        Assert.Equal(1, session.MainLevelIndex);
    }

    [Fact]
    public void ClearingSevenLevelsAdvancesInOrderAndEndsInCompletionState()
    {
        var progress = new CampaignProgressState(levelCount: 7);

        for (var expectedLevel = 2; expectedLevel <= 7; expectedLevel++)
        {
            Assert.True(progress.AdvanceAfterClear());
            Assert.Equal(expectedLevel, progress.CurrentLevelIndex);
            Assert.False(progress.IsComplete);
        }

        Assert.False(progress.AdvanceAfterClear());
        Assert.Equal(7, progress.CurrentLevelIndex);
        Assert.True(progress.IsComplete);
    }
}
