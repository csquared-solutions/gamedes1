using Godot;
using Wick.Hazards;

namespace Wick.Ui;

public static class HazardStateColor
{
    public static Color For(HazardMotionState state) => state switch
    {
        HazardMotionState.Stalled => new Color(0.64f, 0.72f, 0.82f),
        HazardMotionState.Slowed => new Color(1f, 0.78f, 0.32f),
        _ => new Color(1f, 0.43f, 0.32f)
    };
}
