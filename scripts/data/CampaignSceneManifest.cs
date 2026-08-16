using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Wick.Data;

public sealed record CampaignSceneManifestEntry(int Index, string ResourcePath, string RootNodeName);

public static class CampaignSceneManifest
{
    private static readonly CampaignSceneManifestEntry[] Entries = Enumerable.Range(1, 7)
        .Select(index => new CampaignSceneManifestEntry(
            index,
            $"res://scenes/levels/Level{index:00}.tscn",
            $"Level{index:00}"))
        .ToArray();

    public static IReadOnlyList<CampaignSceneManifestEntry> All => Entries;

    public static CampaignSceneManifestEntry Get(int index) =>
        Entries.FirstOrDefault(entry => entry.Index == index)
        ?? throw new ArgumentOutOfRangeException(nameof(index), index, "No campaign scene is declared at this index.");
}

public sealed record GodotExternalResource(string Type, string Path, string Id);

public sealed class GodotSceneNodeContract
{
    public GodotSceneNodeContract(string name, string type, string parent, string instanceResourceId)
    {
        Name = name;
        Type = type;
        Parent = parent;
        InstanceResourceId = instanceResourceId;
    }

    public string Name { get; }
    public string Type { get; }
    public string Parent { get; }
    public string InstanceResourceId { get; }
    public Dictionary<string, string> Properties { get; } = new(StringComparer.Ordinal);
}

public sealed class GodotTextSceneContract
{
    private static readonly Regex AttributePattern = new("(?<key>[a-zA-Z_]+)=\"(?<value>[^\"]*)\"", RegexOptions.Compiled);
    private static readonly Regex InstancePattern = new("instance=ExtResource\\(\"(?<id>[^\"]+)\"\\)", RegexOptions.Compiled);

    private GodotTextSceneContract(
        IReadOnlyDictionary<string, GodotExternalResource> resources,
        IReadOnlyList<GodotSceneNodeContract> nodes)
    {
        Resources = resources;
        Nodes = nodes;
    }

    public IReadOnlyDictionary<string, GodotExternalResource> Resources { get; }
    public IReadOnlyList<GodotSceneNodeContract> Nodes { get; }

    public static GodotTextSceneContract ParseFile(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Godot scene contract file is missing.", path);
        }

        return Parse(File.ReadAllText(path));
    }

    public static GodotTextSceneContract Parse(string text)
    {
        var resources = new Dictionary<string, GodotExternalResource>(StringComparer.Ordinal);
        var nodes = new List<GodotSceneNodeContract>();
        GodotSceneNodeContract currentNode = null;
        foreach (var rawLine in text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None))
        {
            var line = rawLine.Trim();
            if (line.StartsWith("[ext_resource ", StringComparison.Ordinal))
            {
                var attributes = ParseAttributes(line);
                Require(attributes, "type", line);
                Require(attributes, "path", line);
                Require(attributes, "id", line);
                resources.Add(attributes["id"], new GodotExternalResource(attributes["type"], attributes["path"], attributes["id"]));
                currentNode = null;
                continue;
            }

            if (line.StartsWith("[node ", StringComparison.Ordinal))
            {
                var attributes = ParseAttributes(line);
                Require(attributes, "name", line);
                var instance = InstancePattern.Match(line);
                currentNode = new GodotSceneNodeContract(
                    attributes["name"],
                    attributes.GetValueOrDefault("type", string.Empty),
                    attributes.GetValueOrDefault("parent", string.Empty),
                    instance.Success ? instance.Groups["id"].Value : string.Empty);
                nodes.Add(currentNode);
                continue;
            }

            if (line.StartsWith("[", StringComparison.Ordinal))
            {
                currentNode = null;
                continue;
            }

            if (currentNode is not null)
            {
                var separator = line.IndexOf(" = ", StringComparison.Ordinal);
                if (separator > 0)
                {
                    currentNode.Properties[line[..separator]] = line[(separator + 3)..];
                }
            }
        }

        if (nodes.Count == 0)
        {
            throw new InvalidDataException("Godot scene contains no node declarations.");
        }

        return new GodotTextSceneContract(resources, nodes);
    }

    public GodotSceneNodeContract RequireNode(string name)
    {
        var matches = Nodes.Where(node => node.Name == name).ToArray();
        return matches.Length == 1
            ? matches[0]
            : throw new InvalidDataException($"Expected exactly one node named {name}, found {matches.Length}.");
    }

    public string ResolveNodeResourcePath(GodotSceneNodeContract node)
    {
        var resourceId = node.InstanceResourceId;
        if (string.IsNullOrEmpty(resourceId) && node.Properties.TryGetValue("script", out var script))
        {
            var match = InstancePattern.Match($"instance={script}");
            resourceId = match.Success ? match.Groups["id"].Value : string.Empty;
        }

        return !string.IsNullOrEmpty(resourceId) && Resources.TryGetValue(resourceId, out var resource)
            ? resource.Path
            : throw new InvalidDataException($"Node {node.Name} does not resolve to a declared external resource.");
    }

    private static Dictionary<string, string> ParseAttributes(string line) => AttributePattern.Matches(line)
        .Cast<Match>()
        .ToDictionary(match => match.Groups["key"].Value, match => match.Groups["value"].Value, StringComparer.Ordinal);

    private static void Require(IReadOnlyDictionary<string, string> attributes, string key, string line)
    {
        if (!attributes.ContainsKey(key))
        {
            throw new InvalidDataException($"Godot scene declaration is missing {key}: {line}");
        }
    }
}

