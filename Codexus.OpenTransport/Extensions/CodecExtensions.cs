using Codexus.OpenTransport.Codecs.Stream;
using Codexus.OpenTransport.Codecs.Stream.Codecs;

namespace Codexus.OpenTransport.Extensions;

public static class CodecExtensions
{
    extension<T>(IByteBufferCodec<T> codec) where T : struct
    {
        public IByteBufferCodec<T?> Optional()
        {
            return new OptionalCodec<T>(codec);
        }
    }

    extension<T>(IByteBufferCodec<T> codec) where T : class
    {
        public IByteBufferCodec<T?> OptionalRef()
        {
            return new OptionalReferenceCodec<T>(codec);
        }
    }

    extension<T>(IByteBufferCodec<T> codec)
    {
        public IByteBufferCodec<List<T>> List(int maxSize = int.MaxValue)
        {
            return new ListCodec<T>(codec, maxSize);
        }

        public IByteBufferCodec<TResult> Map<TResult>(Func<T, TResult> to, Func<TResult, T> from)
        {
            return StreamCodec.Composite(codec, from, to);
        }
    }
}