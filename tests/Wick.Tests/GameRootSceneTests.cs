using System;
using System.IO;
using System.Linq;
using Xunit;

namespace Wick.Tests;

public sealed class GameRootSceneTests
{
    [Fact]
    public void GameRoot_InstancesTheTaskFourRuntimeControllers()
    {
        var scenePath = FindProjectFile("scenes", "game", "GameRoot.tscn");
        var scene = File.ReadAllText(scenePath);

        Assert.Contains("res://scripts/core/LevelManager.cs", scene);
        Assert.Contains("res://scenes/actors/Player.tscn", scene);
        Assert.Contains("res://scenes/actors/Lamp.tscn", scene);
        Assert.Contains("res://scenes/goals/Stair.tscn", scene);
        Assert.Contains("[node name=\"Player\" parent=\".\"", scene);
        Assert.Contains("[node name=\"Lamp\" parent=\".\"", scene);
        Assert.Contains("[node name=\"Stair\" parent=\".\"", scene);
        Assert.Contains("[node name=\"LevelManager\" type=\"Node\" parent=\".\"]", scene);
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
