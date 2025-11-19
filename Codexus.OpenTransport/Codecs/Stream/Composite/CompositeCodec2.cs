using DotNetty.Buffers;

namespace Codexus.OpenTransport.Codecs.Stream.Composite;

public class CompositeCodec2<T, T1, T2>(
    IByteBufferCodec<T1> codec1,
    Func<T, T1> getter1,
    IByteBufferCodec<T2> codec2,
    Func<T, T2> getter2,
    Func<T1, T2, T> constructor)
    : IByteBufferCodec<T>
{
    private readonly IByteBufferCodec<T1> _codec1 = codec1 ?? throw new ArgumentNullException(nameof(codec1));
    private readonly IByteBufferCodec<T2> _codec2 = codec2 ?? throw new ArgumentNullException(nameof(codec2));
    private readonly Func<T1, T2, T> _constructor = constructor ?? throw new ArgumentNullException(nameof(constructor));
    private readonly Func<T, T1> _getter1 = getter1 ?? throw new ArgumentNullException(nameof(getter1));
    private readonly Func<T, T2> _getter2 = getter2 ?? throw new ArgumentNullException(nameof(getter2));

    public T Decode(IByteBuffer buffer)
    {
        var value1 = _codec1.Decode(buffer);
        var value2 = _codec2.Decode(buffer);
        return _constructor(value1, value2);
    }

    public void Encode(IByteBuffer buffer, T value)
    {
        _codec1.Encode(buffer, _getter1(value));
        _codec2.Encode(buffer, _getter2(value));
    }
}