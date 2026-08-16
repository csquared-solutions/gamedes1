using Wick.Actors;
using Wick.Board;
using Wick.Hazards;
using Wick.Pickups;

namespace Wick.Core;

public sealed record GameplaySnapshot(
    int LevelIndex,
    GridCoord PlayerCell,
    LampSnapshot Lamp,
    WickSnapshot Wick,
    int Turn,
    HazardSimulationSnapshot Hazards,
    OilFlaskCollectionSnapshot Flasks,
    VisibilitySnapshot Visibility,
    bool IsTerminal,
    bool IsCampaignComplete,
    int ReportedRemainingWick)
{
    public static GameplaySnapshot Empty { get; } = new(
        0,
        default,
        new LampSnapshot(default, true),
        new WickSnapshot(0, 0, false),
        0,
        new HazardSimulationSnapshot([], [], [], [], false),
        new OilFlaskCollectionSnapshot([]),
        new VisibilitySnapshot([], []),
        false,
        false,
        -1);

    public static GameplaySnapshot Capture(
        int levelIndex,
        PlayerMovementState player,
        LampState lamp,
        WickState wick,
        TurnState turns,
        HazardSimulationState hazards,
        OilFlaskCollectionState flasks,
        VisibilitySystem visibility,
        bool isTerminal,
        bool isCampaignComplete,
        int reportedRemainingWick = -1) => new(
            levelIndex,
            player.Cell,
            lamp.CaptureSnapshot(),
            wick.CaptureSnapshot(),
            turns.CurrentTurn,
            hazards.CaptureSnapshot(),
            flasks.CaptureSnapshot(),
            visibility.CaptureSnapshot(),
            isTerminal,
            isCampaignComplete,
            reportedRemainingWick);

    public GameplaySnapshotFlags Restore(
        PlayerMovementState player,
        LampState lamp,
        WickState wick,
        TurnState turns,
        HazardSimulationState hazards,
        OilFlaskCollectionState flasks,
        VisibilitySystem visibility)
    {
        player.RestoreCell(PlayerCell);
        lamp.RestoreSnapshot(Lamp);
        wick.RestoreSnapshot(Wick);
        turns.Restore(Turn);
        hazards.RestoreSnapshot(Hazards);
        flasks.RestoreSnapshot(Flasks);
        visibility.RestoreSnapshot(Visibility);
        return new GameplaySnapshotFlags(LevelIndex, IsTerminal, IsCampaignComplete, ReportedRemainingWick);
    }
}

public readonly record struct GameplaySnapshotFlags(
    int LevelIndex,
    bool IsTerminal,
    bool IsCampaignComplete,
    int ReportedRemainingWick);
