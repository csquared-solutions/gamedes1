using Godot;
using Wick.Actors;
using Wick.Core;
using Wick.Hazards;
using Wick.Pickups;

namespace Wick.Ui;

public partial class HudController : CanvasLayer
{
    private static readonly Color AlertColor = new(1f, 0.45f, 0.3f);
    private static readonly Color HintColor = new(0.82f, 0.79f, 0.69f);
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
    private OilFlaskController _oilFlask;

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
        _lossCause = GetNode<Label>("Footer/Panel/Rows/LossCause");
        _campaignStatus = GetNode<Label>("Margin/Panel/Content/CampaignStatus");
        _practiceStatus = GetNode<Label>("Footer/Panel/Rows/Row/PracticeStatus");
        _controls = GetNode<Label>("Footer/Panel/Rows/Row/Controls");
        _oilFlask = GetNode<OilFlaskController>("../OilFlask");
        var events = GetNode<GameEvents>("../GameEvents");
        events.Connect(GameEvents.SignalName.LightOut, Callable.From<string>(ShowLossCause));
        events.Connect(GameEvents.SignalName.LevelCleared, Callable.From<int, int>(ShowLevelCleared));
    }

    public override void _Process(double delta)
    {
        _gauge.SetWick(_oilFlask.DisplayedWick, _oilFlask.DisplayedRadius);
        _returnCost.Text = $"RETURN COST { _tether.GetReturnCost() }";
        UpdateHazardState(_crawler, "CRAWLER", _hazards.CrawlerState, _hazards.IsCrawlerVisible);
        UpdateHazardState(_moth, "MOTH", _hazards.MothState, _hazards.IsMothVisible);
        var presentation = HudPresentationState.Create(new HudPresentationInput(
            _levels.CurrentLevelIndex,
            _levels.IsRunComplete,
            _levels.ReportedRemainingWick,
            _levels.IsPracticeMode,
            _levels.CanUndo,
            _levels.IsExitBlockedByCarriedLamp));
        _campaignStatus.Text = presentation.CampaignStatus;
        _practiceStatus.Text = presentation.PracticeStatus;
        _controls.Text = presentation.Controls;
        if (!_levels.IsTerminal || _levels.IsRunComplete)
        {
            var isAlert = presentation.ExitPrompt.Length > 0;
            _lossCause.Text = isAlert ? presentation.ExitPrompt : presentation.Hint;
            _lossCause.Modulate = isAlert ? AlertColor : HintColor;
        }
    }

    private void ShowLossCause(string cause)
    {
        _lossCause.Text = $"LIGHT OUT: {cause.ToUpperInvariant()}";
        _lossCause.Modulate = AlertColor;
    }

    private void ShowLevelCleared(int levelIndex, int remainingWick)
    {
        _lossCause.Text = $"STAIR REACHED WITH {remainingWick} WICK LEFT";
        _lossCause.Modulate = new Color(1f, 0.902f, 0.639f);
    }

    private static void UpdateHazardState(HazardStateIndicatorController indicator, string name, HazardMotionState state, bool isVisible)
    {
        // Fade instead of hiding so the status row never reflows.
        if (isVisible)
        {
            indicator.SetState(name, state);
        }
        else
        {
            indicator.Modulate = Colors.Transparent;
        }
    }
}
