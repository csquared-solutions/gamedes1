using System;
using System.Collections.Generic;
using Godot;
using Wick.Actors;
using Wick.Board;
using Wick.Data;
using Wick.Goals;
using Wick.Hazards;
using Wick.Ui;

namespace Wick.Core;

public partial class LevelManager : Node
{
    private readonly LevelLoader _loader = new();
    private readonly StairPlacementSelector _stairPlacement = new();
    private readonly Dictionary<int, LevelDefinition> _activeAttempts = new();
    private readonly CampaignSessionState _session = new(7);
    private readonly PracticeModeState _practice = new();
    private BoardState _board;
    private VisibilitySystem _visibility;
    private LevelDefinition _currentLevel;
    private CampaignLevelScene _campaignScene;
    private PlayerController _player;
    private LampController _lamp;
    private StairController _stair;
    private BoardRenderer _boardRenderer;
    private WickSystem _wick;
    private TurnManager _turns;
    private GameEvents _events;
    private HazardSystem _hazards;
    private Pickups.OilFlaskController _oilFlask;
    private GameplaySnapshot _pendingAction;
    private bool _isTerminal;
    private int _reportedRemainingWick = -1;
    private int _presentationGeneration;

    public bool IsPracticeMode => _practice.IsEnabled;
    public bool CanUndo => _practice.CanUndo;
    public int CurrentLevelIndex => _session.ActiveLevelIndex;
    public bool IsRunComplete => _session.IsActiveComplete;
    public bool IsTerminal => _isTerminal;
    public bool IsExitBlockedByCarriedLamp => _currentLevel is not null &&
        _player is not null &&
        _lamp is not null &&
        StairEntryRule.IsBlockedByCarriedLamp(_currentLevel, _player.Cell, _lamp.State.IsCarried);
    public int ReportedRemainingWick => _reportedRemainingWick;

    public override void _Ready()
    {
        _player = GetNode<PlayerController>("../Player");
        _lamp = GetNode<LampController>("../Lamp");
        _stair = GetNode<StairController>("../Stair");
        _boardRenderer = GetNode<BoardRenderer>("../Board/BoardRenderer");
        _wick = GetNode<WickSystem>("../WickSystem");
        _turns = GetNode<TurnManager>("../TurnManager");
        _events = GetNode<GameEvents>("../GameEvents");
        _hazards = GetNode<HazardSystem>("../HazardSystem");
        _oilFlask = GetNode<Pickups.OilFlaskController>("../OilFlask");

        _player.CellChanged += OnPlayerMoved;
        _player.RestartRequested += OnRestartRequested;
        _player.FlareRequested += OnFlareRequested;
        _player.LampChanged += OnLampChanged;
        _player.PracticeModeToggleRequested += TogglePracticeMode;
        _player.UndoRequested += OnUndoRequested;
        _player.ActionStarting += OnActionStarting;
        _player.ActionFinished += OnActionFinished;
        _stair.Reached += OnStairReached;
        _events.Connect(GameEvents.SignalName.LightOut, Callable.From<string>(OnLightOut));

        CallDeferred(nameof(LoadInitialLevel));
    }

    public void BeginGame() => _player.UnlockInput();

    private void LoadInitialLevel()
    {
        LoadCampaignLevel(1);
        _player.LockInput();
    }

    public void LoadCampaignLevel(int index)
    {
        _session.LoadMainLevel(index);
        _practice.SetEnabled(false);
        LoadActiveLevel(index, preserveUndo: false);
    }

    public void LoadLevel(int index) => LoadCampaignLevel(index);

    public void SetPracticeMode(bool enabled)
    {
        if (enabled == IsPracticeMode)
        {
            return;
        }

        _practice.SetEnabled(enabled);
        var transition = _session.SetPracticeMode(enabled);
        _pendingAction = null;
        LoadActiveLevel(transition.LevelIndex, preserveUndo: false);
    }

    public void RestartLevel()
    {
        if (_currentLevel is not null)
        {
            LoadActiveLevel(_session.RestartCurrentLevel().LevelIndex, preserveUndo: false);
        }
    }

    public void RestartRun()
    {
        _practice.SetEnabled(false);
        _reportedRemainingWick = -1;
        LoadActiveLevel(_session.RestartRun().LevelIndex, preserveUndo: false);
    }

