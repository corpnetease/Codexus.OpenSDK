using DotNetty.Buffers;

namespace Codexus.OpenTransport.Codecs.Stream.Composite;

public class CompositeCodec4<T, T1, T2, T3, T4>(
    IByteBufferCodec<T1> codec1,
    Func<T, T1> getter1,
    IByteBufferCodec<T2> codec2,
    Func<T, T2> getter2,
    IByteBufferCodec<T3> codec3,
    Func<T, T3> getter3,
    IByteBufferCodec<T4> codec4,
    Func<T, T4> getter4,
    Func<T1, T2, T3, T4, T> constructor)
    : IByteBufferCodec<T>
{
    private readonly IByteBufferCodec<T1> _codec1 = codec1 ?? throw new ArgumentNullException(nameof(codec1));
    private readonly IByteBufferCodec<T2> _codec2 = codec2 ?? throw new ArgumentNullException(nameof(codec2));
    private readonly IByteBufferCodec<T3> _codec3 = codec3 ?? throw new ArgumentNullException(nameof(codec3));
    private readonly IByteBufferCodec<T4> _codec4 = codec4 ?? throw new ArgumentNullException(nameof(codec4));

    private readonly Func<T1, T2, T3, T4, T> _constructor =
        constructor ?? throw new ArgumentNullException(nameof(constructor));

    private readonly Func<T, T1> _getter1 = getter1 ?? throw new ArgumentNullException(nameof(getter1));
    private readonly Func<T, T2> _getter2 = getter2 ?? throw new ArgumentNullException(nameof(getter2));
    private readonly Func<T, T3> _getter3 = getter3 ?? throw new ArgumentNullException(nameof(getter3));
    private readonly Func<T, T4> _getter4 = getter4 ?? throw new ArgumentNullException(nameof(getter4));

    public T Decode(IByteBuffer buffer)
    {
        var value1 = _codec1.Decode(buffer);
        var value2 = _codec2.Decode(buffer);
        var value3 = _codec3.Decode(buffer);
        var value4 = _codec4.Decode(buffer);
        return _constructor(value1, value2, value3, value4);
    }

    public void Encode(IByteBuffer buffer, T value)
    {
        _codec1.Encode(buffer, _getter1(value));
        _codec2.Encode(buffer, _getter2(value));
        _codec3.Encode(buffer, _getter3(value));
        _codec4.Encode(buffer, _getter4(value));
    }
}