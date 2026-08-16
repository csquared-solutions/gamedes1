using System;
using Godot;
using Wick.Board;
using Wick.Core;

namespace Wick.Goals;

public partial class StairController : Node2D
{
    private static readonly Vector2 BoardOrigin = new(152, 104);
    private const int CellSize = 48;
    private VisibilitySystem _visibility;

    public event Action Reached;
    public GridCoord Cell { get; private set; }

    public void Configure(GridCoord cell, VisibilitySystem visibility)
    {
        Cell = cell;
        _visibility = visibility;
        Position = BoardOrigin + new Vector2((cell.X + 0.5f) * CellSize, (cell.Y + 0.5f) * CellSize);
        RefreshVisibility();
    }

    public void RefreshVisibility() => Visible = _visibility is not null &&
        WorldMarkerReadability.ShouldShow(_visibility, Cell, isConsumed: false);

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
