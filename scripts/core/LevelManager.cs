using Godot;
using Wick.Actors;
using Wick.Board;
using Wick.Data;
using Wick.Goals;

namespace Wick.Core;

public partial class LevelManager : Node
{
    private readonly LevelLoader _loader = new();
    private BoardState _board;
    private VisibilitySystem _visibility;
    private LevelDefinition _currentLevel;
    private PlayerController _player;
    private LampController _lamp;
    private StairController _stair;
    private BoardRenderer _boardRenderer;
    private WickSystem _wick;
    private TurnManager _turns;
    private GameEvents _events;

    public override void _Ready()
    {
        _player = GetNode<PlayerController>("../Player");
        _lamp = GetNode<LampController>("../Lamp");
        _stair = GetNode<StairController>("../Stair");
        _boardRenderer = GetNode<BoardRenderer>("../Board/BoardRenderer");
        _wick = GetNode<WickSystem>("../WickSystem");
        _turns = GetNode<TurnManager>("../TurnManager");
        _events = GetNode<GameEvents>("../GameEvents");

        _player.CellChanged += OnPlayerMoved;
        _player.RestartRequested += RestartLevel;
        _stair.Reached += OnStairReached;
        _events.Connect(GameEvents.SignalName.LightOut, Callable.From<string>(OnLightOut));

        LoadLevel(1);
    }

    public void LoadLevel(int index)
    {
        _currentLevel = _loader.Load(index);
        _board = new BoardState(_currentLevel.Width, _currentLevel.Height, _currentLevel.Walls);
        _visibility = new VisibilitySystem(_board);
        _lamp.Initialize(_currentLevel.LampStart);
        _wick.Initialize(_currentLevel.StartingWick);
        _turns.Reset();
        _player.Initialize(_board, _wick, _turns, _lamp, _currentLevel.PlayerStart, _visibility);
        _stair.Configure(_currentLevel.StairCell);
        _boardRenderer.Configure(_board, _visibility, _lamp.Cell, _wick.CurrentRadius);
    }

    public void RestartLevel()
    {
        if (_currentLevel is not null)
        {
            LoadLevel(_currentLevel.Index);
        }
    }

    private void OnPlayerMoved(GridCoord cell)
    {
        _boardRenderer.RecomputeVisibility(_lamp.Cell, _wick.CurrentRadius);
        _stair.TryEnter(cell);
    }

    private void OnLightOut(string cause) => CallDeferred(nameof(RestartLevel));

    private void OnStairReached()
    {
        _events.EmitSignal(GameEvents.SignalName.LevelCleared, _currentLevel.Index, _wick.CurrentWick);
    }
}
