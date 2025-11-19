using DotNetty.Buffers;

namespace Codexus.OpenTransport.Codecs.Stream.Codecs;

public class OptionalCodec<T>(IByteBufferCodec<T> elementCodec) : IByteBufferCodec<T?>
    where T : struct
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
        buffer.WriteBoolean(value.HasValue);
        if (value.HasValue) _elementCodec.Encode(buffer, value.Value);
    }
}