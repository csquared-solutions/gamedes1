using System;

namespace Wick.Core;

public sealed class CampaignProgressState
{
    private readonly int _levelCount;

    public CampaignProgressState(int levelCount)
    {
        if (levelCount < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(levelCount));
        }

        _levelCount = levelCount;
        CurrentLevelIndex = 1;
    }

    public int CurrentLevelIndex { get; private set; }
    public bool IsComplete { get; private set; }

    public bool AdvanceAfterClear()
    {
        if (IsComplete)
        {
            return false;
        }

        if (CurrentLevelIndex == _levelCount)
        {
            IsComplete = true;
            return false;
        }

        CurrentLevelIndex++;
        return true;
    }

    public void RestartRun()
    {
        CurrentLevelIndex = 1;
        IsComplete = false;
    }

    public void Restore(int levelIndex, bool isComplete)
    {
        if (levelIndex < 1 || levelIndex > _levelCount)
        {
            throw new ArgumentOutOfRangeException(nameof(levelIndex));
        }

        CurrentLevelIndex = levelIndex;
        IsComplete = isComplete;
    }
}
