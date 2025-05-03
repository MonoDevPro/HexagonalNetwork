using NetworkHexagonal.Core.Domain.Enums;

namespace NetworkHexagonal.Core.Domain.Events;

/// <summary>
/// Evento disparado quando uma desconexão acontece
/// </summary>
public class DisconnectionEvent
{
    public int PeerId { get; }
    public DisconnectReason Reason { get; }

    public DisconnectionEvent(int peerId, DisconnectReason reason)
    {
        PeerId = peerId;
        Reason = reason;
    }
}