public static class CampaignRuntimeContract
{
    public static void ValidateLevelScene(GodotTextSceneContract scene, CampaignSceneManifestEntry entry)
    {
        var root = scene.RequireNode(entry.RootNodeName);
        Require(root.Type == "Node", $"Campaign level {entry.Index} root must be a Node.");
        Require(string.IsNullOrEmpty(root.Parent), $"Campaign level {entry.Index} root cannot have a parent.");
        Require(scene.ResolveNodeResourcePath(root) == "res://scripts/data/CampaignLevelScene.cs",
            $"Campaign level {entry.Index} does not use CampaignLevelScene.");
        Require(root.Properties.TryGetValue("index", out var value) && int.TryParse(value, out var index) && index == entry.Index,
            $"Campaign level scene index does not match manifest index {entry.Index}.");
    }

    public static void ValidateGameRoot(GodotTextSceneContract scene)
    {
        var expected = new Dictionary<string, string>
        {
            ["Player"] = "res://scenes/actors/Player.tscn",
            ["Lamp"] = "res://scenes/actors/Lamp.tscn",
            ["Stair"] = "res://scenes/goals/Stair.tscn",
            ["Crawler"] = "res://scenes/hazards/Crawler.tscn",
            ["Moth"] = "res://scenes/hazards/Moth.tscn",
            ["OilFlask"] = "res://scenes/pickups/OilFlask.tscn",
            ["Hud"] = "res://scenes/ui/Hud.tscn",
            ["HazardSystem"] = "res://scripts/hazards/HazardSystem.cs",
            ["LevelManager"] = "res://scripts/core/LevelManager.cs"
        };
        foreach (var pair in expected)
        {
            Require(scene.ResolveNodeResourcePath(scene.RequireNode(pair.Key)) == pair.Value,
                $"GameRoot node {pair.Key} does not resolve to {pair.Value}.");
        }
    }

    public static void ValidateHud(GodotTextSceneContract scene)
    {
        Require(scene.RequireNode("CampaignStatus").Type == "Label", "HUD campaign status must be a Label.");
        Require(scene.RequireNode("PracticeStatus").Type == "Label", "HUD practice status must be a Label.");
        var controls = scene.RequireNode("Controls");
        Require(controls.Type == "Label", "HUD controls must be a Label.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidDataException(message);
        }
    }
}
