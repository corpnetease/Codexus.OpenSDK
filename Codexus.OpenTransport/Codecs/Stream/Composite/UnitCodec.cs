using DotNetty.Buffers;

namespace Codexus.OpenTransport.Codecs.Stream.Composite;

public class UnitCodec<T>(Func<T> constructor) : IByteBufferCodec<T>
{
    private readonly Func<T> _constructor = constructor ?? throw new ArgumentNullException(nameof(constructor));

    public T Decode(IByteBuffer buffer)
    {
        return _constructor();
    }

    public void Encode(IByteBuffer buffer, T value)
    {
    }
}