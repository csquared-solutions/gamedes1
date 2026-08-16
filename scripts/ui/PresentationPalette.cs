using System;
using Godot;

namespace Wick.Ui;

public static class PresentationPalette
{
    public static readonly Color HiddenFloor = new("08090b");
    public static readonly Color RememberedFloor = new("24221f");
    public static readonly Color VisibleFloor = new("51452f");
    public static readonly Color Wall = new("17191b");
    public static readonly Color Amber = new("d0a84e");
    public static readonly Color Flame = new("ffe6a3");
    public static readonly Color Player = new("fff4cf");
    public static readonly Color Danger = new("c65f42");

    public static double ContrastRatio(Color first, Color second)
    {
        var lighter = Math.Max(Luminance(first), Luminance(second));
        var darker = Math.Min(Luminance(first), Luminance(second));
        return (lighter + 0.05) / (darker + 0.05);
    }

    private static double Luminance(Color color) =>
        0.2126 * Linear(color.R) + 0.7152 * Linear(color.G) + 0.0722 * Linear(color.B);

    private static double Linear(float channel) => channel <= 0.04045f
        ? channel / 12.92
        : Math.Pow((channel + 0.055) / 1.055, 2.4);
}
