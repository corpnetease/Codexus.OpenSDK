using DotNetty.Buffers;

namespace Codexus.OpenTransport.Codecs.Stream.Codecs;

public class FloatCodec : IByteBufferCodec<float>
{
    public float Decode(IByteBuffer buffer)
    {
        return buffer.ReadFloat();
    }

    public void Encode(IByteBuffer buffer, float value)
    {
        buffer.WriteFloat(value);
    }
}