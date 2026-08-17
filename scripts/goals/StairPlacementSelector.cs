using System;
using System.Collections.Generic;
using System.Linq;
using Wick.Board;
using Wick.Data;

namespace Wick.Goals;

public sealed class StairPlacementSelector
{
    private readonly Random _random;
    private readonly Dictionary<int, GridCoord> _previousByLevel = new();

    public StairPlacementSelector(Random random = null)
    {
        _random = random ?? Random.Shared;
    }

    public LevelDefinition SelectForAttempt(LevelDefinition authoredLevel)
    {
        ArgumentNullException.ThrowIfNull(authoredLevel);
        var candidates = authoredLevel.StairCandidates.Distinct().ToArray();
        if (candidates.Length < 2)
        {
            throw new InvalidOperationException(
                $"Level {authoredLevel.Index} requires at least two distinct stair candidates.");
        }

        var options = _previousByLevel.TryGetValue(authoredLevel.Index, out var previous)
            ? candidates.Where(candidate => candidate != previous).ToArray()
            : candidates;
        var selected = options[_random.Next(options.Length)];
        _previousByLevel[authoredLevel.Index] = selected;
        return authoredLevel.WithStairCell(selected);
    }
}
