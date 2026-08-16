using System;
using System.Collections.Generic;
using System.Linq;
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
    public GridCoord Cell => _cell;

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

    public void RestoreCollected(bool isCollected) => IsCollected = isCollected;
}

public sealed class OilFlaskCollectionState
{
    private readonly OilFlaskState[] _flasks;

    public OilFlaskCollectionState(WickState wick, ITurnAdvancer turns, IEnumerable<GridCoord> cells)
    {
        _flasks = cells.Select(cell => new OilFlaskState(wick, turns, cell)).ToArray();
        foreach (var flask in _flasks)
        {
            flask.TurnSpent += () => TurnSpent?.Invoke(0);
        }
    }

    public event Action<int> TurnSpent;
    public IReadOnlyList<GridCoord> Cells => _flasks.Select(flask => flask.Cell).ToArray();

    public bool TryBeginLift(GridCoord playerCell)
    {
        var flask = _flasks.FirstOrDefault(candidate => candidate.IsAvailableAt(playerCell));
        return flask is not null && flask.TryBeginLift(playerCell);
    }

    public bool IsCollected(GridCoord cell) => _flasks.FirstOrDefault(flask => flask.Cell == cell)?.IsCollected ?? false;

    public bool IsAvailableAt(GridCoord cell) => _flasks.Any(flask => flask.IsAvailableAt(cell));

    public OilFlaskCollectionSnapshot CaptureSnapshot() => new(_flasks.Select(flask => flask.IsCollected).ToArray());

    public void RestoreSnapshot(OilFlaskCollectionSnapshot snapshot)
    {
        if (snapshot.Collected.Length != _flasks.Length)
        {
            throw new ArgumentException("Flask snapshot does not match the authored flask set.", nameof(snapshot));
        }

        for (var index = 0; index < _flasks.Length; index++)
        {
            _flasks[index].RestoreCollected(snapshot.Collected[index]);
        }
    }
}

public sealed record OilFlaskCollectionSnapshot(bool[] Collected);

public partial class OilFlaskController : Node2D
{
    private static readonly Vector2 BoardOrigin = new(152, 104);
    private const int CellSize = 48;
    private WickSystem _wick;
    private TurnManager _turns;
    private PlayerController _player;
    private BoardRenderer _visibility;
    private Action<int> _onTurnSpent;
    private Action _onLiftCompleted;
    private OilFlaskCollectionState _state;
    private readonly List<Node2D> _markers = new();

    public OilFlaskCollectionState State => _state;

    public void Configure(
        WickSystem wick,
        TurnManager turns,
        PlayerController player,
        BoardRenderer visibility,
        Action<int> onTurnSpent,
        Action onLiftCompleted,
        IEnumerable<GridCoord> flaskCells)
    {
        _wick = wick;
        _turns = turns;
        _player = player;
        _visibility = visibility;
        _onTurnSpent = onTurnSpent;
        _onLiftCompleted = onLiftCompleted;
        _state = new OilFlaskCollectionState(_wick.State, _turns, flaskCells);
        _state.TurnSpent += OnTurnSpent;
        Position = Vector2.Zero;
        RebuildMarkers();
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
        RefreshMarkers();
        _onLiftCompleted?.Invoke();
        _player.UnlockInput();
        return lifted;
    }

    private void OnTurnSpent(int radiusOverride) => _onTurnSpent?.Invoke(radiusOverride);

    public void RestoreSnapshot(OilFlaskCollectionSnapshot snapshot)
    {
        _state.RestoreSnapshot(snapshot);
        RefreshMarkers();
    }

    private void RebuildMarkers()
    {
        for (var index = 1; index < _markers.Count; index++)
        {
            _markers[index].QueueFree();
        }

        _markers.Clear();
        var template = GetNode<Node2D>("Body");
        template.Visible = _state.Cells.Count > 0;
        if (_state.Cells.Count == 0)
        {
            return;
        }

        _markers.Add(template);
        for (var index = 1; index < _state.Cells.Count; index++)
        {
            var marker = template.Duplicate() as Node2D;
            AddChild(marker);
            _markers.Add(marker);
        }

        RefreshMarkers();
    }

    private void RefreshMarkers()
    {
        for (var index = 0; index < _markers.Count; index++)
        {
            var cell = _state.Cells[index];
            _markers[index].Position = BoardOrigin + new Vector2((cell.X + 0.5f) * CellSize, (cell.Y + 0.5f) * CellSize);
            _markers[index].Visible = !_state.IsCollected(cell);
        }
    }
}
