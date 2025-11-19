using DotNetty.Buffers;

namespace Codexus.OpenTransport.Codecs.Stream.Codecs;

public class UuidCodec : IByteBufferCodec<Guid>
{
    public Guid Decode(IByteBuffer buffer)
    {
        var mostSigBits = buffer.ReadLong();
        var leastSigBits = buffer.ReadLong();

        var bytes = new byte[16];
        BitConverter.GetBytes(mostSigBits).CopyTo(bytes, 0);
        BitConverter.GetBytes(leastSigBits).CopyTo(bytes, 8);

        return new Guid(bytes);
    }

    public void Encode(IByteBuffer buffer, Guid value)
    {
        var bytes = value.ToByteArray();
        var mostSigBits = BitConverter.ToInt64(bytes, 0);
        var leastSigBits = BitConverter.ToInt64(bytes, 8);

        buffer.WriteLong(mostSigBits);
        buffer.WriteLong(leastSigBits);
    }
}