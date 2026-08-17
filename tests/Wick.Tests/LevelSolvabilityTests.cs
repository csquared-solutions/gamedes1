using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Wick.Board;
using Wick.Data;
using Xunit;

namespace Wick.Tests;

public sealed class LevelSolvabilityTests
{
    private static readonly GridCoord[] Directions =
    {
        new(0, -1),
        new(0, 1),
        new(-1, 0),
        new(1, 0)
    };

    [Fact]
    public void Campaign_HasExactlySevenLevelsWithRequiredWickBands()
    {
        var levels = new LevelLoader().LoadCampaign();

        Assert.Equal(7, levels.Count);
        Assert.Equal(new[] { 12, 12, 12, 14, 14, 16, 16 }, levels.Select(level => level.StartingWick));
        Assert.Equal(Enumerable.Range(1, 7), levels.Select(level => level.Index));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    public void Level_HasValidStartExitAndAuthoredTopology(int levelIndex)
    {
        var level = new LevelLoader().Load(levelIndex);
        var board = new BoardState(level.Width, level.Height, level.Walls);

        Assert.Equal(19, level.Width);
        Assert.Equal(13, level.Height);
        Assert.True(board.IsWalkable(level.PlayerStart));
        Assert.True(board.IsWalkable(level.LampStart));
        Assert.Equal(3, level.StairCandidates.Length);
        Assert.Equal(3, level.StairCandidates.Distinct().Count());
        Assert.Equal(level.StairCandidates[0], level.StairCell);
        Assert.All(level.StairCandidates, candidate =>
        {
            Assert.True(board.IsWalkable(candidate));
            Assert.NotNull(FindShortestPath(board, level.PlayerStart, candidate));
        });
        Assert.All(level.Crawlers, cell => Assert.True(board.IsWalkable(cell)));
        Assert.All(level.Moths, cell => Assert.True(board.IsWalkable(cell)));
        Assert.All(level.Flasks, cell => Assert.True(board.IsWalkable(cell)));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    public void Level_AllAuthoredObjectsConnectToThePlayableArea(int levelIndex)
    {
        var level = new LevelLoader().Load(levelIndex);
        var board = new BoardState(level.Width, level.Height, level.Walls);
        var reachable = FindReachableCells(board, level.PlayerStart);
        var authoredObjects = level.Crawlers
            .Concat(level.Moths)
            .Concat(level.Flasks)
            .Concat(level.StairCandidates);

        Assert.All(authoredObjects, cell =>
            Assert.True(reachable.Contains(cell), $"Level {levelIndex} object at {cell} is isolated by walls."));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    public void Level_AuthoredObjectsDoNotOverlapThePlayerOrEachOther(int levelIndex)
    {
        var level = new LevelLoader().Load(levelIndex);
        var occupied = level.Crawlers
            .Concat(level.Moths)
            .Concat(level.Flasks)
            .Concat(level.StairCandidates)
            .Append(level.PlayerStart)
            .ToArray();

        Assert.Equal(occupied.Length, occupied.Distinct().Count());
    }

    [Fact]
    public void Campaign_EscalatesHazardsAndRequiresLampPlacementFromLevelThree()
    {
        var levels = new LevelLoader().LoadCampaign();

        Assert.Equal(new[] { 2, 2, 2, 3, 3, 3, 4 }, levels.Select(level => level.Flasks.Length));
        Assert.Equal(new[] { 2, 2, 3, 3, 3, 4, 4 }, levels.Select(level => level.Crawlers.Length));
        Assert.Equal(new[] { 0, 1, 1, 2, 2, 3, 3 }, levels.Select(level => level.Moths.Length));
        Assert.All(levels.Take(2), level => Assert.False(level.RequiresLampPlacement));
        Assert.All(levels.Skip(2), level => Assert.True(level.RequiresLampPlacement));
        Assert.All(levels.Take(3), level =>
        {
            Assert.Equal(2, level.TargetCompletionWickMin);
            Assert.Equal(4, level.TargetCompletionWickMax);
        });
        Assert.All(levels.Skip(3), level =>
        {
            Assert.Equal(3, level.TargetCompletionWickMin);
            Assert.Equal(6, level.TargetCompletionWickMax);
        });
    }

    [Fact]
    public void Campaign_ScenesAreAddressableForEveryRuntimeLevel()
    {
        var loader = new LevelLoader();

        Assert.Equal(
            Enumerable.Range(1, 7).Select(index => $"res://scenes/levels/Level{index:00}.tscn"),
            loader.LoadCampaign().Select(level => loader.GetScenePath(level.Index)));
    }

    [Fact]
    public void Campaign_RuntimeScenePathsResolveToMatchingAuthoredLevelScenes()
    {
        var loader = new LevelLoader();
        var projectRoot = FindProjectRoot();

        foreach (var level in loader.LoadCampaign())
        {
            var resourcePath = loader.GetScenePath(level.Index);
            var filePath = Path.Combine(projectRoot, resourcePath["res://".Length..].Replace('/', Path.DirectorySeparatorChar));
            var scene = GodotTextSceneContract.ParseFile(filePath);
            CampaignRuntimeContract.ValidateLevelScene(scene, CampaignSceneManifest.Get(level.Index));
        }
    }

    [Fact]
    public void CampaignSceneContract_RejectsStructurallyMisparentedLevelRoot()
    {
        const string malformed = """
            [gd_scene format=3]
            [ext_resource type="Script" path="res://scripts/data/CampaignLevelScene.cs" id="1_level"]
            [node name="Level01" type="Node" parent="."]
            script = ExtResource("1_level")
            index = 1
            """;
        var scene = GodotTextSceneContract.Parse(malformed);

        Assert.Throws<InvalidDataException>(() =>
            CampaignRuntimeContract.ValidateLevelScene(scene, CampaignSceneManifest.Get(1)));
    }

    [Fact]
    public void LevelOne_HasReachableTopologicalDeadEndTeachingBranch()
    {
        var level = new LevelLoader().Load(1);
        var board = new BoardState(level.Width, level.Height, level.Walls);
        var reachable = FindReachableCells(board, level.PlayerStart);

        Assert.Contains(reachable, cell =>
            cell != level.PlayerStart &&
            cell != level.StairCell &&
            Directions.Count(direction => board.IsWalkable(cell + direction)) == 1);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    public void Level_HasAWinningResourceRouteWithoutTopologyDeadlock(int levelIndex)
    {
        var level = new LevelLoader().Load(levelIndex);
        var board = new BoardState(level.Width, level.Height, level.Walls);
        foreach (var candidate in level.StairCandidates)
        {
            var path = FindShortestPath(board, level.PlayerStart, candidate);
            Assert.NotNull(path);
            var requiredActionCost = path!.Count - 1 + (level.RequiresLampPlacement ? 1 : 0);
            var availableWick = level.StartingWick + (level.Flasks.Length * LevelTuning.FlaskRestore);
            Assert.True(requiredActionCost < availableWick,
                $"Level {levelIndex} candidate {candidate} has no wick margin.");
        }
    }

    private static IReadOnlyList<GridCoord>? FindShortestPath(BoardState board, GridCoord start, GridCoord goal)
    {
        var queue = new Queue<GridCoord>();
        var visited = new HashSet<GridCoord> { start };
        var previous = new Dictionary<GridCoord, GridCoord>();
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (current == goal)
            {
                var path = new List<GridCoord> { goal };
                while (path[^1] != start)
                {
                    path.Add(previous[path[^1]]);
                }

                path.Reverse();
                return path;
            }

            foreach (var direction in Directions)
            {
                var next = current + direction;
                if (board.IsWalkable(next) && visited.Add(next))
                {
                    previous[next] = current;
                    queue.Enqueue(next);
                }
            }
        }

        return null;
    }

    private static IReadOnlyCollection<GridCoord> FindReachableCells(BoardState board, GridCoord start)
    {
        var queue = new Queue<GridCoord>();
        var visited = new HashSet<GridCoord> { start };
        queue.Enqueue(start);
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var direction in Directions)
            {
                var next = current + direction;
                if (board.IsWalkable(next) && visited.Add(next))
                {
                    queue.Enqueue(next);
                }
            }
        }

        return visited;
    }

    private static string FindProjectRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Wick.csproj")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the Wick project root.");
    }
}
