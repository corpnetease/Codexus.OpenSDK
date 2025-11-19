using DotNetty.Buffers;

namespace Codexus.OpenTransport.Codecs.Stream.Codecs;

public class EnumVarIntCodec<TEnum> : IByteBufferCodec<TEnum> where TEnum : struct, Enum
{
    private readonly VarIntCodec _varIntCodec = new();

    public TEnum Decode(IByteBuffer buffer)
    {
        var intValue = _varIntCodec.Decode(buffer);

        if (!Enum.IsDefined(typeof(TEnum), intValue))
            throw new InvalidOperationException(
                $"Value {intValue} is not a valid {typeof(TEnum).Name} enum value");

        return (TEnum)Enum.ToObject(typeof(TEnum), intValue);
    }

    public void Encode(IByteBuffer buffer, TEnum value)
    {
        var intValue = Convert.ToInt32(value);
        _varIntCodec.Encode(buffer, intValue);
    }
}