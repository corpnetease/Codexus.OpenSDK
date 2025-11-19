using DotNetty.Buffers;

namespace Codexus.OpenTransport.Codecs.Stream.Codecs;

public class ByteCodec : IByteBufferCodec<byte>
{
    public byte Decode(IByteBuffer buffer)
    {
        return buffer.ReadByte();
    }

    public void Encode(IByteBuffer buffer, byte value)
    {
        buffer.WriteByte(value);
    }
}