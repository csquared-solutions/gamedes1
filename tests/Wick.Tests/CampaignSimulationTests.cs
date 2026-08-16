using Wick.Data;
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

        var result = CampaignSimulationHarness.FindWinningPlan(
            level,
            new SimulationRequirements(
                CollectAllFlasks: true,
                RequireLampPlacement: true,
                MinimumLampPlacementAction: 12));

        Assert.True(result.Won, result.Summary);
        Assert.True(result.PlacedLamp);
        Assert.InRange(result.FirstLampPlacementAction, 12, 16);
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
