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
    private int _radius;

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
        _radius = radius;
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
        DrawRect(boardRect.Grow(8f), PresentationPalette.Soot);
        DrawRect(boardRect.Grow(6f), PresentationPalette.Brass.Darkened(0.42f));
        DrawRect(boardRect.Grow(3f), PresentationPalette.Brass);
        DrawRect(boardRect.Grow(1f), PresentationPalette.HiddenFloor);

        for (var y = 0; y < _board.Height; y++)
        for (var x = 0; x < _board.Width; x++)
        {
            var cell = new GridCoord(x, y);
            var rect = new Rect2(BoardOrigin + new Vector2(x * CellSize, y * CellSize), new Vector2(CellSize, CellSize));
            var state = GetVisibilityState(_visibility, cell);
            var variant = DungeonSurfacePattern.StoneVariant(cell);
            DrawRect(rect, GetSurfaceColor(state, variant));

            if (state == VisibilityState.Visible)
            {
                var light = DungeonSurfacePattern.LightStrength(cell, _lampCell, _radius);
                DrawRect(rect.Grow(-1f), WithAlpha(PresentationPalette.Brass, 0.035f + (light * 0.08f)));
            }

            if (state == VisibilityState.Hidden)
            {
                DrawRect(rect, WithAlpha(PresentationPalette.StoneShadow, 0.34f), false, 1f);
                continue;
            }

            if (_board.IsWall(cell))
            {
                DrawWallStone(rect, state, variant);
            }
            else
            {
                DrawFloorStone(rect, state, variant);
            }
        }

        DrawFrameOrnaments(boardRect);
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

    private static Color GetSurfaceColor(VisibilityState state, int variant)
    {
        var color = GetTileColor(state);
        var shift = (variant - 2) * 0.022f;
        return shift >= 0f ? color.Lightened(shift) : color.Darkened(-shift);
    }

    private void DrawFloorStone(Rect2 rect, VisibilityState state, int variant)
    {
        var ink = state == VisibilityState.Visible
            ? PresentationPalette.Brass.Darkened(0.58f)
            : WithAlpha(PresentationPalette.MemoryChalk, 0.28f);
        DrawRect(rect.Grow(-2f), WithAlpha(ink, state == VisibilityState.Visible ? 0.42f : 0.22f), false, 1f);

        var seamY = rect.Position.Y + CellSize * (0.31f + (variant * 0.075f));
        DrawLine(new Vector2(rect.Position.X + 6f, seamY), new Vector2(rect.End.X - 7f, seamY), ink, 1f);

        if (variant % 2 == 0)
        {
            var seamX = rect.Position.X + CellSize * (0.34f + (variant * 0.06f));
            DrawLine(new Vector2(seamX, rect.Position.Y + 7f), new Vector2(seamX, seamY), ink, 1f);
        }

        if (state == VisibilityState.Visible && variant is 1 or 4)
        {
            var start = rect.Position + new Vector2(12f + variant, 34f);
            DrawPolyline(new[]
            {
                start,
                start + new Vector2(6f, -5f),
                start + new Vector2(12f, -3f),
                start + new Vector2(17f, -9f)
            }, WithAlpha(PresentationPalette.Soot, 0.58f), 1.25f);
        }

        DrawCircle(rect.Position + new Vector2(9f + (variant * 6f), 10f + ((variant * 9f) % 25)), 1.1f, WithAlpha(ink, 0.42f));
    }

    private void DrawWallStone(Rect2 rect, VisibilityState state, int variant)
    {
        var block = rect.Grow(-6f);
        DrawRect(new Rect2(block.Position + new Vector2(3f, 4f), block.Size), WithAlpha(PresentationPalette.Soot, 0.82f));
        var stone = variant % 2 == 0 ? PresentationPalette.Wall.Lightened(0.035f) : PresentationPalette.Wall;
        DrawRect(block, stone);

        var edge = state == VisibilityState.Visible
            ? PresentationPalette.Brass.Darkened(0.18f)
            : WithAlpha(PresentationPalette.MemoryChalk, 0.46f);
        DrawLine(block.Position, new Vector2(block.End.X, block.Position.Y), edge, 2f);
        DrawLine(block.Position, new Vector2(block.Position.X, block.End.Y), WithAlpha(edge, 0.58f), 1f);
        DrawLine(new Vector2(block.Position.X, block.Position.Y + block.Size.Y * 0.52f),
            new Vector2(block.End.X, block.Position.Y + block.Size.Y * 0.52f), WithAlpha(edge, 0.38f), 1f);
        DrawCircle(block.Position + new Vector2(5f, 5f), 1.4f, edge);
        DrawCircle(block.End - new Vector2(5f, 5f), 1.4f, edge);
    }

    private void DrawFrameOrnaments(Rect2 boardRect)
    {
        var corner = 18f;
        var color = PresentationPalette.Wax;
        DrawLine(boardRect.Position - new Vector2(4f, 4f), boardRect.Position + new Vector2(corner, -4f), color, 2f);
        DrawLine(boardRect.Position - new Vector2(4f, 4f), boardRect.Position + new Vector2(-4f, corner), color, 2f);
        DrawLine(new Vector2(boardRect.End.X + 4f, boardRect.Position.Y - 4f), new Vector2(boardRect.End.X - corner, boardRect.Position.Y - 4f), color, 2f);
        DrawLine(new Vector2(boardRect.End.X + 4f, boardRect.Position.Y - 4f), new Vector2(boardRect.End.X + 4f, boardRect.Position.Y + corner), color, 2f);
        DrawLine(new Vector2(boardRect.Position.X - 4f, boardRect.End.Y + 4f), new Vector2(boardRect.Position.X + corner, boardRect.End.Y + 4f), color, 2f);
        DrawLine(new Vector2(boardRect.Position.X - 4f, boardRect.End.Y + 4f), new Vector2(boardRect.Position.X - 4f, boardRect.End.Y - corner), color, 2f);
        DrawLine(boardRect.End + new Vector2(4f, 4f), new Vector2(boardRect.End.X - corner, boardRect.End.Y + 4f), color, 2f);
        DrawLine(boardRect.End + new Vector2(4f, 4f), new Vector2(boardRect.End.X + 4f, boardRect.End.Y - corner), color, 2f);
    }

    private static Color WithAlpha(Color color, float alpha) => new(color.R, color.G, color.B, alpha);

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
