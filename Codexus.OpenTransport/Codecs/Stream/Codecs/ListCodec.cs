using DotNetty.Buffers;

namespace Codexus.OpenTransport.Codecs.Stream.Codecs;

public class ListCodec<T>(IByteBufferCodec<T> elementCodec, int maxSize = int.MaxValue)
    : IByteBufferCodec<List<T>>
{
    // ReSharper disable once StaticMemberInGenericType
    private static readonly VarIntCodec CountCodec = new();

    private readonly IByteBufferCodec<T> _elementCodec =
        elementCodec ?? throw new ArgumentNullException(nameof(elementCodec));

    public List<T> Decode(IByteBuffer buffer)
    {
        var count = CountCodec.Decode(buffer);

        if (count > maxSize) throw new InvalidOperationException($"List is too large: {count} > {maxSize}");

        if (count < 0) throw new InvalidOperationException("List size cannot be negative");

        var list = new List<T>(count);
        for (var i = 0; i < count; i++) list.Add(_elementCodec.Decode(buffer));

        return list;
    }

    public void Encode(IByteBuffer buffer, List<T> value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (value.Count > maxSize) throw new InvalidOperationException($"List is too large: {value.Count} > {maxSize}");

        CountCodec.Encode(buffer, value.Count);
        foreach (var item in value) _elementCodec.Encode(buffer, item);
    }
}