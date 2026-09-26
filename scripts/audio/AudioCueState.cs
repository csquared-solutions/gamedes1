using System;

namespace Wick.Audio;

[Flags]
public enum AudioCue
{
    None = 0,
    MothWake = 1,
    LowWick = 2,
    LightOut = 4
}

public sealed class AudioCueState
{
    private bool _mothsAwake;
    private bool _lowWick;
    private bool _lightOutReported;

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

    public AudioCue ReportLightOut()
    {
        if (_lightOutReported)
        {
            return AudioCue.None;
        }

        _lightOutReported = true;
        return AudioCue.LightOut;
    }

    public void Reset()
    {
        _mothsAwake = false;
        _lowWick = false;
        _lightOutReported = false;
    }
}
