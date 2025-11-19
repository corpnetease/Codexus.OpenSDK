using DotNetty.Buffers;

namespace Codexus.OpenTransport.Codecs.Stream.Codecs;

public class VarLongCodec : IByteBufferCodec<long>
{
    public long Decode(IByteBuffer buffer)
    {
        long value = 0;
        var position = 0;
        byte currentByte;

        do
        {
            if (!buffer.IsReadable()) throw new InvalidOperationException("VarLong is incomplete");

            currentByte = buffer.ReadByte();
            value |= (long)(currentByte & 0x7F) << (position * 7);

            if (position++ > 10) throw new InvalidOperationException("VarLong is too big");
        } while ((currentByte & 0x80) == 0x80);

        return value;
    }

    public void Encode(IByteBuffer buffer, long value)
    {
        while ((value & ~0x7FL) != 0)
        {
            buffer.WriteByte((byte)((value & 0x7F) | 0x80));
            value >>= 7;
        }

        buffer.WriteByte((byte)value);
    }
}