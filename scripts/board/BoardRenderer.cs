using System;
using System.Collections.Generic;
using Godot;
using Wick.Core;
using Wick.Ui;

namespace Wick.Board;

public enum VisibilityState
{
    Hidden,
    Remembered,
    Visible
}

public partial class BoardRenderer : Node2D
{
    private const int BoardWidth = 19;
    private const int BoardHeight = 13;
    private readonly GridCoord _initialLampCell = new(9, 6);
    private BoardState _board;
    private VisibilitySystem _visibility;
    private GridCoord _lampCell;

    [Export] public int CellSize { get; set; } = 48;
    [Export] public int PlaceholderRadius { get; set; } = 6;
    [Export] public Vector2 BoardOrigin { get; set; } = new(152, 104);
    public VisibilitySystem Visibility => _visibility;

    public override void _Ready()
    {
        _board = new BoardState(BoardWidth, BoardHeight, CreateBoundaryWalls());
        _visibility = new VisibilitySystem(_board);
        RecomputeVisibility(_initialLampCell, PlaceholderRadius);
    }

    public void Configure(BoardState board, VisibilitySystem visibility, GridCoord lampCell, int radius)
    {
        _board = board;
        _visibility = visibility;
        RecomputeVisibility(lampCell, radius);
    }

    public void RecomputeVisibility(GridCoord lampCell, int radius)
    {
        _lampCell = lampCell;
        _visibility.Recompute(lampCell, radius);
        QueueRedraw();
    }

    public void ForceDarkness()
    {
        _visibility?.Recompute(_lampCell, 0);
        QueueRedraw();
    }

    public void RestoreDrawingState(GridCoord lampCell)
    {
        _lampCell = lampCell;
        QueueRedraw();
    }

    public bool IsCellVisible(GridCoord cell) => _visibility is not null && _visibility.IsVisible(cell);

    public override void _Draw()
    {
        if (_board is null || _visibility is null)
        {
            return;
        }

        var boardRect = new Rect2(BoardOrigin, new Vector2(_board.Width * CellSize, _board.Height * CellSize));
        DrawRect(boardRect.Grow(5f), PresentationPalette.Amber);
        DrawRect(boardRect.Grow(2f), PresentationPalette.HiddenFloor);

        for (var y = 0; y < _board.Height; y++)
        for (var x = 0; x < _board.Width; x++)
        {
            var cell = new GridCoord(x, y);
            var rect = new Rect2(BoardOrigin + new Vector2(x * CellSize, y * CellSize), new Vector2(CellSize, CellSize));
            var state = GetVisibilityState(_visibility, cell);
            DrawRect(rect, GetTileColor(state));
            DrawRect(rect, PresentationPalette.HiddenFloor.Lightened(0.09f), false, 1f);

            if (state == VisibilityState.Visible && !_board.IsWall(cell))
            {
                var seamY = rect.Position.Y + CellSize * (0.33f + 0.17f * ((x + y) % 2));
                DrawLine(
                    new Vector2(rect.Position.X + 8f, seamY),
                    new Vector2(rect.End.X - 8f, seamY),
                    PresentationPalette.Amber.Darkened(0.56f),
                    1f);
            }

            if (state != VisibilityState.Hidden && _board.IsWall(cell))
            {
                var block = rect.Grow(-CellSize / 7f);
                DrawRect(block, PresentationPalette.Wall);
                DrawRect(block, state == VisibilityState.Visible
                    ? PresentationPalette.Amber.Darkened(0.25f)
                    : PresentationPalette.RememberedFloor.Lightened(0.08f), false, 2f);
                DrawCircle(block.Position + new Vector2(5f, 5f), 1.5f, PresentationPalette.Amber.Darkened(0.35f));
                DrawCircle(block.End - new Vector2(5f, 5f), 1.5f, PresentationPalette.Amber.Darkened(0.35f));
            }
        }
    }

    public static VisibilityState GetVisibilityState(VisibilitySystem visibility, GridCoord cell)
    {
        if (visibility is null)
        {
            throw new ArgumentNullException(nameof(visibility));
        }

        return visibility.IsVisible(cell)
            ? VisibilityState.Visible
            : visibility.WasSeen(cell)
                ? VisibilityState.Remembered
                : VisibilityState.Hidden;
    }

    private static Color GetTileColor(VisibilityState state) => state switch
    {
        VisibilityState.Visible => PresentationPalette.VisibleFloor,
        VisibilityState.Remembered => PresentationPalette.RememberedFloor,
        _ => PresentationPalette.HiddenFloor
    };

    private static IEnumerable<GridCoord> CreateBoundaryWalls()
    {
        var walls = new List<GridCoord>();

        for (var x = 0; x < BoardWidth; x++)
        {
            walls.Add(new GridCoord(x, 0));
            walls.Add(new GridCoord(x, BoardHeight - 1));
        }

        for (var y = 1; y < BoardHeight - 1; y++)
        {
            walls.Add(new GridCoord(0, y));
            walls.Add(new GridCoord(BoardWidth - 1, y));
        }

        return walls;
    }
}
