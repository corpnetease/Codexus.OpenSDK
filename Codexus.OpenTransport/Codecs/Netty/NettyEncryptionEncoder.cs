using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Transport.Channels;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;

namespace Codexus.OpenTransport.Codecs.Netty;

public class NettyEncryptionEncoder : MessageToByteEncoder<IByteBuffer>
{
    private readonly CfbBlockCipher _encryptor;

    public NettyEncryptionEncoder(byte[] key)
    {
        _encryptor = new CfbBlockCipher(new AesEngine(), 8);
        _encryptor.Init(true, new ParametersWithIV(new KeyParameter(key), key));
    }

    protected override void Encode(IChannelHandlerContext context, IByteBuffer message, IByteBuffer output)
    {
        var messageLength = message.ReadableBytes;
        output.EnsureWritable(messageLength);
        var messageEndIndex = messageLength + message.ArrayOffset + message.ReaderIndex;
        var outputOffset = output.ArrayOffset;
        output.SetWriterIndex(messageLength);
        for (var currentIndex = message.ArrayOffset + message.ReaderIndex;
             currentIndex < messageEndIndex;
             currentIndex++)
        {
            _encryptor.ProcessBlock(message.Array, currentIndex, output.Array, outputOffset);
            outputOffset++;
        }

        message.SkipBytes(messageLength);
    }
}