using System;
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
    private readonly GridCoord _cell;

    public OilFlaskState(WickState wick, ITurnAdvancer turns, GridCoord cell)
    {
        _wick = wick;
        _turns = turns;
        _cell = cell;
    }

    public event Action TurnSpent;
    public bool IsCollected { get; private set; }

    public bool IsAvailableAt(GridCoord playerCell) =>
        !IsCollected && playerCell == _cell && !_wick.IsDepleted;

    public bool TryBeginLift(GridCoord playerCell)
    {
        if (!IsAvailableAt(playerCell))
        {
            return false;
        }

        IsCollected = true;
        if (!_wick.Spend(1) || _wick.IsDepleted)
        {
            return true;
        }

        _turns.AdvanceTurn();
        TurnSpent?.Invoke();
        if (!_wick.Spend(1) || _wick.IsDepleted)
        {
            return true;
        }

        _turns.AdvanceTurn();
        TurnSpent?.Invoke();
        _wick.Restore(LevelTuning.FlaskRestore);
        return true;
    }
}

public partial class OilFlaskController : Node2D
{
    private static readonly Vector2 BoardOrigin = new(152, 104);
    private const int CellSize = 48;
    private WickSystem _wick;
    private TurnManager _turns;
    private PlayerController _player;
    private BoardRenderer _visibility;
    private Action _onTurnSpent;
    private Action _onLiftCompleted;
    private OilFlaskState _state;

    public void Configure(
        WickSystem wick,
        TurnManager turns,
        PlayerController player,
        BoardRenderer visibility,
        Action onTurnSpent,
        Action onLiftCompleted,
        GridCoord flaskCell)
    {
        _wick = wick;
        _turns = turns;
        _player = player;
        _visibility = visibility;
        _onTurnSpent = onTurnSpent;
        _onLiftCompleted = onLiftCompleted;
        _state = new OilFlaskState(_wick.State, _turns, flaskCell);
        _state.TurnSpent += OnTurnSpent;
        Position = BoardOrigin + new Vector2((flaskCell.X + 0.5f) * CellSize, (flaskCell.Y + 0.5f) * CellSize);
        Visible = true;
    }

    public bool TryBeginLift(GridCoord playerCell)
    {
        if (_wick is null || _turns is null || _player is null || _visibility is null || _state is null || playerCell != _player.Cell || !_state.IsAvailableAt(playerCell))
        {
            return false;
        }

        _player.LockInput();
        _visibility.ForceDarkness();
        var lifted = _state.TryBeginLift(playerCell);
        Visible = !_state.IsCollected;
        _onLiftCompleted?.Invoke();
        _player.UnlockInput();
        return lifted;
    }

    private void OnTurnSpent() => _onTurnSpent?.Invoke();
}
