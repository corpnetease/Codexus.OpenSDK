using System.Text.Json.Serialization;

namespace Codexus.ModSDK;

public class ModManifest
{
    [JsonPropertyName("name")] public required string Name { get; set; }

    [JsonPropertyName("id")] public required string Id { get; set; }

    [JsonPropertyName("version")] public required string Version { get; set; }

    [JsonPropertyName("dependencies")] public Dictionary<string, string> Dependencies { get; set; } = new();

    [JsonPropertyName("entryDll")] public required string EntryDll { get; set; }
}