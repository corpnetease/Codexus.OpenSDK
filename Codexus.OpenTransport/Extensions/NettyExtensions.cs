using DotNetty.Buffers;

namespace Codexus.OpenTransport.Extensions;

public static class NettyExtensions
{
    extension(byte[] buffer)
    {
        public int ReadVarInt()
        {
            var num = 0;
            var num2 = 0;
            foreach (sbyte b in buffer)
            {
                num |= (b & sbyte.MaxValue) << (num2++ * 7);
                if (num2 > 5) throw new Exception("VarInt too big");

                if ((b & 128) != 128) return num;
            }

            throw new IndexOutOfRangeException();
        }
    }

    extension(int input)
    {
        public int GetVarIntSize()
        {
            for (var i = 1; i < 5; i++)
                if ((input & (-1 << (i * 7))) == 0)
                    return i;

            return 5;
        }
    }

    extension(IByteBuffer buffer)
    {
        public IByteBuffer WriteVarInt(int input)
        {
            while ((input & -128) != 0)
            {
                buffer.WriteByte((input & 127) | 128);
                input >>>= 7;
            }

            buffer.WriteByte(input);
            return buffer;
        }

        public int ReadVarInt()
        {
            var value = 0;
            var position = 0;

            while (true)
            {
                var currentByte = buffer.ReadByte();
                value |= (currentByte & 0x7F) << position;

                if ((currentByte & 0x80) == 0) break;

                position += 7;
                if (position >= 32) throw new Exception("VarInt is too big");
            }

            return value;
        }
    }
}