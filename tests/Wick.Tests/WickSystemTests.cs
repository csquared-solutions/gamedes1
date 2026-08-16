using Wick.Core;
using Xunit;

namespace Wick.Tests;

public sealed class WickSystemTests
{
    [Fact]
    public void SightRadius_IsFloorOfHalfWick()
    {
        var wick = new WickSystem();
        wick.Initialize(startingWick: 12);

        Assert.Equal(6, wick.CurrentRadius);
    }

    [Fact]
    public void Spend_ReducesWick_AndSignalsDepletionAtZero()
    {
        var wick = new WickSystem();
        wick.Initialize(startingWick: 1);

        Assert.True(wick.Spend(1));
        Assert.Equal(0, wick.CurrentWick);
        Assert.True(wick.IsDepleted);
    }
}
