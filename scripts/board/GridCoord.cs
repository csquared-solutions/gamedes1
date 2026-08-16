namespace Wick.Board;

public readonly record struct GridCoord(int X, int Y)
{
    public static GridCoord operator +(GridCoord a, GridCoord b) => new(a.X + b.X, a.Y + b.Y);
}
