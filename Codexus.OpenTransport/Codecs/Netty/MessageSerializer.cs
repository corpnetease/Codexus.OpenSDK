using Codexus.OpenTransport.Extensions;
using Codexus.OpenTransport.Packet;
using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Transport.Channels;

namespace Codexus.OpenTransport.Codecs.Netty;

public class MessageSerializer : MessageToByteEncoder<PacketWrapper>
{
    protected override void Encode(IChannelHandlerContext context, PacketWrapper message, IByteBuffer output)
    {
        var session = context.GetSession();
        if (session == null) throw new InvalidOperationException("Session is null");

        var type = message.Packet.GetType();
        var codec = session.Transport
            .Registry.GetCodecByType(message.ProtocolVersion, message.State, message.Direction, type);

        if (codec == null)
            throw new InvalidOperationException(
                $"Codec not found for packet type {type.Name}");

        output.WriteVarInt(message.Id);
        codec.Encode(output, message.Packet);
    }
}