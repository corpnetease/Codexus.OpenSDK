using System.Text.Json;

namespace Codexus.OpenSDK.Entities.Yggdrasil;

public class GameProfile
{
    public required string GameId { get; set; }
    public required string GameVersion { get; set; }
    public required string BootstrapMd5 { get; set; }
    public required string DatFileMd5 { get; set; }
    public required ModList Mods { get; set; }
    public required UserProfile User { get; set; }

    public string GetModInfo()
    {
        return JsonSerializer.Serialize(Mods);
    }

    public GameProfile Clone()
    {
        return new GameProfile
        {
            GameId = GameId,
            GameVersion = GameVersion,
            BootstrapMd5 = BootstrapMd5,
            DatFileMd5 = DatFileMd5,
            Mods = Mods.Clone(),
            User = User.Clone() 
        };
    }
}
