using Codexus.OpenTransport.Extensions;
using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Transport.Channels;

namespace Codexus.OpenTransport.Codecs.Netty;

/*
 * See https://minecraft.wiki/w/Java_Edition_protocol?oldid=2773082#MessageSerializer21Bit
 */
public class MessageSerializer21Bit : MessageToByteEncoder<IByteBuffer>
{
    protected override void Encode(IChannelHandlerContext context, IByteBuffer message, IByteBuffer output)
    {
        var readableBytes = message.ReadableBytes;
        var varIntSize = readableBytes.GetVarIntSize();
        if (varIntSize > 3) return;

        output.EnsureWritable(varIntSize + readableBytes);
        output.WriteVarInt(readableBytes);
        output.WriteBytes(message, message.ReaderIndex, readableBytes);
    }
}