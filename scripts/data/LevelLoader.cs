using System;
using System.Collections.Generic;
using System.Linq;
using Wick.Board;

namespace Wick.Data;

public sealed class LevelLoader
{
    private readonly Dictionary<int, LevelDefinition> _levels;

    public LevelLoader(IEnumerable<LevelDefinition> levels = null)
    {
        _levels = new Dictionary<int, LevelDefinition>();
        foreach (var level in levels ?? CreateCampaign())
        {
            _levels.Add(level.Index, level);
        }
    }

    public LevelDefinition Load(int index) => _levels.TryGetValue(index, out var level)
        ? level
        : throw new ArgumentOutOfRangeException(nameof(index), index, "No authored level exists at this index.");

    public IReadOnlyList<LevelDefinition> LoadCampaign() => _levels.Values.OrderBy(level => level.Index).ToArray();

    public string GetScenePath(int index)
    {
        Load(index);
        return CampaignSceneManifest.Get(index).ResourcePath;
    }

    private static IEnumerable<LevelDefinition> CreateCampaign()
    {
        yield return CreateLevel(
            1, 12, new GridCoord(2, 6), new[] { new GridCoord(14, 9), new GridCoord(6, 1), new GridCoord(4, 11) },
            crawlers: new[] { new GridCoord(2, 2), new GridCoord(2, 10) },
            moths: Array.Empty<GridCoord>(),
            flasks: new[] { new GridCoord(6, 6), new GridCoord(10, 8) },
            interiorWalls: Combine(
                VerticalWall(5, 1, 5, 3),
                VerticalWall(5, 8, 11, 9),
                VerticalWall(11, 2, 8, 6),
                new[]
                {
                    new GridCoord(3, 4), new GridCoord(4, 3),
                    new GridCoord(12, 8), new GridCoord(13, 8), new GridCoord(14, 8),
                    new GridCoord(12, 10), new GridCoord(13, 10), new GridCoord(14, 10),
                    new GridCoord(15, 9)
                }));
        yield return CreateLevel(
            2, 12, new GridCoord(2, 9), new[] { new GridCoord(14, 3), new GridCoord(1, 1), new GridCoord(7, 11) },
            crawlers: new[] { new GridCoord(2, 11), new GridCoord(4, 11) },
            moths: new[] { new GridCoord(16, 11) },
            flasks: new[] { new GridCoord(6, 9), new GridCoord(9, 6) },
            interiorWalls: Combine(
                HorizontalWall(3, 8, 5, 7),
                VerticalWall(10, 2, 10, 4),
                HorizontalWall(11, 16, 7, 14),
                new[] { new GridCoord(15, 11), new GridCoord(17, 11) }));
        yield return CreateLevel(
            3, 12, new GridCoord(2, 6), new[] { new GridCoord(16, 6), new GridCoord(4, 1), new GridCoord(4, 11) },
            crawlers: new[] { new GridCoord(2, 2), new GridCoord(2, 10), new GridCoord(4, 2) },
            moths: new[] { new GridCoord(1, 1) },
            flasks: new[] { new GridCoord(6, 6), new GridCoord(11, 6) },
            interiorWalls: Combine(
                VerticalWall(6, 1, 5, 3),
                VerticalWall(6, 7, 11, 9),
                VerticalWall(12, 2, 10, 6),
                new[]
                {
                    new GridCoord(9, 6),
                    new GridCoord(16, 1),
                    new GridCoord(14, 11), new GridCoord(16, 11)
                }),
            requiresLampPlacement: true);
        yield return CreateLevel(
            4, 14, new GridCoord(2, 10), new[] { new GridCoord(16, 2), new GridCoord(8, 11), new GridCoord(4, 5) },
            crawlers: new[] { new GridCoord(2, 2), new GridCoord(2, 11), new GridCoord(4, 2) },
            moths: new[] { new GridCoord(1, 1), new GridCoord(1, 6) },
            flasks: new[] { new GridCoord(5, 10), new GridCoord(9, 7), new GridCoord(13, 4) },
            interiorWalls: Combine(
                HorizontalWall(2, 8, 6, 5),
                VerticalWall(9, 2, 10, 7),
                HorizontalWall(10, 16, 7, 10, 13),
                new[]
                {
                    new GridCoord(12, 1), new GridCoord(14, 1),
                    new GridCoord(14, 11), new GridCoord(16, 11),
                    new GridCoord(16, 1)
                }),
            requiresLampPlacement: true);
        yield return CreateLevel(
            5, 14, new GridCoord(2, 2), new[] { new GridCoord(16, 10), new GridCoord(8, 1), new GridCoord(1, 8) },
            crawlers: new[] { new GridCoord(2, 10), new GridCoord(2, 11), new GridCoord(4, 10) },
            moths: new[] { new GridCoord(1, 11), new GridCoord(1, 7) },
            flasks: new[] { new GridCoord(5, 3), new GridCoord(10, 6), new GridCoord(14, 8) },
            interiorWalls: Combine(
                VerticalWall(6, 2, 10, 3),
                HorizontalWall(7, 13, 6, 10),
                VerticalWall(14, 2, 9, 8),
                new[]
                {
                    new GridCoord(12, 1), new GridCoord(14, 1),
                    new GridCoord(14, 11), new GridCoord(16, 11),
                    new GridCoord(16, 1)
                }),
            requiresLampPlacement: true);
        yield return CreateLevel(
            6, 16, new GridCoord(2, 10), new[] { new GridCoord(16, 2), new GridCoord(1, 4), new GridCoord(8, 11) },
            crawlers: new[] { new GridCoord(2, 2), new GridCoord(4, 2), new GridCoord(6, 2), new GridCoord(17, 11) },
            moths: new[] { new GridCoord(1, 1), new GridCoord(15, 11), new GridCoord(17, 6) },
            flasks: new[] { new GridCoord(5, 9), new GridCoord(8, 7), new GridCoord(13, 4) },
            interiorWalls: Combine(
                HorizontalWall(2, 7, 5, 4),
                VerticalWall(8, 2, 10, 7),
                HorizontalWall(9, 16, 7, 9, 12),
                VerticalWall(13, 2, 6, 2, 4),
                new[]
                {
                    new GridCoord(10, 1), new GridCoord(12, 1),
                    new GridCoord(14, 1), new GridCoord(16, 1),
                    new GridCoord(14, 11), new GridCoord(16, 11)
                }),
            requiresLampPlacement: true);
        yield return CreateLevel(
            7, 16, new GridCoord(2, 6), new[] { new GridCoord(16, 6), new GridCoord(6, 1), new GridCoord(6, 11) },
            crawlers: new[] { new GridCoord(2, 2), new GridCoord(2, 10), new GridCoord(4, 2), new GridCoord(4, 10) },
            moths: new[] { new GridCoord(1, 1), new GridCoord(1, 11), new GridCoord(17, 11) },
            flasks: new[] { new GridCoord(6, 5), new GridCoord(8, 8), new GridCoord(12, 4), new GridCoord(15, 8) },
            interiorWalls: Combine(
                VerticalWall(5, 1, 5, 3),
                VerticalWall(5, 7, 11, 9),
                HorizontalWall(6, 11, 3, 8),
                HorizontalWall(7, 12, 9, 9),
                VerticalWall(13, 2, 10, 2, 6),
                new[]
                {
                    new GridCoord(12, 1), new GridCoord(14, 1),
                    new GridCoord(16, 1),
                    new GridCoord(14, 11), new GridCoord(16, 11)
                }),
            requiresLampPlacement: true);
    }

