using Godot;

namespace Wick.Ui;

public partial class WickGaugeController : Control
{
    private Label _readout;
    private Label _warning;
    private int _lastRadius = -1;
    private float _pulseTime;

    public override void _Ready()
    {
        _readout = GetNode<Label>("Readout");
        _warning = GetNode<Label>("Warning");
    }

    public override void _Process(double delta)
    {
        if (_pulseTime <= 0f)
        {
            return;
        }

        _pulseTime -= (float)delta;
        Modulate = new Color(1f, 0.75f + 0.25f * Mathf.Abs(Mathf.Sin(_pulseTime * 14f)), 0.45f);
        if (_pulseTime <= 0f)
        {
            Modulate = Colors.White;
        }
    }

    public void SetWick(int wick, int radius)
    {
        _readout.Text = $"WICK {wick}   SIGHT {radius}";
        _warning.Text = wick <= 3 ? "FINAL THREE WICK" : string.Empty;

        if (_lastRadius >= 0 && ((_lastRadius < 5 && radius >= 5) || (_lastRadius >= 5 && radius < 5)))
        {
            _pulseTime = 0.55f;
        }

        _lastRadius = radius;
    }
}
