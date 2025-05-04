using NetworkHexagonal.Core.Domain.Enums;

namespace NetworkHexagonal.Core.Domain.Events.Network;

/// <summary>
/// Evento disparado quando uma desconexão acontece
/// </summary>
public class DisconnectionEvent
{
    // Evento de domínio para notificar quando uma desconexão ocorre.
    // Permite que handlers de persistência, métricas ou lógica de negócio reajam à saída de um peer.
    // Mantém o domínio desacoplado da infraestrutura de rede.
    public int PeerId { get; }
    public DisconnectReason Reason { get; }

    public DisconnectionEvent(int peerId, DisconnectReason reason)
    {
        PeerId = peerId;
        Reason = reason;
    }
}