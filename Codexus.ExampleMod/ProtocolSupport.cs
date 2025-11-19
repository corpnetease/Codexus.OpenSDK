using System.Text;
using Codexus.ExampleMod.Extensions;
using Codexus.ExampleMod.Packet;
using Codexus.ModSDK;
using Codexus.OpenTransport.Codecs.Stream;
using Codexus.OpenTransport.Event;
using Codexus.OpenTransport.Extensions;
using Codexus.OpenTransport.Packet;
using Codexus.OpenTransport.Packet.Handler;
using Codexus.OpenTransport.Registry;
using Codexus.OpenTransport.Session;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Encodings;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Security;
using Serilog;

namespace Codexus.ExampleMod;

/*
 * Provides server join support for Minecraft version 1.21.0
 */
public class ProtocolSupport(IModContext? modContext) : IRegistryApply
{
    private static readonly IByteBufferCodec<C2SLoginStart> LoginStart =
        StreamCodec.Composite(
            ByteBufCodecs.MaxString(16), p => p.Profile,
            ByteBufCodecs.Uuid, p => p.Uuid,
            (profile, uuid) => new C2SLoginStart(profile, uuid)
        );

    private static readonly IByteBufferCodec<C2SEncryptionResponse> EncryptionResponse =
        StreamCodec.Composite(
            ByteBufCodecs.ByteArray, p => p.SecretKeyEncrypted,
            ByteBufCodecs.ByteArray, p => p.VerifyTokenEncrypted,
            (key, token) => new C2SEncryptionResponse(key, token)
        );

    private static readonly IByteBufferCodec<C2SLoginAcknowledged> LoginAcknowledged =
        StreamCodec.Unit(new C2SLoginAcknowledged());

    private static readonly IByteBufferCodec<C2SAcknowledgeFinishConfiguration> AcknowledgeFinishConfiguration =
        StreamCodec.Unit(new C2SAcknowledgeFinishConfiguration());

    private static readonly IByteBufferCodec<S2CEnableCompression> EnableCompression =
        StreamCodec.Composite(
            ByteBufCodecs.VarInt, p => p.CompressionThreshold,
            threshold => new S2CEnableCompression(threshold)
        );

    private static readonly IByteBufferCodec<S2CEncryptionRequest> EncryptionRequest =
        StreamCodec.Composite(
            ByteBufCodecs.MaxString(20), p => p.ServerId,
            ByteBufCodecs.ByteArray, p => p.PublicKey,
            ByteBufCodecs.ByteArray, p => p.VerifyToken,
            ByteBufCodecs.Bool, p => p.ShouldAuthenticate,
            (id, key, token, authenticate) => new S2CEncryptionRequest(id, key, token, authenticate)
        );

    private static readonly IByteBufferCodec<List<Property>> PropertiesCodec =
        StreamCodec.Composite(
            ByteBufCodecs.String, p => p.Name,
            ByteBufCodecs.String, p => p.Value,
            ByteBufCodecs.String.OptionalRef(), p => p.Value,
            (name, value, signature) => new Property(name, value, signature)
        ).List();

    private static readonly IByteBufferCodec<S2CLoginSuccess> LoginSuccess =
        StreamCodec.Composite(
            ByteBufCodecs.Uuid, p => p.Uuid,
            ByteBufCodecs.MaxString(20), p => p.Username,
            PropertiesCodec, p => p.Properties,
            ByteBufCodecs.Bool, p => p.StrictErrorHandling,
            (uuid, username, properties, strict) => new S2CLoginSuccess(uuid, username, properties, strict)
        );

    private static readonly IByteBufferCodec<S2CStartConfiguration> StartConfiguration =
        StreamCodec.Unit(new S2CStartConfiguration());

