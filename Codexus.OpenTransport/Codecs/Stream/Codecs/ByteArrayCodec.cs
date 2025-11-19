using DotNetty.Buffers;

namespace Codexus.OpenTransport.Codecs.Stream.Codecs;

public class ByteArrayCodec(int maxLength = int.MaxValue) : IByteBufferCodec<byte[]>
{
    private static readonly VarIntCodec LengthCodec = new();

    public byte[] Decode(IByteBuffer buffer)
    {
        var length = LengthCodec.Decode(buffer);

        if (length > maxLength) throw new InvalidOperationException($"ByteArray is too long: {length} > {maxLength}");

        if (length < 0) throw new InvalidOperationException("ByteArray length cannot be negative");

        var bytes = new byte[length];
        buffer.ReadBytes(bytes);
        return bytes;
    }

    public void Encode(IByteBuffer buffer, byte[] value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (value.Length > maxLength)
            throw new InvalidOperationException($"ByteArray is too long: {value.Length} > {maxLength}");

        LengthCodec.Encode(buffer, value.Length);
        buffer.WriteBytes(value);
    }
}