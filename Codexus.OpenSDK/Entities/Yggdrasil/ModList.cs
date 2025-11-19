using System.Text.Json.Serialization;

namespace Codexus.OpenSDK.Entities.Yggdrasil;

public class ModList
{
    [JsonPropertyName("mods")] public List<Mod> Mods { get; set; } = [];

    public ModList Clone()
    {
        return new ModList
        {
            Mods = Mods.Select(m => m.Clone()).ToList()
        };
    }
}