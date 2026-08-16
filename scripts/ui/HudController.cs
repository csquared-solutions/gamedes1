using Godot;
using Wick.Actors;
using Wick.Core;
using Wick.Hazards;

namespace Wick.Ui;

public partial class HudController : CanvasLayer
{
    private WickSystem _wick;
    private LevelManager _levels;
    private HazardSystem _hazards;
    private LampTetherController _tether;
    private WickGaugeController _gauge;
    private HazardStateIndicatorController _crawler;
    private HazardStateIndicatorController _moth;
    private Label _returnCost;
    private Label _lossCause;
    private Label _campaignStatus;
    private Label _practiceStatus;
    private Label _controls;

    public override void _Ready()
    {
        _wick = GetNode<WickSystem>("../WickSystem");
        _levels = GetNode<LevelManager>("../LevelManager");
        _hazards = GetNode<HazardSystem>("../HazardSystem");
        _tether = GetNode<LampTetherController>("../LampTether");
        _gauge = GetNode<WickGaugeController>("Margin/Panel/Content/WickGauge");
        _crawler = GetNode<HazardStateIndicatorController>("Margin/Panel/Content/CrawlerState");
        _moth = GetNode<HazardStateIndicatorController>("Margin/Panel/Content/MothState");
        _returnCost = GetNode<Label>("Margin/Panel/Content/ReturnCost");
        _lossCause = GetNode<Label>("Margin/Panel/Content/LossCause");
        _campaignStatus = GetNode<Label>("Margin/Panel/Content/CampaignStatus");
        _practiceStatus = GetNode<Label>("Margin/Panel/Content/PracticeStatus");
        _controls = GetNode<Label>("Margin/Panel/Content/Controls");
        GetNode<GameEvents>("../GameEvents").Connect(GameEvents.SignalName.LightOut, Callable.From<string>(ShowLossCause));
    }

    public override void _Process(double delta)
    {
        _gauge.SetWick(_wick.CurrentWick, _wick.CurrentRadius);
        _returnCost.Text = $"RETURN COST { _tether.GetReturnCost() }";
        UpdateHazardState(_crawler, "CRAWLER", _hazards.CrawlerState, _hazards.IsCrawlerVisible);
        UpdateHazardState(_moth, "MOTH", _hazards.MothState, _hazards.IsMothVisible);
        var presentation = HudPresentationState.Create(new HudPresentationInput(
            _levels.CurrentLevelIndex,
            _levels.IsRunComplete,
            _levels.ReportedRemainingWick,
            _levels.IsPracticeMode,
            _levels.CanUndo));
        _campaignStatus.Text = presentation.CampaignStatus;
        _practiceStatus.Text = presentation.PracticeStatus;
        _controls.Text = presentation.Controls;
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
