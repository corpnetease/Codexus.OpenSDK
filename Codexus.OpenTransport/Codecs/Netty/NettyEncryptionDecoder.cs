using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Transport.Channels;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;

namespace Codexus.OpenTransport.Codecs.Netty;

public class NettyEncryptionDecoder : ByteToMessageDecoder
{
    private readonly CfbBlockCipher _decipher;

    public NettyEncryptionDecoder(byte[] key)
    {
        _decipher = new CfbBlockCipher(new AesEngine(), 8);
        _decipher.Init(false, new ParametersWithIV(new KeyParameter(key), key));
    }

    protected override void Decode(IChannelHandlerContext context, IByteBuffer input, List<object> output)
    {
        var bytesToRead = input.ReadableBytes;
        var outputBuffer = context.Allocator.HeapBuffer(bytesToRead);
        var endIndex = input.ReaderIndex + input.ArrayOffset + bytesToRead;
        var outputOffset = outputBuffer.ArrayOffset;
        for (var currentIndex = input.ReaderIndex + input.ArrayOffset; currentIndex < endIndex; currentIndex++)
        {
            _decipher.ProcessBlock(input.Array, currentIndex, outputBuffer.Array, outputOffset);
            outputOffset++;
        }

        outputBuffer.SetWriterIndex(bytesToRead);
        input.SkipBytes(bytesToRead);
        output.Add(outputBuffer);
    }
}