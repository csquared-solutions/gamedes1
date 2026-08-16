using Godot;
using Wick.Actors;
using Wick.Board;
using Wick.Core;

namespace Wick.Hazards;

public partial class HazardSystem : Node
{
    private static readonly Vector2 BoardOrigin = new(152, 104);
    private const int CellSize = 48;

    private BoardState _board;
    private VisibilitySystem _visibility;
    private LampController _lamp;
    private WickSystem _wick;
    private GameEvents _events;
    private Node2D _crawlerMarker;
    private Node2D _mothMarker;
    private CrawlerController _crawler;
    private MothController _moth;
    private bool _smothered;

    public HazardMotionState CrawlerState { get; private set; } = HazardMotionState.Stalled;
    public HazardMotionState MothState { get; private set; } = HazardMotionState.Stalled;

    public void Initialize(
        BoardState board,
        VisibilitySystem visibility,
        LampController lamp,
        WickSystem wick,
        GameEvents events,
        Node2D crawlerMarker,
        Node2D mothMarker)
    {
        _board = board;
        _visibility = visibility;
        _lamp = lamp;
        _wick = wick;
        _events = events;
        _crawlerMarker = crawlerMarker;
        _mothMarker = mothMarker;
        _crawler = new CrawlerController(new GridCoord(board.Width - 2, 3));
        _moth = new MothController(new GridCoord(board.Width - 2, board.Height - 4));
        _smothered = false;
        CrawlerState = HazardMotionState.Stalled;
        MothState = HazardMotionState.Stalled;
        SyncMarkers();
    }

    public void AdvanceTurn()
    {
        if (_smothered || _crawler is null || _moth is null)
        {
            return;
        }

        var context = new HazardContext(_board, _visibility, _lamp.Cell, _wick.CurrentRadius);
        var crawlerCell = _crawler.Cell;
        var mothCell = _moth.Cell;
        _crawler.TakeTurn(context);
        _moth.TakeTurn(context);
        CrawlerState = _crawler.Cell == crawlerCell
            ? HazardMotionState.Stalled
            : context.IsLit(_crawler.Cell) ? HazardMotionState.Slowed : HazardMotionState.Moving;
        MothState = _moth.Cell == mothCell
            ? HazardMotionState.Stalled
            : HazardMotionState.Moving;
        SyncMarkers();

        if (_crawler.HasReachedLamp || _moth.HasReachedLamp)
        {
            _smothered = true;
            _events.EmitSignal(GameEvents.SignalName.LightOut, "smothered");
        }
    }

    private void SyncMarkers()
    {
        _crawlerMarker.Position = ToWorld(_crawler.Cell);
        _mothMarker.Position = ToWorld(_moth.Cell);
    }

    private static Vector2 ToWorld(GridCoord cell) =>
        BoardOrigin + new Vector2((cell.X + 0.5f) * CellSize, (cell.Y + 0.5f) * CellSize);
}
