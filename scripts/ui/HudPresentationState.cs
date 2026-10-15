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
    string ExitPrompt,
    string Hint);

public static class HudPresentationState
{
    private static readonly string[] LevelHints =
    {
        "FIND THE STAIR. EVERY STEP BURNS 1 WICK, AND LESS WICK MEANS LESS LIGHT.",
        "MOTHS WAKE WHEN YOUR SIGHT IS 5 OR MORE, THEN FLY AT YOUR LAMP.",
        "FROM HERE ON, SET THE LAMP DOWN (E) BEFORE YOU STEP ONTO THE STAIR.",
        "A FLARE (F) DOUBLES YOUR LIGHT FOR ONE TURN AND COSTS 3 WICK.",
        "CRAWLERS RUSH IN THE DARK BUT SLOW DOWN INSIDE YOUR LIGHT.",
        "AN OIL FLASK GIVES 6 WICK, BUT A BRIGHTER LAMP WAKES THE MOTHS.",
        "LAST LEVEL. STUCK? PRESS P FOR PRACTICE MODE WITH UNDO.",
    };

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
            "ARROWS/WASD MOVE | E LAMP | F FLARE | R RESTART",
            input.IsExitBlockedByCarriedLamp
                ? "EXIT BLOCKED: STEP BACK, PRESS E TO SET LAMP, THEN RETURN"
                : string.Empty,
            input.IsRunComplete
                ? "RUN COMPLETE. PRESS R TO PLAY AGAIN."
                : LevelHints[System.Math.Clamp(input.LevelIndex, 1, LevelHints.Length) - 1]);
    }
}
