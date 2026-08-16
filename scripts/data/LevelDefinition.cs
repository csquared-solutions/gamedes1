using System;
using Wick.Board;

namespace Wick.Data;

public sealed class LevelDefinition
{
    public int Index { get; init; }
    public int StartingWick { get; init; }
    public GridCoord PlayerStart { get; init; }
    public GridCoord LampStart { get; init; }
    public GridCoord OilFlaskCell { get; init; }
    public GridCoord StairCell { get; init; }
    public GridCoord[] Walls { get; init; } = Array.Empty<GridCoord>();
    public int Width => LevelTuning.GridWidth;
    public int Height => LevelTuning.GridHeight;
}
