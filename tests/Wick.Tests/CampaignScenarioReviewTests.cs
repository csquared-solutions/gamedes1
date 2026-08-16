using System.Collections.Generic;
using System.Linq;
using Wick.Data;
using Xunit;

namespace Wick.Tests;

public sealed class CampaignScenarioReviewTests
{
    [Fact]
    public void Campaign_IndividualFlaskSubsetsAndRouteOrdersMatchReviewedOutcomes()
    {
        var rows = new List<string>();
        foreach (var level in new LevelLoader().LoadCampaign())
        {
            var reviews = CampaignSimulationHarness.ReviewFlaskRouteVariants(level, new SimulationRequirements());
            var outcomes = reviews
                .GroupBy(review => review.FlaskMask)
                .OrderBy(group => group.Key)
                .Select(group => $"{group.Key}:{group.Count(review => review.Result.Won)}/{group.Count()}");
            rows.Add($"L{level.Index} {string.Join(',', outcomes)}");
        }

        Assert.Equal(new[]
        {
            "L1 0:0/1,1:0/1,2:0/1,3:2/2",
            "L2 0:0/1,1:0/1,2:0/1,3:2/2",
            "L3 0:0/1,1:0/1,2:0/1,3:2/2",
            "L4 0:0/1,1:0/1,2:0/1,3:0/2,4:0/1,5:0/2,6:0/2,7:6/6",
            "L5 0:0/1,1:0/1,2:0/1,3:0/2,4:0/1,5:0/2,6:0/2,7:6/6",
            "L6 0:0/1,1:0/1,2:0/1,3:0/2,4:0/1,5:0/2,6:0/2,7:6/6",
            "L7 0:0/1,1:1/1,2:0/1,3:1/2,4:0/1,5:2/2,6:0/2,7:1/6,8:0/1,9:1/2,10:0/2,11:1/6,12:0/2,13:3/6,14:0/6,15:1/24"
        }, rows);
    }

    [Fact]
    public void Campaign_ScenarioMatrixMatchesReviewedTakingSkippingAndLampTimingOutcomes()
    {
        var rows = new List<(bool Take, int Wick, bool Skip, bool Early, bool Late)>();
        foreach (var level in new LevelLoader().LoadCampaign())
        {
            var takeAll = CampaignSimulationHarness.FindWinningPlan(level, new SimulationRequirements(
                RequireFlare: level.Index == 1,
                CollectAllFlasks: true,
                FollowAuthoredRoute: true));
            var skipAll = CampaignSimulationHarness.FindWinningPlan(level, new SimulationRequirements(
                SkipAllFlasks: true,
                FollowAuthoredRoute: true));
            var earlyLamp = CampaignSimulationHarness.FindWinningPlan(level, new SimulationRequirements(
                RequireFlare: level.Index == 1,
                CollectAllFlasks: true,
                MinimumLampPlacementAction: 1,
                MaximumLampPlacementAction: 5,
                FollowAuthoredRoute: true,
                RequireLampPlacement: true));
            var lateLamp = CampaignSimulationHarness.FindWinningPlan(level, new SimulationRequirements(
                RequireFlare: level.Index == 1,
                CollectAllFlasks: true,
                MinimumLampPlacementAction: 12,
                FollowAuthoredRoute: true,
                RequireLampPlacement: true));

            rows.Add((takeAll.Won, takeAll.RemainingWick, skipAll.Won, earlyLamp.Won, lateLamp.Won));
        }

        Assert.Equal(new[] { true, true, true, true, true, true, true }, rows.Select(row => row.Take));
        Assert.Equal(new[] { 2, 2, 3, 3, 3, 5, 3 }, rows.Select(row => row.Wick));
        Assert.Equal(new[] { false, false, false, false, false, false, false }, rows.Select(row => row.Skip));
        Assert.Equal(new[] { false, false, false, false, false, false, false }, rows.Select(row => row.Early));
        Assert.Equal(new[] { false, false, true, true, true, true, true }, rows.Select(row => row.Late));
    }
}
