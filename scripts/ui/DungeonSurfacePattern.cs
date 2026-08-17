using System;
using Wick.Board;

namespace Wick.Ui;

public static class DungeonSurfacePattern
{
    public static int StoneVariant(GridCoord cell)
    {
        var hash = unchecked((uint)(cell.X * 73_856_093) ^ (uint)(cell.Y * 19_349_663));
        return (int)(hash % 5);
    }

    public static float LightStrength(GridCoord cell, GridCoord lampCell, int radius)
    {
        if (radius <= 0)
        {
            return 0f;
        }

        var distance = Math.Abs(cell.X - lampCell.X) + Math.Abs(cell.Y - lampCell.Y);
        return distance > radius ? 0f : 1f - ((float)distance / (radius + 1));
    }
}
