using System;
using System.IO;
using Wick.Data;
using Xunit;

namespace Wick.Tests;

public sealed class GameRootSceneTests
{
    [Fact]
    public void GameRoot_InstancesTheRuntimeControllers()
    {
        var scenePath = FindProjectFile("scenes", "game", "GameRoot.tscn");
        var scene = GodotTextSceneContract.ParseFile(scenePath);

        CampaignRuntimeContract.ValidateGameRoot(scene);
    }

    [Fact]
    public void Hud_ContainsVisiblePracticeAndUndoKeyboardStatus()
    {
        var scenePath = FindProjectFile("scenes", "ui", "Hud.tscn");
        var scene = GodotTextSceneContract.ParseFile(scenePath);

        CampaignRuntimeContract.ValidateHud(scene);
    }

    [Fact]
    public void CampaignScenes_UseTheCSharpExportPropertyNameGodotLoads()
    {
        for (var index = 1; index <= 7; index++)
        {
            var scenePath = FindProjectFile("scenes", "levels", $"Level{index:00}.tscn");
            var root = GodotTextSceneContract.ParseFile(scenePath).RequireNode($"Level{index:00}");

            Assert.True(root.Properties.TryGetValue("Index", out var value), $"Level {index} must serialize the C# Index export using exact casing.");
            Assert.Equal(index.ToString(), value);
        }
    }

    private static string FindProjectFile(params string[] relativePath)
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
