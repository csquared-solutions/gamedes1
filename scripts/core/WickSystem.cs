using Godot;

namespace Wick.Core;

public partial class WickSystem : Node, IPlayerWick
{
    private readonly WickState _state = new();
    private GameEvents _events;

    public WickState State => _state;
    public int CurrentWick => _state.CurrentWick;
    public int CurrentRadius => _state.CurrentRadius;
    public bool IsDepleted => _state.IsDepleted;
    public bool IsFlareActive => _state.IsFlareActive;

    public override void _Ready()
    {
        _events = GetNodeOrNull<GameEvents>("../GameEvents");
        _state.WickChanged += OnWickChanged;
        _state.Depleted += OnDepleted;
    }

    public override void _ExitTree()
    {
        _state.WickChanged -= OnWickChanged;
        _state.Depleted -= OnDepleted;
        _events = null;
    }

    public void Initialize(int startingWick) => _state.Initialize(startingWick);

    public void StartFreshLevel(int startingWick, bool freezeDecrease) =>
        _state.StartFreshLevel(startingWick, freezeDecrease);

    public bool Spend(int amount) => _state.Spend(amount);

    public void Restore(int amount) => _state.Restore(amount);

    public bool TryFireFlare() => _state.TryFireFlare();

    public void AdvanceTurn() => _state.AdvanceTurn();

    public void SetDecreaseFrozen(bool frozen) => _state.SetDecreaseFrozen(frozen);

    private void OnWickChanged(int wickValue)
    {
        _events?.EmitSignal(GameEvents.SignalName.WickChanged, wickValue);
    }

    private void OnDepleted()
    {
        _events?.EmitSignal(GameEvents.SignalName.LightOut, "depleted");
    }
}
