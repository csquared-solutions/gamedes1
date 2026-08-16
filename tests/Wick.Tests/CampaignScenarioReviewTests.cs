using System.Collections.Generic;
using System.Linq;
using Wick.Data;
using Xunit;

namespace Wick.Tests;

public sealed class CampaignScenarioReviewTests
{
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
