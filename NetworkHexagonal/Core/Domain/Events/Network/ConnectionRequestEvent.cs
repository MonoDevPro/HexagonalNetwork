using System;
using NetworkHexagonal.Core.Domain.Models;

namespace NetworkHexagonal.Core.Domain.Events.Network;

/// <summary>
/// Evento disparado quando há uma solicitação de conexão
/// </summary>
// Evento de domínio para notificar quando há uma solicitação de conexão de um peer externo.
// Permite lógica de autenticação, autorização ou logging desacoplada da infraestrutura.
// Handlers podem decidir aceitar ou rejeitar a conexão.
public class ConnectionRequestEvent
{
    public ConnectionRequestEventArgs EventArgs { get; }

    public ConnectionRequestEvent(ConnectionRequestEventArgs eventArgs)
    {
        EventArgs = eventArgs;
    }
}