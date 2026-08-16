using Wick.Core;
using Xunit;

namespace Wick.Tests;

public sealed class WickSystemTests
{
    [Fact]
    public void SightRadius_IsFloorOfHalfWick()
    {
        var wick = new WickState();
        wick.Initialize(startingWick: 12);

        Assert.Equal(6, wick.CurrentRadius);
    }

    [Fact]
    public void Spend_ReducesWick_AndSignalsDepletionAtZero()
    {
        var wick = new WickState();
        wick.Initialize(startingWick: 1);

        Assert.True(wick.Spend(1));
        Assert.Equal(0, wick.CurrentWick);
        Assert.True(wick.IsDepleted);
    }

    [Fact]
    public void Spend_ReachingZero_RaisesWickChangedAndDepleted()
    {
        var wick = new WickState();
        var wickChanges = new List<int>();
        var depleted = 0;
        wick.WickChanged += wickChanges.Add;
        wick.Depleted += () => depleted++;

        wick.Initialize(startingWick: 1);
        wick.Spend(1);

        Assert.Equal(new[] { 1, 0 }, wickChanges);
        Assert.Equal(1, depleted);
    }

    [Fact]
    public void Flare_SpendsThreeWick_AndDoublesRadiusForOneTurn()
    {
        var wick = new WickState();
        wick.Initialize(startingWick: 12);

        Assert.True(wick.TryFireFlare());
        Assert.Equal(9, wick.CurrentWick);
        Assert.Equal(8, wick.CurrentRadius);

        wick.AdvanceTurn();

        Assert.Equal(4, wick.CurrentRadius);
    }

    [Fact]
    public void Initialize_ClearsAnActiveFlareModifier()
    {
        var wick = new WickState();
        wick.Initialize(startingWick: 12);
        wick.TryFireFlare();

        wick.Initialize(startingWick: 12);

        Assert.Equal(6, wick.CurrentRadius);
        Assert.False(wick.IsFlareActive);
    }
}
