namespace Codexus.OpenTransport.Codecs.Stream;

public interface IStreamCodec<in TBuffer, T>
{
    T Decode(TBuffer buffer);
    void Encode(TBuffer buffer, T value);
}