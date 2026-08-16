using Wick.Audio;
using Xunit;

namespace Wick.Tests;

public sealed class AudioCueStateTests
{
    [Fact]
    public void Update_PlaysMothWakeOnlyWhenMothsEnterTheBrightBand()
    {
        var state = new AudioCueState();

        Assert.Equal(AudioCue.None, state.Update(wick: 8, radius: 4, hasMoths: true));
        Assert.Equal(AudioCue.MothWake, state.Update(wick: 10, radius: 5, hasMoths: true));
        Assert.Equal(AudioCue.None, state.Update(wick: 12, radius: 6, hasMoths: true));
        Assert.Equal(AudioCue.None, state.Update(wick: 8, radius: 4, hasMoths: true));
        Assert.Equal(AudioCue.MothWake, state.Update(wick: 10, radius: 5, hasMoths: true));
    }

    [Fact]
    public void Update_DoesNotPlayMothWakeWhenTheLevelHasNoMoths()
    {
        var state = new AudioCueState();

        Assert.Equal(AudioCue.None, state.Update(wick: 12, radius: 6, hasMoths: false));
    }

    [Fact]
    public void Update_PlaysLowWickOnceOnEnteringTheFinalThree()
    {
        var state = new AudioCueState();

        Assert.Equal(AudioCue.None, state.Update(wick: 4, radius: 2, hasMoths: false));
        Assert.Equal(AudioCue.LowWick, state.Update(wick: 3, radius: 1, hasMoths: false));
        Assert.Equal(AudioCue.None, state.Update(wick: 2, radius: 1, hasMoths: false));
        Assert.Equal(AudioCue.None, state.Update(wick: 7, radius: 3, hasMoths: false));
        Assert.Equal(AudioCue.LowWick, state.Update(wick: 3, radius: 1, hasMoths: false));
    }

    [Fact]
    public void Reset_AllowsActiveCuesToBeReportedForTheNextLevel()
    {
        var state = new AudioCueState();
        state.Update(wick: 12, radius: 6, hasMoths: true);

        state.Reset();

        Assert.Equal(AudioCue.MothWake, state.Update(wick: 12, radius: 6, hasMoths: true));
    }
}
