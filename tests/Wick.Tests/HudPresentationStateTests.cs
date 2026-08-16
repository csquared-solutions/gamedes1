using Wick.Ui;
using Xunit;

namespace Wick.Tests;

public sealed class HudPresentationStateTests
{
    [Fact]
    public void CampaignMode_PresentsLevelReportAndKeyboardPracticeControl()
    {
        var presentation = HudPresentationState.Create(new HudPresentationInput(
            LevelIndex: 4,
            IsRunComplete: false,
            ReportedRemainingWick: 5,
            IsPracticeMode: false,
            CanUndo: false));

        Assert.Equal("LEVEL 4/7 | LAST CLEAR: 5 WICK", presentation.CampaignStatus);
        Assert.Equal("CAMPAIGN MODE | P PRACTICE", presentation.PracticeStatus);
        Assert.Equal("P PRACTICE | Z UNDO | E SET LAMP | F FLARE | R RESTART", presentation.Controls);
    }

    [Theory]
    [InlineData(false, "EMPTY")]
    [InlineData(true, "READY")]
    public void PracticeMode_PresentsFrozenWickAndObservableUndoReadiness(bool canUndo, string undoState)
    {
        var presentation = HudPresentationState.Create(new HudPresentationInput(
            LevelIndex: 2,
            IsRunComplete: false,
            ReportedRemainingWick: -1,
            IsPracticeMode: true,
            CanUndo: canUndo));

        Assert.Equal("LEVEL 2/7", presentation.CampaignStatus);
        Assert.Equal($"PRACTICE MODE | WICK FROZEN | Z UNDO {undoState}", presentation.PracticeStatus);
    }

    [Fact]
    public void CompletedRun_PresentsFinalRemainingWick()
    {
        var presentation = HudPresentationState.Create(new HudPresentationInput(
            LevelIndex: 7,
            IsRunComplete: true,
            ReportedRemainingWick: 3,
            IsPracticeMode: false,
            CanUndo: false));

        Assert.Equal("RUN COMPLETE | LEVEL 7 CLEAR: 3 WICK", presentation.CampaignStatus);
    }
}
