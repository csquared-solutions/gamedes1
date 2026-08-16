using System;

namespace Wick.Ui;

public static class WickGaugeVisualState
{
    public static float GetBrightness(int wick, float mothPulseRemaining, float phase)
    {
        if (mothPulseRemaining > 0f)
        {
            return 1.08f + 0.12f * MathF.Abs(MathF.Sin(phase * 14f));
        }

        return wick is > 0 and <= 3
            ? 0.62f + 0.38f * MathF.Abs(MathF.Sin(phase * 10f))
            : 1f;
    }
}
