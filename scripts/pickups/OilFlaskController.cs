using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Wick.Actors;
using Wick.Board;
using Wick.Core;
using Wick.Data;
using Wick.Ui;

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

    public bool TryBeginLift(GridCoord playerCell) => TryBeginLift(playerCell, () => true);

    public bool TryBeginLift(GridCoord playerCell, Func<bool> canContinue)
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
        if (!canContinue())
        {
            return true;
        }

        if (!_wick.Spend(1) || _wick.IsDepleted)
        {
            return true;
        }

        _turns.AdvanceTurn();
        TurnSpent?.Invoke();
        if (!canContinue())
        {
            return true;
        }

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

    public bool TryBeginLift(GridCoord playerCell) => TryBeginLift(playerCell, () => true);

    public bool TryBeginLift(GridCoord playerCell, Func<bool> canContinue)
    {
        var flask = _flasks.FirstOrDefault(candidate => candidate.IsAvailableAt(playerCell));
        return flask is not null && flask.TryBeginLift(playerCell, canContinue);
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

public sealed class FlaskPresentationState
{
    private int _displayWick;

    public bool IsActive { get; private set; }

    public void Begin(int displayWick)
    {
        _displayWick = displayWick;
        IsActive = true;
    }

    public void Complete() => IsActive = false;

    public int GetDisplayedWick(int actualWick) => IsActive ? _displayWick : actualWick;

    public int GetDisplayedRadius(int actualRadius) => IsActive ? 0 : actualRadius;
}

public static class OilFlaskInputPolicy
{
    public static bool ShouldUnlockAfterLift(bool isTerminal) => !isTerminal;
    public static bool ShouldRestoreLightAfterLift(bool isTerminal) => !isTerminal;
}

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
    private Func<bool> _canContinueLift;
    private OilFlaskCollectionState _state;
    private readonly List<Node2D> _markers = new();
    private readonly FlaskPresentationState _presentation = new();
    private int _presentationVersion;

    public event Action LiftStarted;
    public OilFlaskCollectionState State => _state;
    public int DisplayedWick => _presentation.GetDisplayedWick(_wick?.CurrentWick ?? 0);
    public int DisplayedRadius => _presentation.GetDisplayedRadius(_wick?.CurrentRadius ?? 0);

    public void Configure(
        WickSystem wick,
        TurnManager turns,
        PlayerController player,
        BoardRenderer visibility,
        Action<int> onTurnSpent,
        Action onLiftCompleted,
        Func<bool> canContinueLift,
        IEnumerable<GridCoord> flaskCells)
    {
        _presentationVersion++;
        _presentation.Complete();
        _wick = wick;
        _turns = turns;
        _player = player;
        _visibility = visibility;
        _onTurnSpent = onTurnSpent;
        _onLiftCompleted = onLiftCompleted;
        _canContinueLift = canContinueLift;
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
        _presentation.Begin(_wick.CurrentWick);
        LiftStarted?.Invoke();
        var lifted = _state.TryBeginLift(playerCell, _canContinueLift);
        RefreshMarkers();
        FinishLiftPresentation(_presentationVersion);
        return lifted;
    }

    private async void FinishLiftPresentation(int version)
    {
        await ToSignal(
            GetTree().CreateTimer(PresentationTiming.FlaskDarknessSeconds),
            SceneTreeTimer.SignalName.Timeout);
        if (version != _presentationVersion)
        {
            return;
        }

        _presentation.Complete();
        var isTerminal = !(_canContinueLift?.Invoke() ?? true);
        if (OilFlaskInputPolicy.ShouldRestoreLightAfterLift(isTerminal))
        {
            _onLiftCompleted?.Invoke();
        }

        if (OilFlaskInputPolicy.ShouldUnlockAfterLift(isTerminal))
        {
            _player.UnlockInput();
        }
    }

    private void OnTurnSpent(int radiusOverride) => _onTurnSpent?.Invoke(radiusOverride);

    public void RestoreSnapshot(OilFlaskCollectionSnapshot snapshot)
    {
        _state.RestoreSnapshot(snapshot);
        RefreshMarkers();
    }

    public void RefreshVisibility() => RefreshMarkers();

    private void RebuildMarkers()
    {
        for (var index = 1; index < _markers.Count; index++)
        {
            _markers[index].QueueFree();
        }

        _markers.Clear();
        var template = GetNode<Node2D>("FlaskSilhouette");
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
            _markers[index].Visible = WorldMarkerReadability.ShouldShow(
                _visibility.Visibility,
                cell,
                _state.IsCollected(cell));
        }
    }
}
