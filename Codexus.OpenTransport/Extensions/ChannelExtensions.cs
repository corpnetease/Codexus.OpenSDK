using Codexus.OpenTransport.Session;
using DotNetty.Common.Utilities;
using DotNetty.Transport.Channels;

namespace Codexus.OpenTransport.Extensions;

public static class ChannelExtensions
{
    private static readonly AttributeKey<NetworkSession> SessionKey =
        AttributeKey<NetworkSession>.ValueOf("minecraft:network_session");

    extension(IChannel channel)
    {
        // ReSharper disable once MemberCanBePrivate.Global
        public void SetSession(NetworkSession session)
        {
            channel.GetAttribute(SessionKey).Set(session);
        }

        // ReSharper disable once MemberCanBePrivate.Global
        public NetworkSession? GetSession()
        {
            return channel.GetAttribute(SessionKey).Get();
        }

        // ReSharper disable once MemberCanBePrivate.Global
        public NetworkSession? RemoveSession()
        {
            return channel.GetAttribute(SessionKey).GetAndRemove();
        }
    }

    extension(IChannelHandlerContext context)
    {
        // ReSharper disable once MemberCanBePrivate.Global
        public void SetSession(NetworkSession session)
        {
            context.Channel.SetSession(session);
        }

        // ReSharper disable once MemberCanBePrivate.Global
        public NetworkSession? GetSession()
        {
            return context.Channel.GetSession();
        }

        // ReSharper disable once MemberCanBePrivate.Global
        public NetworkSession? RemoveSession()
        {
            return context.Channel.RemoveSession();
        }
    }
}