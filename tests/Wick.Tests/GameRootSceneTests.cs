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
