using System;
using System.Collections.Generic;

namespace Wick.Board;

public sealed class BoardState
{
    private readonly HashSet<GridCoord> _walls = new();

    public int Width { get; }
    public int Height { get; }

    public BoardState(int width, int height, IEnumerable<GridCoord> walls = null)
    {
        Width = width;
        Height = height;

        if (walls is null)
        {
            return;
        }

        foreach (var wall in walls)
        {
            if (!IsInside(wall))
            {
                throw new ArgumentOutOfRangeException(nameof(walls), wall, "Walls must be inside the board.");
            }

            _walls.Add(wall);
        }
    }

    public bool IsInside(GridCoord cell) => cell.X >= 0 && cell.X < Width && cell.Y >= 0 && cell.Y < Height;

    public bool IsWall(GridCoord cell) => _walls.Contains(cell);

    public bool IsWalkable(GridCoord cell) => IsInside(cell) && !IsWall(cell);
}
