using DotNetty.Buffers;

namespace Codexus.OpenTransport.Codecs.Stream;

public interface IByteBufferCodecBase
{
    object Decode(IByteBuffer buffer);
    void Encode(IByteBuffer buffer, object value);
}