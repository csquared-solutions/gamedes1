using Godot;
using Wick.Hazards;

namespace Wick.Ui;

public partial class HazardStateIndicatorController : Control
{
    private Label _label;

    public override void _Ready() => _label = GetNode<Label>("Label");

    public void SetState(string hazardName, HazardMotionState state)
    {
        _label.Text = $"{hazardName}: {state.ToString().ToUpperInvariant()}";
        Modulate = state switch
        {
            HazardMotionState.Stalled => new Color(0.64f, 0.72f, 0.82f),
            HazardMotionState.Slowed => new Color(1f, 0.78f, 0.32f),
            _ => new Color(1f, 0.43f, 0.32f)
        };
    }
}
