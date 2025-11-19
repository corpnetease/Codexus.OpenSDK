using DotNetty.Buffers;

namespace Codexus.OpenTransport.Codecs.Stream.Codecs;

public class UnsignedShortCodec : IByteBufferCodec<ushort>
{
    public ushort Decode(IByteBuffer buffer)
    {
        return buffer.ReadUnsignedShort();
    }

    public void Encode(IByteBuffer buffer, ushort value)
    {
        buffer.WriteUnsignedShort(value);
    }
}