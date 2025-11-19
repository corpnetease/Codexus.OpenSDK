using DotNetty.Buffers;

namespace Codexus.OpenTransport.Codecs.Stream.Codecs;

public class VarIntCodec : IByteBufferCodec<int>
{
    public int Decode(IByteBuffer buffer)
    {
        var value = 0;
        var position = 0;
        byte currentByte;

        do
        {
            if (!buffer.IsReadable()) throw new InvalidOperationException("VarInt is incomplete");

            currentByte = buffer.ReadByte();
            value |= (currentByte & 0x7F) << (position * 7);

            if (position++ > 5) throw new InvalidOperationException("VarInt is too big");
        } while ((currentByte & 0x80) == 0x80);

        return value;
    }

    public void Encode(IByteBuffer buffer, int value)
    {
        while ((value & ~0x7F) != 0)
        {
            buffer.WriteByte((byte)((value & 0x7F) | 0x80));
            value >>= 7;
        }

        buffer.WriteByte((byte)value);
    }
}