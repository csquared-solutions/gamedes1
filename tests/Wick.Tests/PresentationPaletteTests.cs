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
    public void SootAndGoldMaterials_PreserveReadableContrast()
    {
        Assert.True(PresentationPalette.ContrastRatio(PresentationPalette.Brass, PresentationPalette.Soot) >= 7.0);
        Assert.True(PresentationPalette.ContrastRatio(PresentationPalette.Wax, PresentationPalette.StoneShadow) >= 7.0);
        Assert.True(PresentationPalette.ContrastRatio(PresentationPalette.Ember, PresentationPalette.HiddenFloor) >= 4.5);
    }

    [Fact]
    public void DungeonSurfacePattern_IsDeterministicAndLightFallsOffFromTheLamp()
    {
        var cell = new Wick.Board.GridCoord(7, 4);

        Assert.Equal(DungeonSurfacePattern.StoneVariant(cell), DungeonSurfacePattern.StoneVariant(cell));
        Assert.InRange(DungeonSurfacePattern.StoneVariant(cell), 0, 4);
        Assert.True(DungeonSurfacePattern.LightStrength(cell, cell, radius: 5) >
            DungeonSurfacePattern.LightStrength(new Wick.Board.GridCoord(9, 4), cell, radius: 5));
        Assert.Equal(0f, DungeonSurfacePattern.LightStrength(new Wick.Board.GridCoord(13, 4), cell, radius: 5));
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
