using System;

namespace NetworkHexagonal.Core.Domain.Events;

/// <summary>
/// Evento disparado quando uma conexão é estabelecida
/// </summary>
public class ConnectionEvent
{
    public int PeerId { get; }

    public ConnectionEvent(int peerId)
    {
        PeerId = peerId;
    }
}