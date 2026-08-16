using System;
using System.Collections.Generic;
using Wick.Board;

namespace Wick.Data;

public sealed class LevelLoader
{
    private readonly Dictionary<int, LevelDefinition> _levels;

    public LevelLoader(IEnumerable<LevelDefinition> levels = null)
    {
        _levels = new Dictionary<int, LevelDefinition>();
        foreach (var level in levels ?? new[] { CreateSingleRoom() })
        {
            _levels.Add(level.Index, level);
        }
    }

    public LevelDefinition Load(int index) => _levels.TryGetValue(index, out var level)
        ? level
        : throw new ArgumentOutOfRangeException(nameof(index), index, "No authored level exists at this index.");

    private static LevelDefinition CreateSingleRoom()
    {
        var walls = new List<GridCoord>();
        for (var x = 0; x < LevelTuning.GridWidth; x++)
        {
            walls.Add(new GridCoord(x, 0));
            walls.Add(new GridCoord(x, LevelTuning.GridHeight - 1));
        }

        for (var y = 1; y < LevelTuning.GridHeight - 1; y++)
        {
            walls.Add(new GridCoord(0, y));
            walls.Add(new GridCoord(LevelTuning.GridWidth - 1, y));
        }

        return new LevelDefinition
        {
            Index = 1,
            StartingWick = 12,
            PlayerStart = new GridCoord(2, 6),
            LampStart = new GridCoord(2, 6),
            OilFlaskCell = new GridCoord(7, 6),
            StairCell = new GridCoord(13, 6),
            Walls = walls.ToArray()
        };
    }
}
