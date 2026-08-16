using System;
using System.Collections.Generic;
using Godot;
using Wick.Core;

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

    public override void _Draw()
    {
        if (_board is null || _visibility is null)
        {
            return;
        }

        for (var y = 0; y < _board.Height; y++)
        for (var x = 0; x < _board.Width; x++)
        {
            var cell = new GridCoord(x, y);
            var rect = new Rect2(BoardOrigin + new Vector2(x * CellSize, y * CellSize), new Vector2(CellSize, CellSize));
            var state = GetVisibilityState(_visibility, cell);
            DrawRect(rect, GetTileColor(state));
            DrawRect(rect, new Color(0.08f, 0.09f, 0.11f), false, 1f);

            if (state != VisibilityState.Hidden && _board.IsWall(cell))
            {
                DrawRect(rect.Grow(-CellSize / 6f), state == VisibilityState.Visible
                    ? new Color(0.30f, 0.23f, 0.15f)
                    : new Color(0.15f, 0.12f, 0.10f));
            }
        }

        if (GetVisibilityState(_visibility, _lampCell) == VisibilityState.Visible)
        {
            var center = BoardOrigin + new Vector2((_lampCell.X + 0.5f) * CellSize, (_lampCell.Y + 0.5f) * CellSize);
            DrawCircle(center, CellSize / 5f, new Color(1f, 0.85f, 0.45f));
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
        VisibilityState.Visible => new Color(0.62f, 0.52f, 0.30f),
        VisibilityState.Remembered => new Color(0.24f, 0.21f, 0.18f),
        _ => new Color(0.04f, 0.05f, 0.07f)
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
