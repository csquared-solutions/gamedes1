using System;
using Godot;

namespace Wick.Ui;

public static class PresentationPalette
{
    public static readonly Color Soot = new("0a0908");
    public static readonly Color StoneShadow = new("201d19");
    public static readonly Color Stone = new("473a29");
    public static readonly Color StoneHighlight = new("685333");
    public static readonly Color Brass = new("d8aa4e");
    public static readonly Color Wax = new("f7e9bb");
    public static readonly Color Ember = new("e06b42");
    public static readonly Color MemoryChalk = new("716655");

    public static readonly Color HiddenFloor = Soot;
    public static readonly Color RememberedFloor = StoneShadow;
    public static readonly Color VisibleFloor = Stone;
    public static readonly Color Wall = new("171512");
    public static readonly Color Amber = Brass;
    public static readonly Color Flame = Wax;
    public static readonly Color Player = new("fff4cf");
    public static readonly Color Danger = Ember;

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
