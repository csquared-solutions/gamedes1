using System;
using Wick.Board;

namespace Wick.Data;

public sealed class LevelDefinition
{
    public int Index { get; init; }
    public int StartingWick { get; init; }
    public GridCoord PlayerStart { get; init; }
    public GridCoord LampStart { get; init; }
    public GridCoord StairCell { get; init; }
    public GridCoord[] Walls { get; init; } = Array.Empty<GridCoord>();
    public GridCoord[] Crawlers { get; init; } = Array.Empty<GridCoord>();
    public GridCoord[] Moths { get; init; } = Array.Empty<GridCoord>();
    public GridCoord[] Flasks { get; init; } = Array.Empty<GridCoord>();
    public bool RequiresLampPlacement { get; init; }
    public int TargetCompletionWickMin { get; init; }
    public int TargetCompletionWickMax { get; init; }
    public int Width => LevelTuning.GridWidth;
    public int Height => LevelTuning.GridHeight;
}
