using System;
using NetworkHexagonal.Core.Domain.Models;

namespace NetworkHexagonal.Core.Domain.Events;

/// <summary>
    /// Evento disparado quando há uma solicitação de conexão
    /// </summary>
    public class ConnectionRequestEvent
    {
        public ConnectionRequestEventArgs EventArgs { get; }
        
        public ConnectionRequestEvent(ConnectionRequestEventArgs eventArgs)
        {
            EventArgs = eventArgs;
        }
    }