using Codexus.OpenTransport.Session;

namespace Codexus.OpenTransport.Packet.Handler;

// ReSharper disable once ClassNeverInstantiated.Global
public class PacketHandlerContext
{
    public readonly List<Func<Task>> SendAfterActions = [];
    public required NetworkSession Session { get; init; }

    public required OpenTransport Transport { get; init; }

    public bool IsPropagation { get; private set; } = true;

    public bool IsCancel { get; private set; }

    public void StopPropagation()
    {
        IsPropagation = false;
    }

    public void Cancel()
    {
        IsCancel = true;
    }

    public void Uncancel()
    {
        IsCancel = false;
    }

    public void OnSendAfter(Func<Task> action)
    {
        SendAfterActions.Add(action);
    }

    public void OnSendAfter(Action action)
    {
        SendAfterActions.Add(() =>
        {
            action();
            return Task.CompletedTask;
        });
    }

    public Task SendToRemote(IPacket packet, EnumConnectionState? state = null, EnumProtocolVersion? version = null)
    {
        state ??= Session.State;
        version ??= Session.ProtocolVersion;

        var type = packet.GetType();
        var id = Transport.Registry.GetPacketId
            (version.Value, state.Value, EnumPacketDirection.ServerBound, type);

        if (id == null) throw new ArgumentException($"Packet ID {id} is invalid.");

        if (Session.Remote == null) throw new ArgumentException("Session.Remote cannot be null.");

        return Session.Remote.WriteAndFlushAsync(new PacketWrapper(id.Value, version.Value, state.Value,
            EnumPacketDirection.ServerBound, packet));
    }

    public Task SendToLocal(IPacket packet, EnumConnectionState? state = null, EnumProtocolVersion? version = null)
    {
        state ??= Session.State;
        version ??= Session.ProtocolVersion;

        var type = packet.GetType();
        var id = Transport.Registry.GetPacketId
            (version.Value, state.Value, EnumPacketDirection.ClientBound, type);

        if (id == null) throw new ArgumentException($"Packet ID {id} is invalid.");

        return Session.Local.WriteAndFlushAsync(new PacketWrapper(id.Value, version.Value, state.Value,
            EnumPacketDirection.ClientBound, packet));
    }
}