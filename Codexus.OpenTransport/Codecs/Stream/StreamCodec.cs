using Codexus.OpenTransport.Codecs.Stream.Composite;

namespace Codexus.OpenTransport.Codecs.Stream;

public static class StreamCodec
{
    public static IByteBufferCodec<T> Unit<T>(T constantValue)
    {
        return new UnitCodec<T>(() => constantValue);
    }

    public static IByteBufferCodec<T> Unit<T>(Func<T> constructor)
    {
        return new UnitCodec<T>(constructor);
    }

    public static IByteBufferCodec<T> Composite<T, T1>(
        IByteBufferCodec<T1> codec1,
        Func<T, T1> getter1,
        Func<T1, T> constructor)
    {
        return new CompositeCodec1<T, T1>(codec1, getter1, constructor);
    }

    public static IByteBufferCodec<T> Composite<T, T1, T2>(
        IByteBufferCodec<T1> codec1,
        Func<T, T1> getter1,
        IByteBufferCodec<T2> codec2,
        Func<T, T2> getter2,
        Func<T1, T2, T> constructor)
    {
        return new CompositeCodec2<T, T1, T2>(
            codec1, getter1,
            codec2, getter2,
            constructor);
    }


    public static IByteBufferCodec<T> Composite<T, T1, T2, T3>(
        IByteBufferCodec<T1> codec1,
        Func<T, T1> getter1,
        IByteBufferCodec<T2> codec2,
        Func<T, T2> getter2,
        IByteBufferCodec<T3> codec3,
        Func<T, T3> getter3,
        Func<T1, T2, T3, T> constructor)
    {
        return new CompositeCodec3<T, T1, T2, T3>(
            codec1, getter1,
            codec2, getter2,
            codec3, getter3,
            constructor);
    }

    public static IByteBufferCodec<T> Composite<T, T1, T2, T3, T4>(
        IByteBufferCodec<T1> codec1,
        Func<T, T1> getter1,
        IByteBufferCodec<T2> codec2,
        Func<T, T2> getter2,
        IByteBufferCodec<T3> codec3,
        Func<T, T3> getter3,
        IByteBufferCodec<T4> codec4,
        Func<T, T4> getter4,
        Func<T1, T2, T3, T4, T> constructor)
    {
        return new CompositeCodec4<T, T1, T2, T3, T4>(
            codec1, getter1,
            codec2, getter2,
            codec3, getter3,
            codec4, getter4,
            constructor);
    }
}