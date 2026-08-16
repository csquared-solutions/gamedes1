using System;

namespace Wick.Core;

public readonly record struct CampaignLevelTransition(
    int LevelIndex,
    bool StartsFreshLevelCopy,
    bool StartsNewRun);

public sealed class CampaignSessionState
{
    private readonly CampaignProgressState _main;
    private readonly CampaignProgressState _practice;

    public CampaignSessionState(int levelCount)
    {
        _main = new CampaignProgressState(levelCount);
        _practice = new CampaignProgressState(levelCount);
    }

    public bool IsPracticeMode { get; private set; }
    public int MainLevelIndex => _main.CurrentLevelIndex;
    public int ActiveLevelIndex => ActiveProgress.CurrentLevelIndex;
    public bool IsActiveComplete => ActiveProgress.IsComplete;

    public CampaignLevelTransition SetPracticeMode(bool enabled)
    {
        if (enabled && !IsPracticeMode)
        {
            _practice.Restore(_main.CurrentLevelIndex, isComplete: false);
        }

        IsPracticeMode = enabled;
        return FreshLevel(ActiveLevelIndex);
    }

    public bool AdvanceAfterClear() => ActiveProgress.AdvanceAfterClear();

    public CampaignLevelTransition RestartCurrentLevel() => FreshLevel(ActiveLevelIndex);

    public CampaignLevelTransition HandleRestartRequest() =>
        !IsPracticeMode && _main.IsComplete ? RestartRun() : RestartCurrentLevel();

    public CampaignLevelTransition RestartRun()
    {
        IsPracticeMode = false;
        _main.RestartRun();
        _practice.RestartRun();
        return new CampaignLevelTransition(_main.CurrentLevelIndex, StartsFreshLevelCopy: true, StartsNewRun: true);
    }

    public void LoadMainLevel(int levelIndex)
    {
        IsPracticeMode = false;
        _main.Restore(levelIndex, isComplete: false);
    }

    public void RestoreActive(int levelIndex, bool isComplete) =>
        ActiveProgress.Restore(levelIndex, isComplete);

    private CampaignProgressState ActiveProgress => IsPracticeMode ? _practice : _main;

    private static CampaignLevelTransition FreshLevel(int levelIndex) =>
        new(levelIndex, StartsFreshLevelCopy: true, StartsNewRun: false);
}
