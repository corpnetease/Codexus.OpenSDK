using DotNetty.Buffers;

namespace Codexus.OpenTransport.Codecs.Stream.Codecs;

public class IntCodec : IByteBufferCodec<int>
{
    public int Decode(IByteBuffer buffer)
    {
        return buffer.ReadInt();
    }

    public void Encode(IByteBuffer buffer, int value)
    {
        buffer.WriteInt(value);
    }
}