using Codexus.OpenTransport.Packet.Handler;

namespace Codexus.OpenTransport.Event;

public record EventJoinServer(PacketHandlerContext Context, string ServerId);