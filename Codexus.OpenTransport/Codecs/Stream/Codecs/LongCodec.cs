using DotNetty.Buffers;

namespace Codexus.OpenTransport.Codecs.Stream.Codecs;

public class LongCodec : IByteBufferCodec<long>
{
    public long Decode(IByteBuffer buffer)
    {
        return buffer.ReadLong();
    }

    public void Encode(IByteBuffer buffer, long value)
    {
        buffer.WriteLong(value);
    }
}