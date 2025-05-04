using System;

namespace NetworkHexagonal.Core.Domain.Events.Network;

/// <summary>
/// Evento disparado quando uma conexão é estabelecida
/// </summary>
public class ConnectionEvent
{
    // Evento de domínio para notificar quando uma conexão é estabelecida com sucesso.
    // Usado para acionar handlers de lógica de negócio, persistência ou métricas.
    // Mantém o domínio desacoplado da infraestrutura de rede.
    public int PeerId { get; }

    public ConnectionEvent(int peerId)
    {
        PeerId = peerId;
    }
}