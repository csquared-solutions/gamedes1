using Wick.Ui;
using Xunit;

namespace Wick.Tests;

public sealed class PresentationPaletteTests
{
    [Fact]
    public void PlayerAndFlame_RemainHighContrastAgainstEveryFloorState()
    {
        Assert.True(PresentationPalette.ContrastRatio(PresentationPalette.Player, PresentationPalette.HiddenFloor) >= 7.0);
        Assert.True(PresentationPalette.ContrastRatio(PresentationPalette.Player, PresentationPalette.RememberedFloor) >= 4.5);
        Assert.True(PresentationPalette.ContrastRatio(PresentationPalette.Player, PresentationPalette.VisibleFloor) >= 4.5);
        Assert.True(PresentationPalette.ContrastRatio(PresentationPalette.Flame, PresentationPalette.HiddenFloor) >= 7.0);
    }

    [Fact]
    public void GaugeVisuals_FlickerOnlyInTheFinalThreeAndPulseOnMothWake()
    {
        Assert.Equal(1f, WickGaugeVisualState.GetBrightness(wick: 4, mothPulseRemaining: 0f, phase: 0f));
        Assert.NotEqual(
            WickGaugeVisualState.GetBrightness(wick: 3, mothPulseRemaining: 0f, phase: 0f),
            WickGaugeVisualState.GetBrightness(wick: 3, mothPulseRemaining: 0f, phase: 0.25f));
        Assert.True(WickGaugeVisualState.GetBrightness(wick: 8, mothPulseRemaining: 0.5f, phase: 0f) > 1f);
    }

    [Fact]
    public void PresentationHolds_AreLongEnoughToReadWithoutAdvancingTheSimulation()
    {
        Assert.InRange(PresentationTiming.FlaskDarknessSeconds, 0.5, 1.5);
        Assert.InRange(PresentationTiming.EndStateSeconds, 0.75, 2.0);
    }
}
