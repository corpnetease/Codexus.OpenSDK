using Codexus.OpenTransport.Packet;
using Codexus.OpenTransport.Packet.Handler;

namespace Codexus.OpenTransport.Registry;

public class PacketHandlerEntry
{
    public required int Priority { get; init; }
    public required Action<PacketHandlerContext, IPacket> Handler { get; init; }
}