    private static LevelDefinition CreateLevel(
        int index,
        int startingWick,
        GridCoord playerStart,
        GridCoord[] stairCandidates,
        GridCoord[] crawlers,
        GridCoord[] moths,
        GridCoord[] flasks,
        GridCoord[] interiorWalls,
        bool requiresLampPlacement = false)
    {
        if (stairCandidates is null || stairCandidates.Length != 3 || stairCandidates.Distinct().Count() != 3)
        {
            throw new ArgumentException("Campaign levels require exactly three distinct stair candidates.", nameof(stairCandidates));
        }

        var targetMin = index <= 3 ? 2 : 3;
        var targetMax = index <= 3 ? 4 : 6;
        return new LevelDefinition
        {
            Index = index,
            StartingWick = startingWick,
            PlayerStart = playerStart,
            LampStart = playerStart,
            StairCell = stairCandidates[0],
            StairCandidates = [.. stairCandidates],
            Walls = Combine(CreateBoundaryWalls(), interiorWalls),
            Crawlers = crawlers,
            Moths = moths,
            Flasks = flasks,
            RequiresLampPlacement = requiresLampPlacement,
            TargetCompletionWickMin = targetMin,
            TargetCompletionWickMax = targetMax
        };
    }

    private static GridCoord[] CreateBoundaryWalls()
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

        return walls.ToArray();
    }

    private static GridCoord[] HorizontalWall(int fromX, int toX, int y, params int[] openings)
    {
        var open = new HashSet<int>(openings);
        return Enumerable.Range(fromX, toX - fromX + 1)
            .Where(x => !open.Contains(x))
            .Select(x => new GridCoord(x, y))
            .ToArray();
    }

    private static GridCoord[] VerticalWall(int x, int fromY, int toY, params int[] openings)
    {
        var open = new HashSet<int>(openings);
        return Enumerable.Range(fromY, toY - fromY + 1)
            .Where(y => !open.Contains(y))
            .Select(y => new GridCoord(x, y))
            .ToArray();
    }

    private static GridCoord[] Combine(params GridCoord[][] groups) => groups.SelectMany(group => group).Distinct().ToArray();
}
