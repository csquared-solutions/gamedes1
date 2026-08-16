using Godot;
using Wick.Actors;
using Wick.Core;
using Wick.Hazards;

namespace Wick.Ui;

public partial class HudController : CanvasLayer
{
    private WickSystem _wick;
    private HazardSystem _hazards;
    private LampTetherController _tether;
    private WickGaugeController _gauge;
    private HazardStateIndicatorController _crawler;
    private HazardStateIndicatorController _moth;
    private Label _returnCost;
    private Label _lossCause;

    public override void _Ready()
    {
        _wick = GetNode<WickSystem>("../WickSystem");
        _hazards = GetNode<HazardSystem>("../HazardSystem");
        _tether = GetNode<LampTetherController>("../LampTether");
        _gauge = GetNode<WickGaugeController>("Margin/Panel/Content/WickGauge");
        _crawler = GetNode<HazardStateIndicatorController>("Margin/Panel/Content/CrawlerState");
        _moth = GetNode<HazardStateIndicatorController>("Margin/Panel/Content/MothState");
        _returnCost = GetNode<Label>("Margin/Panel/Content/ReturnCost");
        _lossCause = GetNode<Label>("Margin/Panel/Content/LossCause");
        GetNode<GameEvents>("../GameEvents").Connect(GameEvents.SignalName.LightOut, Callable.From<string>(ShowLossCause));
    }

    public override void _Process(double delta)
    {
        _gauge.SetWick(_wick.CurrentWick, _wick.CurrentRadius);
        _returnCost.Text = $"RETURN COST { _tether.GetReturnCost() }";
        UpdateHazardState(_crawler, "CRAWLER", _hazards.CrawlerState, _hazards.IsCrawlerVisible);
        UpdateHazardState(_moth, "MOTH", _hazards.MothState, _hazards.IsMothVisible);
    }

    private void ShowLossCause(string cause) => _lossCause.Text = $"LIGHT OUT: {cause.ToUpperInvariant()}";

    private static void UpdateHazardState(HazardStateIndicatorController indicator, string name, HazardMotionState state, bool isVisible)
    {
        indicator.Visible = isVisible;
        if (isVisible)
        {
            indicator.SetState(name, state);
        }
    }
}
