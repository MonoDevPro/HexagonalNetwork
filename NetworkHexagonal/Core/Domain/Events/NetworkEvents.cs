using NetworkHexagonal.Core.Domain.Enums;
using NetworkHexagonal.Core.Domain.Models;

namespace NetworkHexagonal.Core.Domain.Events
{
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
    
    /// <summary>
    /// Evento disparado quando ocorre um erro de rede
    /// </summary>
    public class NetworkErrorEvent
    {
        public string ErrorMessage { get; }
        public int? PeerId { get; }
        
        public NetworkErrorEvent(string errorMessage, int? peerId = null)
        {
            ErrorMessage = errorMessage;
            PeerId = peerId;
        }
    }
}