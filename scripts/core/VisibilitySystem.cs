using System;
using System.Collections.Generic;
using System.Linq;
using Wick.Board;

namespace Wick.Core;

public sealed class VisibilitySystem
{
    private readonly BoardState _board;
    private readonly HashSet<GridCoord> _visible = new();
    private readonly HashSet<GridCoord> _seen = new();

    public VisibilitySystem(BoardState board) => _board = board;

    public VisibilityResult Recompute(GridCoord lampCell, int radius)
    {
        _visible.Clear();

        if (!_board.IsInside(lampCell) || radius < 0)
        {
            return CreateResult();
        }

        for (var y = 0; y < _board.Height; y++)
        for (var x = 0; x < _board.Width; x++)
        {
            var cell = new GridCoord(x, y);
            var distance = Math.Abs(cell.X - lampCell.X) + Math.Abs(cell.Y - lampCell.Y);
            if (distance <= radius && HasLineOfSight(lampCell, cell))
            {
                _visible.Add(cell);
                _seen.Add(cell);
            }
        }

        return CreateResult();
    }

    public bool IsVisible(GridCoord cell) => _visible.Contains(cell);

    public bool WasSeen(GridCoord cell) => _seen.Contains(cell);

    private VisibilityResult CreateResult() => new(_visible.ToArray(), _seen.ToArray());

    private bool HasLineOfSight(GridCoord from, GridCoord to)
    {
        if (!_board.IsInside(from) || !_board.IsInside(to))
        {
            return false;
        }

        var x = from.X;
        var y = from.Y;
        var deltaX = to.X - from.X;
        var deltaY = to.Y - from.Y;
        var stepX = Math.Sign(deltaX);
        var stepY = Math.Sign(deltaY);
        var distanceX = Math.Abs(deltaX);
        var distanceY = Math.Abs(deltaY);
        var tDeltaX = distanceX == 0 ? double.PositiveInfinity : 1d / distanceX;
        var tDeltaY = distanceY == 0 ? double.PositiveInfinity : 1d / distanceY;
        var tMaxX = tDeltaX / 2d;
        var tMaxY = tDeltaY / 2d;

        while (x != to.X || y != to.Y)
        {
            if (tMaxX < tMaxY)
            {
                x += stepX;
                tMaxX += tDeltaX;
            }
            else if (tMaxY < tMaxX)
            {
                y += stepY;
                tMaxY += tDeltaY;
            }
            else
            {
                x += stepX;
                y += stepY;
                tMaxX += tDeltaX;
                tMaxY += tDeltaY;
            }

            var cell = new GridCoord(x, y);
            if (cell != to && _board.IsWall(cell))
            {
                return false;
            }
        }

        return true;
    }
}

public sealed record VisibilityResult(IReadOnlyCollection<GridCoord> Visible, IReadOnlyCollection<GridCoord> Seen);
