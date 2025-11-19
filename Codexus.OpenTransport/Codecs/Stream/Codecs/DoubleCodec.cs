using DotNetty.Buffers;

namespace Codexus.OpenTransport.Codecs.Stream.Codecs;

public class DoubleCodec : IByteBufferCodec<double>
{
    public double Decode(IByteBuffer buffer)
    {
        return buffer.ReadDouble();
    }

    public void Encode(IByteBuffer buffer, double value)
    {
        buffer.WriteDouble(value);
    }
}