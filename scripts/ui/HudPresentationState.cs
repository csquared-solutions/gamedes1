namespace Wick.Ui;

public readonly record struct HudPresentationInput(
    int LevelIndex,
    bool IsRunComplete,
    int ReportedRemainingWick,
    bool IsPracticeMode,
    bool CanUndo,
    bool IsExitBlockedByCarriedLamp = false);

public readonly record struct HudPresentation(
    string CampaignStatus,
    string PracticeStatus,
    string Controls,
    string ExitPrompt);

public static class HudPresentationState
{
    public static HudPresentation Create(HudPresentationInput input)
    {
        var campaignStatus = input.IsRunComplete
            ? $"RUN COMPLETE | LEVEL 7 CLEAR: {input.ReportedRemainingWick} WICK"
            : $"LEVEL {input.LevelIndex}/7" + (input.ReportedRemainingWick >= 0
                ? $" | LAST CLEAR: {input.ReportedRemainingWick} WICK"
                : string.Empty);
        var practiceStatus = input.IsPracticeMode
            ? $"PRACTICE MODE | WICK FROZEN | Z UNDO {(input.CanUndo ? "READY" : "EMPTY")}"
            : "CAMPAIGN MODE | P PRACTICE";
        return new HudPresentation(
            campaignStatus,
            practiceStatus,
            "P PRACTICE | Z UNDO | E SET LAMP | F FLARE | R RESTART",
            input.IsExitBlockedByCarriedLamp
                ? "EXIT BLOCKED: STEP BACK, PRESS E TO SET LAMP, THEN RETURN"
                : string.Empty);
    }
}
