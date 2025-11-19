using DotNetty.Buffers;

namespace Codexus.OpenTransport.Codecs.Stream.Codecs;

public class OptionalReferenceCodec<T>(IByteBufferCodec<T> elementCodec) : IByteBufferCodec<T?>
    where T : class
{
    private readonly IByteBufferCodec<T> _elementCodec =
        elementCodec ?? throw new ArgumentNullException(nameof(elementCodec));

    public T? Decode(IByteBuffer buffer)
    {
        var present = buffer.ReadBoolean();
        return present ? _elementCodec.Decode(buffer) : null;
    }

    public void Encode(IByteBuffer buffer, T? value)
    {
        buffer.WriteBoolean(value != null);
        if (value != null) _elementCodec.Encode(buffer, value);
    }
}