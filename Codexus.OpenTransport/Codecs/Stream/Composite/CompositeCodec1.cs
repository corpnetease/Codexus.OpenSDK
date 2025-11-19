using DotNetty.Buffers;

namespace Codexus.OpenTransport.Codecs.Stream.Composite;

public class CompositeCodec1<T, T1>(
    IByteBufferCodec<T1> codec1,
    Func<T, T1> getter1,
    Func<T1, T> constructor)
    : IByteBufferCodec<T>
{
    private readonly IByteBufferCodec<T1> _codec1 = codec1 ?? throw new ArgumentNullException(nameof(codec1));
    private readonly Func<T1, T> _constructor = constructor ?? throw new ArgumentNullException(nameof(constructor));
    private readonly Func<T, T1> _getter1 = getter1 ?? throw new ArgumentNullException(nameof(getter1));

    public T Decode(IByteBuffer buffer)
    {
        var value1 = _codec1.Decode(buffer);
        return _constructor(value1);
    }

    public void Encode(IByteBuffer buffer, T value)
    {
        _codec1.Encode(buffer, _getter1(value));
    }
}