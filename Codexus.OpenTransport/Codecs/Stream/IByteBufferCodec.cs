using DotNetty.Buffers;

namespace Codexus.OpenTransport.Codecs.Stream;

public interface IByteBufferCodec<T> : IByteBufferCodecBase
{
    object IByteBufferCodecBase.Decode(IByteBuffer buffer)
    {
        return Decode(buffer)!;
    }

    void IByteBufferCodecBase.Encode(IByteBuffer buffer, object value)
    {
        Encode(buffer, (T)value);
    }

    new T Decode(IByteBuffer buffer);
    void Encode(IByteBuffer buffer, T value);
}