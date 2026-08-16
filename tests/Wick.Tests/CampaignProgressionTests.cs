using Wick.Core;
using Xunit;

namespace Wick.Tests;

public sealed class CampaignProgressionTests
{
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