    public bool TryUndo()
    {
        if (!_practice.TryTakeUndo(out var snapshot))
        {
            return false;
        }

        if (_currentLevel.Index != snapshot.LevelIndex)
        {
            _session.RestoreActive(snapshot.LevelIndex, snapshot.IsCampaignComplete);
            LoadActiveLevel(snapshot.LevelIndex, preserveUndo: true);
        }

        var flags = snapshot.Restore(
            _player.State,
            _lamp.State,
            _wick.State,
            _turns.State,
            _hazards.State,
            _oilFlask.State,
            _visibility);
        _session.RestoreActive(flags.LevelIndex, flags.IsCampaignComplete);
        _isTerminal = flags.IsTerminal;
        _reportedRemainingWick = flags.ReportedRemainingWick;
        _player.RestoreCell(snapshot.PlayerCell);
        _lamp.RestoreSnapshot(snapshot.Lamp);
        _hazards.RestoreSnapshot(snapshot.Hazards);
        _oilFlask.RestoreSnapshot(snapshot.Flasks);
        _boardRenderer.RestoreDrawingState(snapshot.Lamp.Cell);
        if (_isTerminal || _session.IsActiveComplete)
        {
            _player.LockInput();
        }
        else
        {
            _player.UnlockInput();
        }

        return true;
    }

    private void LoadActiveLevel(int index, bool preserveUndo)
    {
        _presentationGeneration++;
        var previousLevelIndex = _currentLevel?.Index;
        ActivateCampaignScene(index);
        var restoresPreviousAttempt = preserveUndo && previousLevelIndex.HasValue && index < previousLevelIndex.Value;
        if (restoresPreviousAttempt && _activeAttempts.TryGetValue(index, out var restoredLevel))
        {
            _currentLevel = restoredLevel;
        }
        else
        {
            _currentLevel = _stairPlacement.SelectForAttempt(_campaignScene.Definition);
            _activeAttempts[index] = _currentLevel;
        }

        _board = new BoardState(_currentLevel.Width, _currentLevel.Height, _currentLevel.Walls);
        _visibility = new VisibilitySystem(_board);
        _lamp.Initialize(_currentLevel.LampStart);
        _wick.StartFreshLevel(_currentLevel.StartingWick, IsPracticeMode);
        _turns.Reset();
        _player.Initialize(_board, _wick, _turns, _lamp, _currentLevel.PlayerStart, _visibility);
        _player.UnlockInput();
        _stair.Configure(_currentLevel.StairCell, _visibility);
        _boardRenderer.Configure(_board, _visibility, _lamp.Cell, _wick.CurrentRadius);
        _hazards.Initialize(
            _board,
            _visibility,
            _lamp,
            _wick,
            _events,
            GetNode<Node2D>("../Crawler"),
            GetNode<Node2D>("../Moth"),
            _currentLevel.Crawlers,
            _currentLevel.Moths);
        _oilFlask.Configure(
            _wick,
            _turns,
            _player,
            _boardRenderer,
            OnFlaskTurnSpent,
            RefreshTacticalState,
            () => !_isTerminal,
            _currentLevel.Flasks);
        _isTerminal = false;
        if (!preserveUndo)
        {
            _reportedRemainingWick = -1;
        }
        _pendingAction = null;
        if (!preserveUndo)
        {
            _practice.ClearUndo();
        }
    }

    private void ActivateCampaignScene(int index)
    {
        var path = _loader.GetScenePath(index);
        var packed = GD.Load<PackedScene>(path) ?? throw new InvalidOperationException($"Campaign scene is missing: {path}");
        var nextScene = packed.Instantiate<CampaignLevelScene>();
        if (nextScene.Index != index)
        {
            nextScene.QueueFree();
            throw new InvalidOperationException($"Campaign scene {path} identifies level {nextScene.Index} instead of {index}.");
        }

        _campaignScene?.QueueFree();
        _campaignScene = nextScene;
        AddChild(_campaignScene);
    }

    private void TogglePracticeMode() => SetPracticeMode(!IsPracticeMode);

    private void OnRestartRequested()
    {
        var transition = _session.HandleRestartRequest();
        if (transition.StartsNewRun)
        {
            _practice.SetEnabled(false);
            _reportedRemainingWick = -1;
        }

        LoadActiveLevel(transition.LevelIndex, preserveUndo: false);
    }