    public void ApplyTo(MinecraftRegistry registry, RegistryScope scope)
    {
        registry
            .Builder(scope)
            .ForVersion(EnumProtocolVersion.V1210)

            // Login
            .InState(EnumConnectionState.Login)
            .ServerBound()

            // LoginStart
            .Register(0x00, LoginStart)
            .Attach<C2SLoginStart>((context, packet) =>
            {
                var session = context.Session;

                packet.Profile = session.Request.RoleName;
                Log.Information("{Profile} trying to login...", packet.Profile);
            })

            // EncryptionResponse
            .Register(0x01, EncryptionResponse)

            // LoginAcknowledged
            .Register(0x03, LoginAcknowledged)
            .Attach<C2SLoginAcknowledged>((context, _) =>
            {
                context.Session.SetState(EnumConnectionState.Configuration);
                modContext?.LogInformation("Initial configuration process started.");
            })
            .ClientBound()

            // EnableCompression
            .Register(0x03, EnableCompression)
            .Attach<S2CEnableCompression>((context, packet) =>
            {
                NetworkSession.EnableCompression(context.Session.Remote!, packet.CompressionThreshold);

                context.OnSendAfter(() =>
                {
                    NetworkSession.EnableCompression(context.Session.Local, packet.CompressionThreshold);
                });
            })

            // EncryptionRequest
            .Register(0x01, EncryptionRequest)
            .Attach<S2CEncryptionRequest>(HandleEncryptionRequest)

            // LoginSuccess
            .Register(0x02, LoginSuccess)
            .Attach<S2CLoginSuccess>((_, packet) =>
            {
                modContext?.LogInformation("{0}({1}) has joined the server.", packet.Username, packet.Uuid);
            })

            // Play
            .InState(EnumConnectionState.Play)
            .ClientBound()

            // StartConfiguration
            .Register(0x69, StartConfiguration)
            .Attach<S2CStartConfiguration>((context, _) =>
            {
                context.Session.SetState(EnumConnectionState.Configuration);
                modContext?.LogInformation("Restarting configuration.");
            })

            // Configuration
            .InState(EnumConnectionState.Configuration)
            .ServerBound()

            // AcknowledgeFinishConfiguration
            .Register(0x03, AcknowledgeFinishConfiguration)
            .Attach<C2SAcknowledgeFinishConfiguration>((context, _) =>
            {
                context.Session.SetState(EnumConnectionState.Play);
                modContext?.LogInformation("Configuration completed successfully.");
            });
    }

    private void HandleEncryptionRequest(PacketHandlerContext context, S2CEncryptionRequest packet)
    {
        var session = context.Session;

        #region Initalize

        var generator = new CipherKeyGenerator();
        generator.Init(new KeyGenerationParameters(new SecureRandom(), 128));
        var rsaKey = SubjectPublicKeyInfo.GetInstance(packet.PublicKey);
        var secretKey = generator.GenerateKey();

        #endregion

        #region Generate ServerId

        using var stream = new MemoryStream(20);
        stream.Write(Encoding.GetEncoding("ISO-8859-1").GetBytes(packet.ServerId));
        stream.Write(secretKey);
        stream.Write(packet.PublicKey);
        stream.Position = 0;
        var serverId = stream.ToServerId();

        #endregion

        #region Publish Event

        modContext?.EventBus.Publish(new EventJoinServer(context, serverId));

        #endregion

        #region Build EncryptionResponse

        var encoding = new Pkcs1Encoding(new RsaEngine());
        encoding.Init(true, PublicKeyFactory.CreateKey(rsaKey));

        context.OnSendAfter(async () =>
        {
            var response = new C2SEncryptionResponse
            (
                encoding.ProcessBlock(secretKey, 0, secretKey.Length),
                encoding.ProcessBlock(packet.VerifyToken, 0, packet.VerifyToken.Length)
            );

            await context.SendToRemote(response);
            if (session.Remote != null) NetworkSession.EnableEncryption(session.Remote, secretKey);
        });

        #endregion

        context.Cancel();
    }
}