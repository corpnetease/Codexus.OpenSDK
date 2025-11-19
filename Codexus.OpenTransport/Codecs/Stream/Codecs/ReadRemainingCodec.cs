using DotNetty.Buffers;

namespace Codexus.OpenTransport.Codecs.Stream.Codecs;

public class ReadRemainingCodec : IByteBufferCodec<byte[]>
{
    public byte[] Decode(IByteBuffer buffer)
    {
        var remainingBytes = new byte[buffer.ReadableBytes];
        buffer.ReadBytes(remainingBytes);
        return remainingBytes;
    }

    public void Encode(IByteBuffer buffer, byte[] value)
    {
        buffer.WriteBytes(value);
    }
}