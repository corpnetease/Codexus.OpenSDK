using System.Text;
using DotNetty.Buffers;

namespace Codexus.OpenTransport.Codecs.Stream.Codecs;

public class StringCodec(int maxLength = 32767) : IByteBufferCodec<string>
{
    private static readonly VarIntCodec LengthCodec = new();

    public string Decode(IByteBuffer buffer)
    {
        var length = LengthCodec.Decode(buffer);

        if (length > maxLength) throw new InvalidOperationException($"String is too long: {length} > {maxLength}");

        if (length < 0) throw new InvalidOperationException("String length cannot be negative");

        if (buffer.ReadableBytes < length) throw new InvalidOperationException("Not enough bytes to read string");

        var bytes = new byte[length];
        buffer.ReadBytes(bytes);

        return Encoding.UTF8.GetString(bytes);
    }

    public void Encode(IByteBuffer buffer, string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var bytes = Encoding.UTF8.GetBytes(value);

        if (bytes.Length > maxLength)
            throw new InvalidOperationException($"String is too long: {bytes.Length} > {maxLength}");

        LengthCodec.Encode(buffer, bytes.Length);
        buffer.WriteBytes(bytes);
    }
}