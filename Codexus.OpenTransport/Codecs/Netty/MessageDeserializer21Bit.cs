using Codexus.OpenTransport.Extensions;
using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Transport.Channels;

namespace Codexus.OpenTransport.Codecs.Netty;

public class MessageDeserializer21Bit : ByteToMessageDecoder
{
    protected override void Decode(IChannelHandlerContext context, IByteBuffer message, List<object> output)
    {
        message.MarkReaderIndex();

        var lengthArray = new byte[3];
        for (var i = 0; i < 3; i++)
        {
            if (!message.IsReadable())
            {
                message.ResetReaderIndex();
                return;
            }

            lengthArray[i] = message.ReadByte();
            if (lengthArray[i] >= 128) continue;

            var length = lengthArray.ReadVarInt();
            if (message.ReadableBytes >= length)
            {
                output.Add(message.ReadBytes(length));
                return;
            }

            message.ResetReaderIndex();

            return;
        }
    }
}