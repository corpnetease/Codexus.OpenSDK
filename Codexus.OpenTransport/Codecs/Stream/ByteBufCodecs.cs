using Codexus.OpenTransport.Codecs.Stream.Codecs;

namespace Codexus.OpenTransport.Codecs.Stream;

public static class ByteBufCodecs
{
    public static readonly IByteBufferCodec<bool> Bool = new BooleanCodec();
    public static readonly IByteBufferCodec<byte> Byte = new ByteCodec();
    public static readonly IByteBufferCodec<short> Short = new ShortCodec();
    public static readonly IByteBufferCodec<ushort> UnsignedShort = new UnsignedShortCodec();
    public static readonly IByteBufferCodec<int> Int = new IntCodec();
    public static readonly IByteBufferCodec<long> Long = new LongCodec();
    public static readonly IByteBufferCodec<float> Float = new FloatCodec();
    public static readonly IByteBufferCodec<double> Double = new DoubleCodec();
    public static readonly IByteBufferCodec<int> VarInt = new VarIntCodec();
    public static readonly IByteBufferCodec<long> VarLong = new VarLongCodec();
    public static readonly IByteBufferCodec<string> String = new StringCodec();
    public static readonly IByteBufferCodec<Guid> Uuid = new UuidCodec();
    public static readonly IByteBufferCodec<byte[]> ByteArray = new ByteArrayCodec();
    public static readonly IByteBufferCodec<byte[]> ReadRemaining = new ReadRemainingCodec();

    public static IByteBufferCodec<string> MaxString(int maxLength)
    {
        return new StringCodec(maxLength);
    }

    public static IByteBufferCodec<byte[]> MaxByteArray(int maxLength)
    {
        return new ByteArrayCodec(maxLength);
    }

    public static IByteBufferCodec<TEnum> EnumVarInt<TEnum>() where TEnum : struct, Enum
    {
        return new EnumVarIntCodec<TEnum>();
    }
}