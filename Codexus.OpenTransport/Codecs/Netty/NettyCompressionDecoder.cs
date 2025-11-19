using System.Buffers;
using Codexus.OpenTransport.Extensions;
using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Transport.Channels;
using ICSharpCode.SharpZipLib.Zip.Compression;

namespace Codexus.OpenTransport.Codecs.Netty;

public class NettyCompressionDecoder(int threshold) : ByteToMessageDecoder
{
    private const int InitialBufferSize = 8192;
    private readonly ArrayPool<byte> _arrayPool = ArrayPool<byte>.Shared;
    private readonly Inflater _inflater = new();
    public int Threshold { get; set; } = threshold;

    protected override void Decode(IChannelHandlerContext context, IByteBuffer input, List<object> output)
    {
        if (input.ReadableBytes == 0) return;

        var decompressedLength = input.ReadVarInt();
        if (decompressedLength == 0)
        {
            output.Add(input.ReadBytes(input.ReadableBytes));
            return;
        }

        if (decompressedLength < Threshold)
            throw new DecoderException($"Decompressed length {decompressedLength} is below threshold {Threshold}");

        var compressedData = input.ReadableBytes;
        var inputArray = new byte[compressedData];
        input.ReadBytes(inputArray);

        var buffer = _arrayPool.Rent(Math.Max(InitialBufferSize, decompressedLength));
        try
        {
            _inflater.Reset();
            _inflater.SetInput(inputArray);

            if (_inflater.IsNeedingDictionary) throw new DecoderException("Inflater requires dictionary");

            var byteBuffer = context.Allocator.HeapBuffer(decompressedLength);
            var bytesWritten = 0;

            while (!_inflater.IsFinished && bytesWritten < decompressedLength)
            {
                var count = _inflater.Inflate(buffer);
                if (count == 0 && _inflater.IsNeedingInput) throw new DecoderException("Incomplete compressed data");
                byteBuffer.WriteBytes(buffer, 0, count);
                bytesWritten += count;
            }

            if (bytesWritten != decompressedLength)
            {
                byteBuffer.Release();
                throw new DecoderException(
                    $"Decompressed length mismatch: expected {decompressedLength}, got {bytesWritten}");
            }

            output.Add(byteBuffer);
        }
        finally
        {
            _arrayPool.Return(buffer);
        }
    }
}