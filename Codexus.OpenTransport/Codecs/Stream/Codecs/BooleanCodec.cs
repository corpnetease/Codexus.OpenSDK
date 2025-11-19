using DotNetty.Buffers;

namespace Codexus.OpenTransport.Codecs.Stream.Codecs;

public class BooleanCodec : IByteBufferCodec<bool>
{
    public bool Decode(IByteBuffer buffer)
    {
        return buffer.ReadBoolean();
    }

    public void Encode(IByteBuffer buffer, bool value)
    {
        buffer.WriteBoolean(value);
    }
}