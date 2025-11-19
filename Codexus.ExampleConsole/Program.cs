using Codexus.ModHost;
using Codexus.ModHost.Event;
using Codexus.OpenSDK;
using Codexus.OpenSDK.Entities.Yggdrasil;
using Codexus.OpenSDK.Yggdrasil;
using Codexus.OpenTransport;
using Codexus.OpenTransport.Entities.Transport;
using Codexus.OpenTransport.Event;
using Serilog;

/*
 * Sample Project: Example for testing OpenTransport and the Mod system
 */
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .CreateLogger();

var modsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Mods");
var manager = new ModManager(Log.Logger, modsDir);
manager.Initialize();

var c4399 = new C4399();
var x19 = new X19();

var cookie = await c4399.LoginWithPasswordAsync("YOUR_USER", "YOUR_PASSWORD");
var (user, _) = await x19.ContinueAsync(cookie);

var profile = new GameProfile
{
    GameId = "4663909014288106690",
    GameVersion = "1.21",
    BootstrapMd5 = "684528BF492A84489F825F5599B3E1C6",
    DatFileMd5 = "574033E7E4841D8AC4C14D7FA5E05337",
    Mods = new ModList(),
    User = new UserProfile
    {
        UserId = int.Parse(user.EntityId),
        UserToken = user.Token
    }
};

var request = new CreateRequest
{
    ServerAddress = "45.253.142.30",
    ServerPort = 25565,
    RoleName = "YOUR_ROLE",
    Debug = false
};

var yggdrasil = new StandardYggdrasil(new YggdrasilData
{
    LauncherVersion = x19.GameVersion,
    Channel = "netease",
    CrcSalt = "22AC4B0143EFFC80F2905B267D4D84D3"
});

EventBus.Instance.Subscribe<EventJoinServer>(async e =>
{
    await Task.Run(async () =>
    {
        var result = await yggdrasil.JoinServerAsync(e.Context.Session.Profile, e.ServerId);

        if (result.IsSuccess)
            Log.Information("Joined server successfully");
        else
            Log.Error("Joined server failed: {Error}", result.Error);
    }).ConfigureAwait(false);
});

var transport = OpenTransport.Create(profile, request, Log.Logger);
await transport.StartAsync();

Console.WriteLine("Press any key to exit...");
Console.ReadKey();