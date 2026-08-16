using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
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

    [Fact]
    public void WindowsPackagingScript_IncludesRequiredDotNetAssemblies()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var projectRoot = Directory.CreateTempSubdirectory("wick-package-test-");
        try
        {
            var exportDirectory = Directory.CreateDirectory(
                Path.Combine(projectRoot.FullName, "build", "Wick-Windows"));
            var assembliesDirectory = Directory.CreateDirectory(
                Path.Combine(exportDirectory.FullName, "data_Wick_windows_x86_64"));
            File.WriteAllText(Path.Combine(exportDirectory.FullName, "Wick.exe"), "exe");
            File.WriteAllText(Path.Combine(exportDirectory.FullName, "Wick.pck"), "pck");
            File.WriteAllText(Path.Combine(exportDirectory.FullName, "README.txt"), "readme");
            File.WriteAllText(Path.Combine(assembliesDirectory.FullName, "Wick.dll"), "assembly");

            var startInfo = new ProcessStartInfo("powershell.exe")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };
            startInfo.ArgumentList.Add("-NoProfile");
            startInfo.ArgumentList.Add("-ExecutionPolicy");
            startInfo.ArgumentList.Add("Bypass");
            startInfo.ArgumentList.Add("-File");
            startInfo.ArgumentList.Add(ProjectFile("tools", "package_windows.ps1"));
            startInfo.ArgumentList.Add("-ProjectRoot");
            startInfo.ArgumentList.Add(projectRoot.FullName);
            startInfo.ArgumentList.Add("-SkipExport");

            using var process = Process.Start(startInfo);
            Assert.NotNull(process);
            var standardOutput = process.StandardOutput.ReadToEnd();
            var standardError = process.StandardError.ReadToEnd();
            process.WaitForExit();
            Assert.True(
                process.ExitCode == 0,
                $"Packaging failed ({process.ExitCode}).\n{standardOutput}\n{standardError}");

            using var archive = ZipFile.OpenRead(
                Path.Combine(projectRoot.FullName, "build", "Wick-Windows.zip"));
            var entries = archive.Entries
                .Select(entry => entry.FullName.Replace('\\', '/'))
                .ToArray();
            Assert.Contains("Wick.exe", entries);
            Assert.Contains("Wick.pck", entries);
            Assert.Contains("README.txt", entries);
            Assert.Contains("data_Wick_windows_x86_64/Wick.dll", entries);
        }
        finally
        {
            projectRoot.Delete(recursive: true);
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
