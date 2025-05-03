using System;

namespace NetworkHexagonal.Core.Domain.Exceptions
{
    /// <summary>
    /// Exceção base para erros de rede
    /// </summary>
    public class NetworkException : Exception
    {
        public NetworkException(string message) : base(message) { }
        public NetworkException(string message, Exception innerException) : base(message, innerException) { }
    }
    
    /// <summary>
    /// Exceção lançada quando ocorre um erro de conexão
    /// </summary>
    public class ConnectionException : NetworkException
    {
        public ConnectionException(string message) : base(message) { }
        public ConnectionException(string message, Exception innerException) : base(message, innerException) { }
    }
    
    /// <summary>
    /// Exceção lançada quando ocorre um erro no processamento de pacotes
    /// </summary>
    public class PacketHandlingException : NetworkException
    {
        public PacketHandlingException(string message) : base(message) { }
        public PacketHandlingException(string message, Exception innerException) : base(message, innerException) { }
    }
    
    /// <summary>
    /// Exceção lançada quando ocorre um erro de serialização ou desserialização
    /// </summary>
    public class SerializationException : NetworkException
    {
        public SerializationException(string message) : base(message) { }
        public SerializationException(string message, Exception innerException) : base(message, innerException) { }
    }
}