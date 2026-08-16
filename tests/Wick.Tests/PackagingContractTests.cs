using System;
using System.IO;
using System.Linq;
using Xunit;

namespace Wick.Tests;

public sealed class PackagingContractTests
{
    private static readonly string[] RequiredCaptures =
    {
        "01-start-screen.png",
        "02-starting-room.png",
        "03-first-branch.png",
        "04-crawler-light-edge.png",
        "05-flask-lift-darkness.png",
        "06-lamp-tether.png",
        "07-stair-clear.png",
        "08-loss-depleted.png",
        "09-loss-smothered.png"
    };

    [Fact]
    public void WindowsExportPreset_ProducesTheNamedCourseworkBuild()
    {
        var preset = File.ReadAllText(ProjectFile("export_presets.cfg"));

        Assert.Contains("name=\"Windows Desktop\"", preset, StringComparison.Ordinal);
        Assert.Contains("export_path=\"build/Wick-Windows/Wick.exe\"", preset, StringComparison.Ordinal);
        Assert.Contains("application/product_name=\"Wick\"", preset, StringComparison.Ordinal);
    }

    [Fact]
    public void SubmissionCaptureSet_CoversEveryAuthoredPresentationBeat()
    {
        foreach (var capture in RequiredCaptures)
        {
            var path = ProjectFile("docs", "captures", capture);
            Assert.True(File.Exists(path), $"Submission capture is missing: {capture}");
            Assert.True(new FileInfo(path).Length > 10_000, $"Submission capture is unexpectedly small: {capture}");
        }
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
