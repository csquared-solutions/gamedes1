using System;
using Godot;
using Wick.Board;
using Wick.Core;

namespace Wick.Actors;

public sealed class PlayerMovementState
{
    private readonly BoardState _board;
    private readonly IPlayerWick _wick;
    private readonly ITurnAdvancer _turns;
    private readonly LampState _lamp;
    private readonly VisibilitySystem _visibility;

    public PlayerMovementState(
        BoardState board,
        IPlayerWick wick,
        ITurnAdvancer turns,
        LampState lamp,
        GridCoord startCell,
        VisibilitySystem visibility = null)
    {
        _board = board;
        _wick = wick;
        _turns = turns;
        _lamp = lamp;
        _visibility = visibility;
        Cell = startCell;
        RefreshVisibility();
    }

    public GridCoord Cell { get; private set; }

    public bool TryMove(GridCoord direction)
    {
        if (!IsOrthogonal(direction))
        {
            return false;
        }

        var target = Cell + direction;
        if (!_board.IsWalkable(target) || _wick.CurrentWick < 1)
        {
            return false;
        }

        _turns.AdvanceTurn();
        _wick.Spend(1);
        Cell = target;
        if (_lamp.IsCarried)
        {
            _lamp.AttachToPlayer(Cell);
        }
        else if (_lamp.Cell == Cell)
        {
            _lamp.AttachToPlayer(Cell);
        }

        RefreshVisibility();
        return true;
    }

    public bool TryPlaceLamp()
    {
        if (!_lamp.IsCarried || !TrySpendAction())
        {
            return false;
        }

        _lamp.Place(Cell);
        RefreshVisibility();
        return true;
    }

    private static bool IsOrthogonal(GridCoord direction) =>
        Math.Abs(direction.X) + Math.Abs(direction.Y) == 1;

    private bool TrySpendAction()
    {
        if (_wick.CurrentWick < 1)
        {
            return false;
        }

        _turns.AdvanceTurn();
        _wick.Spend(1);
        return true;
    }

    private void RefreshVisibility() => _visibility?.Recompute(_lamp.Cell, _wick.CurrentRadius);

    public void RestoreCell(GridCoord cell)
    {
        Cell = cell;
    }
}

public partial class PlayerController : Node2D
{
    private static readonly Vector2 BoardOrigin = new(152, 104);
    private const int CellSize = 48;
    private PlayerMovementState _state;
    private LampController _lamp;
    private bool _inputLocked;

    public event Action<GridCoord> CellChanged;
    public event Action RestartRequested;
    public event Action FlareRequested;
    public event Action LampChanged;
    public event Action PracticeModeToggleRequested;
    public event Action UndoRequested;
    public event Action ActionStarting;
    public event Action<bool> ActionFinished;

    public GridCoord Cell => _state?.Cell ?? default;
    public PlayerMovementState State => _state;

    public override void _Ready()
    {
        EnsureInputBindings();
    }

    public void Initialize(
        BoardState board,
        IPlayerWick wick,
        ITurnAdvancer turns,
        LampController lamp,
        GridCoord startCell,
        VisibilitySystem visibility)
    {
        _lamp = lamp;
        _state = new PlayerMovementState(board, wick, turns, lamp.State, startCell, visibility);
        SyncPositions();
    }

    public bool TryMove(GridCoord direction)
    {
        ActionStarting?.Invoke();
        if (_state is null || !_state.TryMove(direction))
        {
            ActionFinished?.Invoke(false);
            return false;
        }

        SyncPositions();
        CellChanged?.Invoke(Cell);
        ActionFinished?.Invoke(true);
        return true;
    }

    public bool TryPlaceLamp()
    {
        ActionStarting?.Invoke();
        if (_state is null || !_state.TryPlaceLamp())
        {
            ActionFinished?.Invoke(false);
            return false;
        }

        _lamp.SyncPosition();
        LampChanged?.Invoke();
        ActionFinished?.Invoke(true);
        return true;
    }

    public void LockInput() => _inputLocked = true;

    public void UnlockInput() => _inputLocked = false;

    public void RestoreCell(GridCoord cell)
    {
        _state?.RestoreCell(cell);
        SyncPositions();
    }

    public override void _UnhandledInput(InputEvent inputEvent)
    {
        if (inputEvent.IsActionPressed("restart_level"))
        {
            RestartRequested?.Invoke();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (inputEvent.IsActionPressed("practice_mode"))
        {
            PracticeModeToggleRequested?.Invoke();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (inputEvent.IsActionPressed("undo_action"))
        {
            UndoRequested?.Invoke();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (_inputLocked)
        {
            return;
        }

        if (inputEvent.IsActionPressed("lamp_toggle"))
        {
            TryPlaceLamp();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (inputEvent.IsActionPressed("flare"))
        {
            FlareRequested?.Invoke();
            GetViewport().SetInputAsHandled();
            return;
        }

        var direction = inputEvent.IsActionPressed("move_up") ? new GridCoord(0, -1)
            : inputEvent.IsActionPressed("move_down") ? new GridCoord(0, 1)
            : inputEvent.IsActionPressed("move_left") ? new GridCoord(-1, 0)
            : inputEvent.IsActionPressed("move_right") ? new GridCoord(1, 0)
            : default;

        if (direction != default && TryMove(direction))
        {
            GetViewport().SetInputAsHandled();
        }
    }

    private void SyncPositions()
    {
        Position = BoardOrigin + new Vector2((Cell.X + 0.5f) * CellSize, (Cell.Y + 0.5f) * CellSize);
        _lamp.SyncPosition();
    }

    private static void EnsureInputBindings()
    {
        Bind("move_up", Key.Up, Key.W);
        Bind("move_down", Key.Down, Key.S);
        Bind("move_left", Key.Left, Key.A);
        Bind("move_right", Key.Right, Key.D);
        Bind("lamp_toggle", Key.E, Key.Q);
        Bind("flare", Key.F, Key.Space);
        Bind("restart_level", Key.R);
        Bind("practice_mode", Key.P);
        Bind("undo_action", Key.Z);
    }

    private static void Bind(string action, params Key[] keys)
    {
        if (!InputMap.HasAction(action))
        {
            InputMap.AddAction(action);
        }

        foreach (var key in keys)
        {
            var keyEvent = new InputEventKey { Keycode = key };
            if (!InputMap.ActionHasEvent(action, keyEvent))
            {
                InputMap.ActionAddEvent(action, keyEvent);
            }
        }
    }
}
