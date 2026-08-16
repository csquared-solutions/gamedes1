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
                RequireFlare: level.Index == 1,
                CollectAllFlasks: true,
                FollowAuthoredRoute: true));

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
