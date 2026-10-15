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
        Assert.Equal("ARROWS/WASD MOVE | E LAMP | F FLARE | R RESTART", presentation.Controls);
    }

    [Theory]
    [InlineData(1, "EVERY STEP BURNS 1 WICK")]
    [InlineData(2, "MOTHS WAKE")]
    [InlineData(3, "SET THE LAMP DOWN (E) BEFORE YOU STEP ONTO THE STAIR")]
    [InlineData(7, "PRACTICE MODE")]
    public void EachLevel_ShowsAHintForTheRuleItIntroduces(int level, string expected)
    {
        var presentation = HudPresentationState.Create(new HudPresentationInput(
            LevelIndex: level,
            IsRunComplete: false,
            ReportedRemainingWick: -1,
            IsPracticeMode: false,
            CanUndo: false));

        Assert.Contains(expected, presentation.Hint);
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
        Assert.Equal("RUN COMPLETE. PRESS R TO PLAY AGAIN.", presentation.Hint);
    }

    [Fact]
    public void CarriedLampAtRequiredExit_ExplainsHowToAdvance()
    {
        var presentation = HudPresentationState.Create(new HudPresentationInput(
            LevelIndex: 3,
            IsRunComplete: false,
            ReportedRemainingWick: -1,
            IsPracticeMode: false,
            CanUndo: false,
            IsExitBlockedByCarriedLamp: true));

        Assert.Equal("EXIT BLOCKED: STEP BACK, PRESS E TO SET LAMP, THEN RETURN", presentation.ExitPrompt);
    }
}
