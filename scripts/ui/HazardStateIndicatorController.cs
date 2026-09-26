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
        Modulate = HazardStateColor.For(state);
    }
}
