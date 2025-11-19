using Codexus.OpenTransport.Extensions;
using Codexus.OpenTransport.Session;
using DotNetty.Buffers;
using DotNetty.Transport.Channels;

namespace Codexus.OpenTransport.NettyHandler;

public class ClientInboundMessageHandler(OpenTransport transport) : ChannelHandlerAdapter
{
    public override void ChannelActive(IChannelHandlerContext context)
    {
        var session = new NetworkSession(transport, context.Channel);
        transport.AddConnection(context.Channel, session);
    }

    public override void ChannelInactive(IChannelHandlerContext context)
    {
        transport.RemoveConnection(context.Channel);
    }

    public override void ChannelRead(IChannelHandlerContext context, object message)
    {
        var session = context.GetSession();
        session?.OnClientReceived((IByteBuffer)message);
    }

    public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
    {
        context.CloseAsync();
    }
}