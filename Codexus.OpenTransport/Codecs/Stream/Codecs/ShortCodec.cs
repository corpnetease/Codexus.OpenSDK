using DotNetty.Buffers;

namespace Codexus.OpenTransport.Codecs.Stream.Codecs;

public class ShortCodec : IByteBufferCodec<short>
{
    public short Decode(IByteBuffer buffer)
    {
        return buffer.ReadShort();
    }

    public void Encode(IByteBuffer buffer, short value)
    {
        buffer.WriteShort(value);
    }
}