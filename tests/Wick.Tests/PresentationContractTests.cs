using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Wick.Data;
using Xunit;

namespace Wick.Tests;

public sealed class PresentationContractTests
{
    [Fact]
    public void GameRoot_ProvidesGameplayCuesAndQuietLoopingAmbience()
    {
        var scene = GodotTextSceneContract.ParseFile(ProjectFile("scenes", "game", "GameRoot.tscn"));

        Assert.Equal("res://scripts/audio/AudioCueController.cs", scene.ResolveNodeResourcePath(scene.RequireNode("AudioCues")));
        Assert.Equal("AudioStreamPlayer", scene.RequireNode("MothWakeCue").Type);
        Assert.Equal("AudioStreamPlayer", scene.RequireNode("LowWickCue").Type);
        Assert.Equal("AudioStreamPlayer", scene.RequireNode("LightOutCue").Type);
        var ambience = scene.RequireNode("BasementAmbience");
        Assert.Equal("AudioStreamPlayer", ambience.Type);
        Assert.Equal("true", ambience.Properties["autoplay"]);
        Assert.Equal(4, scene.Nodes.Count(node => node.Type == "AudioStreamPlayer"));
    }

    [Fact]
    public void BasementAmbience_IsAudibleWithoutOverpoweringGameplayCues()
    {
        var scene = GodotTextSceneContract.ParseFile(ProjectFile("scenes", "game", "GameRoot.tscn"));
        var playerVolumeDb = double.Parse(
            scene.RequireNode("BasementAmbience").Properties["volume_db"],
            CultureInfo.InvariantCulture);
        var trackRms = ReadMono16BitWaveRms(ProjectFile("assets", "audio", "basement-ambience.wav"));
        var effectiveRmsDb = 20.0 * Math.Log10(trackRms) + playerVolumeDb;

        Assert.InRange(effectiveRmsDb, -42.0, -30.0);
    }

    [Fact]
    public void Hud_IsAHorizontalStatusRailAndIncludesTheRequiredContentNote()
    {
        var scene = GodotTextSceneContract.ParseFile(ProjectFile("scenes", "ui", "Hud.tscn"));

        Assert.Equal("GridContainer", scene.RequireNode("Content").Type);
        var contentNote = scene.RequireNode("ContentNote");
        Assert.Contains("darkness", contentNote.Properties["text"], StringComparison.OrdinalIgnoreCase);
        Assert.Contains("insects", contentNote.Properties["text"], StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Label", scene.RequireNode("StartPrompt").Type);
    }

    [Theory]
    [InlineData("scenes/actors/Player.tscn", "WickSilhouette")]
    [InlineData("scenes/actors/Lamp.tscn", "FlameSilhouette")]
    [InlineData("scenes/hazards/Crawler.tscn", "CrawlerSilhouette")]
    [InlineData("scenes/hazards/Moth.tscn", "MothSilhouette")]
    [InlineData("scenes/pickups/OilFlask.tscn", "FlaskSilhouette")]
    [InlineData("scenes/goals/Stair.tscn", "StairSilhouette")]
    public void GameplayMarkers_HaveNamedReadableSilhouettes(string relativePath, string nodeName)
    {
        var scene = GodotTextSceneContract.ParseFile(ProjectFile(relativePath.Split('/')));

        Assert.NotNull(scene.RequireNode(nodeName));
    }

    [Theory]
    [InlineData("scenes/hazards/Crawler.tscn")]
    [InlineData("scenes/hazards/Moth.tscn")]
    public void HazardMarkers_CarryAStateRing(string relativePath)
    {
        var scene = GodotTextSceneContract.ParseFile(ProjectFile(relativePath.Split('/')));

        Assert.Equal("Line2D", scene.RequireNode("StateRing").Type);
    }

    [Theory]
    [InlineData("scenes/actors/Player.tscn", "Hood")]
    [InlineData("scenes/actors/Lamp.tscn", "GlowHalo")]
    [InlineData("scenes/hazards/Crawler.tscn", "Legs")]
    [InlineData("scenes/hazards/Moth.tscn", "WingVeins")]
    [InlineData("scenes/pickups/OilFlask.tscn", "GlassOutline")]
    [InlineData("scenes/goals/Stair.tscn", "Arch")]
    public void GameplayMarkers_IncludeSootAndGoldIllustrationDetails(string relativePath, string nodeName)
    {
        var scene = GodotTextSceneContract.ParseFile(ProjectFile(relativePath.Split('/')));

        Assert.NotNull(scene.RequireNode(nodeName));
    }

    [Fact]
    public void StartScreen_UsesTheAuthoredDungeonBackdrop()
    {
        Assert.True(File.Exists(ProjectFile("assets", "art", "start-backdrop.svg")));
        var scene = GodotTextSceneContract.ParseFile(ProjectFile("scenes", "ui", "Hud.tscn"));

        Assert.Equal("TextureRect", scene.RequireNode("DungeonBackdrop").Type);
    }

    [Fact]
    public void SubmissionAssetsAndAttribution_ArePresent()
    {
        Assert.True(File.Exists(ProjectFile("assets", "art", "wick-mark.svg")));
        Assert.True(File.Exists(ProjectFile("assets", "audio", "moth-wake.wav")));
        Assert.True(File.Exists(ProjectFile("assets", "audio", "low-wick-gutter.wav")));
        Assert.True(File.Exists(ProjectFile("assets", "audio", "light-out.wav")));
        Assert.True(File.Exists(ProjectFile("assets", "audio", "basement-ambience.wav")));
        var ambienceImport = File.ReadAllText(ProjectFile("assets", "audio", "basement-ambience.wav.import"));
        Assert.Contains("edit/loop_mode=1", ambienceImport, StringComparison.Ordinal);
        Assert.True(File.Exists(ProjectFile("assets", "fonts", "AtkinsonHyperlegible-Regular.ttf")));

        var attribution = File.ReadAllText(ProjectFile("docs", "attribution.md"));
        Assert.Contains("Atkinson Hyperlegible", attribution, StringComparison.Ordinal);
        Assert.Contains("SIL Open Font License 1.1", attribution, StringComparison.Ordinal);
        Assert.Contains("moth-wake.wav", attribution, StringComparison.Ordinal);
        Assert.Contains("low-wick-gutter.wav", attribution, StringComparison.Ordinal);
        Assert.Contains("light-out.wav", attribution, StringComparison.Ordinal);
        Assert.Contains("basement-ambience.wav", attribution, StringComparison.Ordinal);
    }

    private static string ProjectFile(params string[] relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Wick.csproj")))
            {
                return Path.Combine(new[] { directory.FullName }.Concat(relativePath).ToArray());
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the Wick project root.");
    }

    private static double ReadMono16BitWaveRms(string path)
    {
        using var stream = File.OpenRead(path);
        using var reader = new BinaryReader(stream);
        stream.Position = 44;
        var sumOfSquares = 0.0;
        var sampleCount = 0;

        while (stream.Position + sizeof(short) <= stream.Length)
        {
            var sample = reader.ReadInt16() / 32768.0;
            sumOfSquares += sample * sample;
            sampleCount++;
        }

        return Math.Sqrt(sumOfSquares / sampleCount);
    }
}
