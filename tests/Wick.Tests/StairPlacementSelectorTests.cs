using Wick.Board;
using Wick.Data;
using Wick.Goals;
using Xunit;

namespace Wick.Tests;

public sealed class StairPlacementSelectorTests
{
    [Fact]
    public void SelectForAttempt_UsesCandidatesWithoutRepeatingThePreviousAttempt()
    {
        var level = CreateLevel();
        var selector = new StairPlacementSelector(new Random(17));
        GridCoord? previous = null;

        for (var attempt = 0; attempt < 30; attempt++)
        {
            var active = selector.SelectForAttempt(level);

            Assert.Contains(active.StairCell, level.StairCandidates);
            if (previous.HasValue)
            {
                Assert.NotEqual(previous.Value, active.StairCell);
            }

            previous = active.StairCell;
        }
    }

    [Fact]
    public void SelectForAttempt_ProducesTheSameSequenceForTheSameSeed()
    {
        var level = CreateLevel();
        var first = new StairPlacementSelector(new Random(29));
        var second = new StairPlacementSelector(new Random(29));

        var firstSequence = Enumerable.Range(0, 12)
            .Select(_ => first.SelectForAttempt(level).StairCell)
            .ToArray();
        var secondSequence = Enumerable.Range(0, 12)
            .Select(_ => second.SelectForAttempt(level).StairCell)
            .ToArray();

        Assert.Equal(firstSequence, secondSequence);
    }

    [Fact]
    public void SelectForAttempt_ReturnsAnActiveCopyWithoutMutatingAuthoredData()
    {
        var level = CreateLevel();
        var originalStair = level.StairCell;
        var originalCandidates = level.StairCandidates.ToArray();
        var selector = new StairPlacementSelector(new Random(3));

        var active = selector.SelectForAttempt(level);

        Assert.NotSame(level, active);
        Assert.Equal(originalStair, level.StairCell);
        Assert.Equal(originalCandidates, level.StairCandidates);
        Assert.Equal(originalCandidates, active.StairCandidates);
    }

    [Fact]
    public void SelectForAttempt_RejectsARepeatedOrSingleCandidatePool()
    {
        var level = CreateLevel(
            2,
            new GridCoord(8, 6),
            new GridCoord(8, 6));
        var selector = new StairPlacementSelector(new Random(1));

        Assert.Throws<InvalidOperationException>(() => selector.SelectForAttempt(level));
    }

    private static LevelDefinition CreateLevel(int index = 2, params GridCoord[] candidates)
    {
        var defaultCandidates = new[]
        {
            new GridCoord(14, 3),
            new GridCoord(1, 1),
            new GridCoord(7, 11)
        };
        var stairCandidates = candidates.Length == 0 ? defaultCandidates : candidates;
        return new LevelDefinition
        {
            Index = index,
            StartingWick = 12,
            PlayerStart = new GridCoord(2, 9),
            LampStart = new GridCoord(2, 9),
            StairCell = stairCandidates[0],
            StairCandidates = stairCandidates
        };
    }
}
