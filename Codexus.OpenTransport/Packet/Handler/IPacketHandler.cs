namespace Codexus.OpenTransport.Packet.Handler;

public interface IPacketHandler<in TPacket> where TPacket : IPacket
{
    void Handle(PacketHandlerContext context, TPacket packet);
}