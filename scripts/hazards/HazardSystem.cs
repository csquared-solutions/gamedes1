using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Wick.Actors;
using Wick.Board;
using Wick.Core;
using Wick.Ui;

namespace Wick.Hazards;

public partial class HazardSystem : Node
{
    private static readonly Vector2 BoardOrigin = new(152, 104);
    private const int CellSize = 48;

    private VisibilitySystem _visibility;
    private LampController _lamp;
    private WickSystem _wick;
    private GameEvents _events;
    private readonly List<Node2D> _crawlerMarkers = new();
    private readonly List<Node2D> _mothMarkers = new();

    public HazardSimulationState State { get; private set; }
    public HazardMotionState CrawlerState => AggregateMotion(State?.CrawlerMotion);
    public HazardMotionState MothState => AggregateMotion(State?.MothMotion);
    public bool IsCrawlerVisible => State is not null && State.CrawlerCells.Any(cell => HazardReadability.IsVisible(_visibility, cell));
    public bool IsMothVisible => State is not null && State.MothCells.Any(cell => HazardReadability.IsVisible(_visibility, cell));
    public bool HasMoths => State is not null && State.MothCells.Count > 0;

    public void Initialize(
        BoardState board,
        VisibilitySystem visibility,
        LampController lamp,
        WickSystem wick,
        GameEvents events,
        Node2D crawlerMarker,
        Node2D mothMarker,
        IEnumerable<GridCoord> crawlerStarts,
        IEnumerable<GridCoord> mothStarts)
    {
        _visibility = visibility;
        _lamp = lamp;
        _wick = wick;
        _events = events;
        var tieBreakerSeed = unchecked((uint)Random.Shared.Next(1, 3));
        State = new HazardSimulationState(board, crawlerStarts, mothStarts, tieBreakerSeed);
        RebuildMarkers(_crawlerMarkers, crawlerMarker, State.CrawlerCells.Count);
        RebuildMarkers(_mothMarkers, mothMarker, State.MothCells.Count);
        SyncMarkers();
    }

    public void AdvanceTurn(int radiusOverride = -1)
    {
        if (State is null)
        {
            return;
        }

        var radius = radiusOverride >= 0 ? radiusOverride : _wick.CurrentRadius;
        if (State.AdvanceTurn(_visibility, _lamp.Cell, radius))
        {
            _events.EmitSignal(GameEvents.SignalName.LightOut, "smothered");
        }

        SyncMarkers();
    }

    public void RestoreSnapshot(HazardSimulationSnapshot snapshot)
    {
        State.RestoreSnapshot(snapshot);
        SyncMarkers();
    }

    public void RefreshVisibility() => SyncMarkers();

    private void SyncMarkers()
    {
        if (State is null)
        {
            return;
        }

        SyncMarkerSet(_crawlerMarkers, State.CrawlerCells, State.CrawlerMotion);
        SyncMarkerSet(_mothMarkers, State.MothCells, State.MothMotion);
    }

    private void SyncMarkerSet(IReadOnlyList<Node2D> markers, IReadOnlyList<GridCoord> cells, IReadOnlyList<HazardMotionState> motion)
    {
        for (var index = 0; index < markers.Count; index++)
        {
            markers[index].Position = ToWorld(cells[index]);
            markers[index].Visible = _visibility.IsVisible(cells[index]);
            markers[index].GetNode<Line2D>("StateRing").Modulate = HazardStateColor.For(motion[index]);
        }
    }

    private static void RebuildMarkers(List<Node2D> markers, Node2D template, int count)
    {
        for (var index = 1; index < markers.Count; index++)
        {
            markers[index].QueueFree();
        }

        markers.Clear();
        template.Visible = count > 0;
        if (count == 0)
        {
            return;
        }

        markers.Add(template);
        for (var index = 1; index < count; index++)
        {
            var marker = template.Duplicate() as Node2D;
            template.GetParent().AddChild(marker);
            markers.Add(marker);
        }
    }

    private static HazardMotionState AggregateMotion(IReadOnlyList<HazardMotionState> states)
    {
        if (states is null || states.Count == 0)
        {
            return HazardMotionState.Stalled;
        }

        if (states.Contains(HazardMotionState.Moving))
        {
            return HazardMotionState.Moving;
        }

        return states.Contains(HazardMotionState.Slowed) ? HazardMotionState.Slowed : HazardMotionState.Stalled;
    }

    private static Vector2 ToWorld(GridCoord cell) =>
        BoardOrigin + new Vector2((cell.X + 0.5f) * CellSize, (cell.Y + 0.5f) * CellSize);
}
