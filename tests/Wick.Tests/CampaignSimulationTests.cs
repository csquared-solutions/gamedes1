using Wick.Data;
using Wick.Board;
using Xunit;

namespace Wick.Tests;

public sealed class CampaignSimulationTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    public void AuthoredLevel_HasHazardAwareWinningPlanInTargetWickBand(int levelIndex)
    {
        var level = new LevelLoader().Load(levelIndex);

        var result = CampaignSimulationHarness.FindWinningPlan(
            level,
            new SimulationRequirements(
                RequireFlare: level.Index == 1));

        Assert.True(result.Won, result.Summary);
        Assert.InRange(result.RemainingWick, level.TargetCompletionWickMin, level.TargetCompletionWickMax);
        Assert.InRange(result.ActionCount, 1, 45);
        if (level.RequiresLampPlacement)
        {
            Assert.True(result.PlacedLamp, $"Level {levelIndex} was solved without its required lamp-placement verb.");
        }
    }

    [Theory]
    [InlineData(1u)]
    [InlineData(2u)]
    public void Campaign_BothRandomTieBreakOutcomesPreserveWinningRoutes(uint tieBreakerSeed)
    {
        foreach (var level in new LevelLoader().LoadCampaign())
        {
            var result = CampaignSimulationHarness.FindWinningPlan(
                level,
                new SimulationRequirements(RequireFlare: level.Index == 1),
                tieBreakerSeed: tieBreakerSeed);

            Assert.True(result.Won, $"Seed {tieBreakerSeed}: {result.Summary}");
        }
    }

    [Fact]
    public void LevelOne_HasAWinningTeachingRouteForFlareStallAndForcedDarkFlaskLift()
    {
        var level = new LevelLoader().Load(1);
        var requirements = new SimulationRequirements(
            RequireFlare: true,
            RequireCrawlerStall: true,
            MinimumFlasksCollected: 1,
            CollectAllFlasks: true,
            FollowAuthoredRoute: true);

        var result = CampaignSimulationHarness.FindWinningPlan(level, requirements);

        Assert.True(result.Won, result.Summary);
        Assert.True(result.FiredFlare);
        Assert.True(result.ObservedCrawlerStall);
        Assert.True(result.ForcedDarkFlaskTurns >= 2);
    }

    [Fact]
    public void LevelOne_HasAWinningLateLampPlacementTeachingRoute()
    {
        var level = new LevelLoader().Load(1);
        var board = new BoardState(level.Width, level.Height, level.Walls);
        var corridor = LevelTopology.FindTerminalCorridor(board, level.StairCell);

        Assert.NotNull(corridor);

        var result = CampaignSimulationHarness.FindWinningPlan(
            level,
            new SimulationRequirements(
                CollectAllFlasks: true,
                RequireLampPlacement: true,
                MinimumLampPlacementAction: 13,
                MaximumLampPlacementAction: 13));

        Assert.True(result.Won, result.Summary);
        var placementIndex = result.Trace.ToList().FindIndex(step => step.Action == "P");
        Assert.True(placementIndex >= 0);
        var placement = result.Trace[placementIndex];
        Assert.Equal(corridor!.CommitmentPoint, placement.PlayerCell);
        Assert.False(placement.Lamp.IsCarried);

        var corridorMoves = result.Trace
            .Skip(placementIndex + 1)
            .Where(step => step.Action != "P" && step.Action != "F")
            .ToArray();
        Assert.Equal(corridor.Cells, corridorMoves.Select(step => step.PlayerCell));
        Assert.All(corridorMoves, step =>
        {
            Assert.False(step.Lamp.IsCarried);
            Assert.Equal(corridor.CommitmentPoint, step.Lamp.Cell);
        });
        Assert.Equal(level.StairCell, corridorMoves[^1].PlayerCell);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    public void RequiredLampLevel_HasNoWinningOutcomeWhenActualPlacementActionIsDisabled(int levelIndex)
    {
        var level = new LevelLoader().Load(levelIndex);

        var results = CampaignSimulationHarness.ReviewFlaskRouteVariants(
            level,
            new SimulationRequirements(AllowLampPlacement: false));

        Assert.DoesNotContain(results, result => result.Result.Won);
    }

    [Theory]
    [InlineData(1, 5)]
    [InlineData(2, 5)]
    [InlineData(3, 5)]
    [InlineData(4, 16)]
    [InlineData(5, 16)]
    [InlineData(6, 16)]
    [InlineData(7, 65)]
    public void AuthoredLevel_ReviewsEveryFlaskSubsetAndVisitOrderWithinFinitePlans(int levelIndex, int expectedPlans)
    {
        var level = new LevelLoader().Load(levelIndex);

        var results = CampaignSimulationHarness.ReviewFlaskRouteVariants(level, new SimulationRequirements());

        Assert.Equal(expectedPlans, results.Count);
        Assert.Contains(results, result => result.Result.Won);
        foreach (var flaskIndex in Enumerable.Range(0, level.Flasks.Length))
        {
            Assert.Contains(results, result => (result.FlaskMask & (1 << flaskIndex)) == 0);
            Assert.Contains(results, result => (result.FlaskMask & (1 << flaskIndex)) != 0);
        }
    }

    [Fact]
    public void StairEntry_UsesActualLampOwnershipRuleInsteadOfSimulationGoalMetadata()
    {
        var loader = new LevelLoader();
        var levelTwo = loader.Load(2);
        var levelThree = loader.Load(3);

        Assert.True(StairEntryRule.CanEnter(levelTwo, levelTwo.StairCell, lampIsCarried: true));
        Assert.False(StairEntryRule.CanEnter(levelThree, levelThree.StairCell, lampIsCarried: true));
        Assert.True(StairEntryRule.CanEnter(levelThree, levelThree.StairCell, lampIsCarried: false));
        Assert.True(StairEntryRule.IsBlockedByCarriedLamp(levelThree, levelThree.StairCell, lampIsCarried: true));
        Assert.False(StairEntryRule.IsBlockedByCarriedLamp(levelThree, levelThree.PlayerStart, lampIsCarried: true));
        Assert.False(StairEntryRule.IsBlockedByCarriedLamp(levelTwo, levelTwo.StairCell, lampIsCarried: true));
    }

    [Fact]
    public void LevelFour_SearchTerminatesWithinFiniteWickStateBoundAndFindsWinningPlan()
    {
        var level = new LevelLoader().Load(4);
        var finiteActionBound = level.StartingWick + (level.Flasks.Length * LevelTuning.FlaskRestore) - 1;

        var result = CampaignSimulationHarness.FindWinningPlan(
            level,
            new SimulationRequirements(CollectAllFlasks: true, FollowAuthoredRoute: true),
            maxActions: finiteActionBound,
            maxStates: 20_000);

        Assert.True(result.Won, result.Summary);
    }
}
