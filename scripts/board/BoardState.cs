namespace Wick.Board;

public sealed class BoardState
{
    public int Width { get; }
    public int Height { get; }

    public BoardState(int width, int height)
    {
        Width = width;
        Height = height;
    }

    public bool IsInside(GridCoord cell) => cell.X >= 0 && cell.X < Width && cell.Y >= 0 && cell.Y < Height;
}
