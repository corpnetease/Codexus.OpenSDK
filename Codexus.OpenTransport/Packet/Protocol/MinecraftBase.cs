using Codexus.OpenTransport.Codecs.Stream;
using Codexus.OpenTransport.Packet.Handshake;
using Codexus.OpenTransport.Registry;

namespace Codexus.OpenTransport.Packet.Protocol;

public class MinecraftBase : IRegistryApply
{
    private static readonly IByteBufferCodec<C2SHandshake> Handshake =
        StreamCodec.Composite(
            ByteBufCodecs.EnumVarInt<EnumProtocolVersion>(), p => p.ProtocolVersion,
            ByteBufCodecs.String, p => p.ServerAddress,
            ByteBufCodecs.UnsignedShort, p => p.ServerPort,
            ByteBufCodecs.EnumVarInt<EnumConnectionState>(), p => p.NextState,
            (ver, addr, port, state) => new C2SHandshake(ver, addr, port, state)
        );

    public void ApplyTo(MinecraftRegistry registry, RegistryScope scope)
    {
        registry.Builder(scope)
            .ForAllVersion()
            .InState(EnumConnectionState.Handshake)
            .ServerBound()
            .Register(0x00, Handshake)
            .Attach<C2SHandshake>((context, packet) =>
            {
                var session = context.Session;

                session.SetProtocolVersion(packet.ProtocolVersion);
                session.SetState(packet.NextState);

                packet.ServerPort = (ushort)session.Request.ServerPort;
                packet.ServerAddress = session.ProtocolVersion switch
                {
                    > EnumProtocolVersion.V1206 => session.Request.ServerAddress + "\0FORGE",
                    > EnumProtocolVersion.V1180 => session.Request.ServerAddress + "\0FML3\0",
                    > EnumProtocolVersion.V1122 => session.Request.ServerAddress + "\0FML2\0",
                    _ => session.Request.ServerAddress + "\0FML\0"
                };
            });
    }
}