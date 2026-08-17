using System;
using System.IO;
using System.Linq;
using Wick.Data;
using Xunit;

namespace Wick.Tests;

public sealed class PresentationContractTests
{
    [Fact]
    public void GameRoot_ProvidesWarningCuesAndQuietLoopingAmbience()
    {
        var scene = GodotTextSceneContract.ParseFile(ProjectFile("scenes", "game", "GameRoot.tscn"));

        Assert.Equal("res://scripts/audio/AudioCueController.cs", scene.ResolveNodeResourcePath(scene.RequireNode("AudioCues")));
        Assert.Equal("AudioStreamPlayer", scene.RequireNode("MothWakeCue").Type);
        Assert.Equal("AudioStreamPlayer", scene.RequireNode("LowWickCue").Type);
        var ambience = scene.RequireNode("BasementAmbience");
        Assert.Equal("AudioStreamPlayer", ambience.Type);
        Assert.Equal("true", ambience.Properties["autoplay"]);
        Assert.Equal(3, scene.Nodes.Count(node => node.Type == "AudioStreamPlayer"));
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
        Assert.True(File.Exists(ProjectFile("assets", "audio", "basement-ambience.wav")));
        var ambienceImport = File.ReadAllText(ProjectFile("assets", "audio", "basement-ambience.wav.import"));
        Assert.Contains("edit/loop_mode=1", ambienceImport, StringComparison.Ordinal);
        Assert.True(File.Exists(ProjectFile("assets", "fonts", "AtkinsonHyperlegible-Regular.ttf")));

        var attribution = File.ReadAllText(ProjectFile("docs", "attribution.md"));
        Assert.Contains("Atkinson Hyperlegible", attribution, StringComparison.Ordinal);
        Assert.Contains("SIL Open Font License 1.1", attribution, StringComparison.Ordinal);
        Assert.Contains("moth-wake.wav", attribution, StringComparison.Ordinal);
        Assert.Contains("low-wick-gutter.wav", attribution, StringComparison.Ordinal);
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
}
