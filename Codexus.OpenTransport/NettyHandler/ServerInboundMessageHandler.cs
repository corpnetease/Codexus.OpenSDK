using Codexus.OpenTransport.Extensions;
using Codexus.OpenTransport.Session;
using DotNetty.Buffers;
using DotNetty.Transport.Channels;

namespace Codexus.OpenTransport.NettyHandler;

public class ServerInboundMessageHandler(NetworkSession session) : ChannelHandlerAdapter
{
    public override void ChannelActive(IChannelHandlerContext context)
    {
        context.SetSession(session);
    }

    public override void ChannelRead(IChannelHandlerContext context, object message)
    {
        session.OnServerReceived((IByteBuffer)message);
    }

    public override void ChannelInactive(IChannelHandlerContext context)
    {
        context.RemoveSession();
    }
}