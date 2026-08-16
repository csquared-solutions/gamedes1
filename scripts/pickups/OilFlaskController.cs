using System;
using System.Threading.Tasks;
using Godot;
using Wick.Actors;
using Wick.Board;
using Wick.Core;
using Wick.Data;

namespace Wick.Pickups;

public sealed class OilFlaskState
{
    private readonly WickState _wick;
    private readonly ITurnAdvancer _turns;

    public OilFlaskState(WickState wick, ITurnAdvancer turns)
    {
        _wick = wick;
        _turns = turns;
    }

    public event Action TurnSpent;

    public void BeginLift()
    {
        _wick.Spend(1);
        _turns.AdvanceTurn();
        TurnSpent?.Invoke();
        _wick.Spend(1);
        _turns.AdvanceTurn();
        TurnSpent?.Invoke();
        _wick.Restore(LevelTuning.FlaskRestore);
    }
}

public partial class OilFlaskController : Node
{
    private WickSystem _wick;
    private TurnManager _turns;
    private PlayerController _player;
    private BoardRenderer _visibility;
    private Action _onTurnSpent;
    private Action _onLiftCompleted;

    public void Configure(
        WickSystem wick,
        TurnManager turns,
        PlayerController player,
        BoardRenderer visibility,
        Action onTurnSpent,
        Action onLiftCompleted)
    {
        _wick = wick;
        _turns = turns;
        _player = player;
        _visibility = visibility;
        _onTurnSpent = onTurnSpent;
        _onLiftCompleted = onLiftCompleted;
    }

    public async Task BeginLift()
    {
        if (_wick is null || _turns is null || _player is null || _visibility is null)
        {
            return;
        }

        _player.LockInput();
        _visibility.ForceDarkness();

        var state = new OilFlaskState(_wick.State, _turns);
        state.TurnSpent += OnTurnSpent;
        state.BeginLift();
        state.TurnSpent -= OnTurnSpent;

        _onLiftCompleted?.Invoke();
        _player.UnlockInput();
        await Task.CompletedTask;
    }

    private void OnTurnSpent() => _onTurnSpent?.Invoke();
}
