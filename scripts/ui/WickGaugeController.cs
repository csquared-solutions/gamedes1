using Godot;

namespace Wick.Ui;

public partial class WickGaugeController : Control
{
    private Label _readout;
    private Label _warning;
    private int _lastRadius = -1;
    private float _pulseTime;
    private float _phase;
    private int _wick;

    public override void _Ready()
    {
        _readout = GetNode<Label>("Readout");
        _warning = GetNode<Label>("Warning");
    }

    public override void _Process(double delta)
    {
        _phase += (float)delta;
        _pulseTime = Mathf.Max(0f, _pulseTime - (float)delta);
        var brightness = WickGaugeVisualState.GetBrightness(_wick, _pulseTime, _phase);
        Modulate = new Color(brightness, brightness * 0.9f, brightness * 0.62f);
    }

    public void SetWick(int wick, int radius)
    {
        _wick = wick;
        _readout.Text = $"WICK {wick}   SIGHT {radius}";
        _warning.Text = wick is > 0 and <= 3 ? "FINAL THREE WICK" : string.Empty;

        if (_lastRadius >= 0 && _lastRadius < 5 && radius >= 5)
        {
            _pulseTime = 0.55f;
        }

        _lastRadius = radius;
    }
}
