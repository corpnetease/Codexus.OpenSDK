namespace Codexus.OpenTransport.Packet.Handler;

public class DelegatePacketHandler<T>(Action<PacketHandlerContext, T> handler) : IPacketHandler<T>
    where T : IPacket
{
    public void Handle(PacketHandlerContext context, T packet)
    {
        handler(context, packet);
    }
}