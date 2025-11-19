using System.Text.Json.Serialization;

namespace Codexus.OpenSDK.Entities.Yggdrasil;

public class Mod
{
    [JsonPropertyName("modPath")] public required string ModPath { get; set; }
    [JsonPropertyName("name")] public required string Name { get; set; } = "";
    [JsonPropertyName("id")] public required string Id { get; set; }
    [JsonPropertyName("iid")] public required string Iid { get; set; }
    [JsonPropertyName("md5")] public required string Md5 { get; set; }
    [JsonPropertyName("version")] public required string Version { get; set; } = "";

    public Mod Clone()
    {
        return new Mod
        {
            ModPath = ModPath,
            Name = Name,
            Id = Id,
            Iid = Iid,
            Md5 = Md5,
            Version = Version
        };
    }
}