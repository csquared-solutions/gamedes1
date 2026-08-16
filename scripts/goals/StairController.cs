using System;
using Godot;
using Wick.Board;

namespace Wick.Goals;

public partial class StairController : Node2D
{
    private static readonly Vector2 BoardOrigin = new(152, 104);
    private const int CellSize = 48;

    public event Action Reached;
    public GridCoord Cell { get; private set; }

    public void Configure(GridCoord cell)
    {
        Cell = cell;
        Position = BoardOrigin + new Vector2((cell.X + 0.5f) * CellSize, (cell.Y + 0.5f) * CellSize);
    }

    public bool TryEnter(GridCoord playerCell)
    {
        if (playerCell != Cell)
        {
            return false;
        }

        Reached?.Invoke();
        return true;
    }
}
