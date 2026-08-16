using System;

namespace Wick.Audio;

[Flags]
public enum AudioCue
{
    None = 0,
    MothWake = 1,
    LowWick = 2
}

public sealed class AudioCueState
{
    private bool _mothsAwake;
    private bool _lowWick;

    public AudioCue Update(int wick, int radius, bool hasMoths)
    {
        var mothsAwake = hasMoths && radius >= 5;
        var lowWick = wick is > 0 and <= 3;
        var cue = AudioCue.None;

        if (mothsAwake && !_mothsAwake)
        {
            cue |= AudioCue.MothWake;
        }

        if (lowWick && !_lowWick)
        {
            cue |= AudioCue.LowWick;
        }

        _mothsAwake = mothsAwake;
        _lowWick = lowWick;
        return cue;
    }

    public void Reset()
    {
        _mothsAwake = false;
        _lowWick = false;
    }
}