    private void OnUndoRequested() => TryUndo();

    private void OnActionStarting()
    {
        _pendingAction = IsPracticeMode && !_isTerminal ? CaptureSnapshot() : null;
    }

    private void OnActionFinished(bool succeeded)
    {
        if (succeeded && _pendingAction is not null)
        {
            _practice.RecordSuccessfulAction(_pendingAction);
        }

        _pendingAction = null;
    }

    private GameplaySnapshot CaptureSnapshot() => GameplaySnapshot.Capture(
        _currentLevel.Index,
        _player.State,
        _lamp.State,
        _wick.State,
        _turns.State,
        _hazards.State,
        _oilFlask.State,
        _visibility,
        _isTerminal,
        _session.IsActiveComplete,
        _reportedRemainingWick);

    private void OnPlayerMoved(GridCoord cell)
    {
        RefreshTacticalState();
        if (StairEntryRule.CanEnter(_currentLevel, cell, _lamp.State.IsCarried))
        {
            _stair.TryEnter(cell);
        }
        if (_isTerminal || _wick.IsDepleted)
        {
            return;
        }

        _hazards.AdvanceTurn();
        if (_isTerminal)
        {
            return;
        }

        _wick.AdvanceTurn();
        RefreshTacticalState();
        if (!_wick.IsDepleted)
        {
            _oilFlask.TryBeginLift(cell);
        }
    }

    private void OnFlareRequested()
    {
        OnActionStarting();
        if (!_wick.TryFireFlare())
        {
            OnActionFinished(false);
            return;
        }

        _turns.AdvanceTurn();
        RefreshTacticalState();
        _hazards.AdvanceTurn();
        if (!_isTerminal)
        {
            _wick.AdvanceTurn();
            RefreshTacticalState();
        }

        OnActionFinished(true);
    }

    private void OnLampChanged()
    {
        if (_wick.IsDepleted)
        {
            return;
        }

        RefreshTacticalState();
        _hazards.AdvanceTurn();
        if (!_isTerminal)
        {
            _wick.AdvanceTurn();
            RefreshTacticalState();
        }
    }

    private void OnFlaskTurnSpent(int radiusOverride) => _hazards.AdvanceTurn(radiusOverride);

    private void RefreshTacticalState()
    {
        _boardRenderer.RecomputeVisibility(_lamp.Cell, _wick.CurrentRadius);
        _stair.RefreshVisibility();
        _hazards.RefreshVisibility();
        _oilFlask.RefreshVisibility();
    }

    private void OnLightOut(string cause)
    {
        if (_isTerminal)
        {
            return;
        }

        _isTerminal = true;
        _player.LockInput();
        _boardRenderer.ForceDarkness();
        _stair.RefreshVisibility();
        _hazards.RefreshVisibility();
        _oilFlask.RefreshVisibility();
        if (!IsPracticeMode)
        {
            RestartAfterPresentation(_presentationGeneration);
        }
    }

    private void OnStairReached()
    {
        if (_isTerminal)
        {
            return;
        }

        _reportedRemainingWick = _wick.CurrentWick;
        _isTerminal = true;
        _player.LockInput();
        _events.EmitSignal(GameEvents.SignalName.LevelCleared, _currentLevel.Index, _reportedRemainingWick);
        AdvanceCampaignAfterPresentation(_presentationGeneration);
    }

    private async void RestartAfterPresentation(int generation)
    {
        await ToSignal(
            GetTree().CreateTimer(PresentationTiming.EndStateSeconds),
            SceneTreeTimer.SignalName.Timeout);
        if (generation == _presentationGeneration && _isTerminal)
        {
            RestartLevel();
        }
    }

    private async void AdvanceCampaignAfterPresentation(int generation)
    {
        await ToSignal(
            GetTree().CreateTimer(PresentationTiming.EndStateSeconds),
            SceneTreeTimer.SignalName.Timeout);
        if (generation == _presentationGeneration && _isTerminal)
        {
            AdvanceCampaignAfterClear();
        }
    }

    private void AdvanceCampaignAfterClear()
    {
        if (!_isTerminal)
        {
            return;
        }

        if (_session.AdvanceAfterClear())
        {
            LoadActiveLevel(_session.ActiveLevelIndex, preserveUndo: true);
            return;
        }

        _isTerminal = true;
        _player.LockInput();
    }
}
