using System;

namespace NetworkHexagonal.Core.Domain.Models
{
    /// <summary>
    /// Argumentos para um evento de requisição de conexão
    /// </summary>
    public class ConnectionRequestEventArgs
    {
        public ConnectionRequestInfo RequestInfo { get; }
        public bool ShouldAccept { get; set; } = true;
        
        public ConnectionRequestEventArgs(ConnectionRequestInfo requestInfo)
        {
            RequestInfo = requestInfo;
        }
    }
}