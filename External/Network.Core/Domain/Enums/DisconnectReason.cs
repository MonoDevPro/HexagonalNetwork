namespace Network.Core.Domain.Enums;

/// <summary>
/// Razões para desconexão de um peer
/// </summary>
public enum DisconnectReason
{
    Unknown,
    Timeout,
    Rejected,
    ConnectionClosed,
    SocketError,
    RemoteClose
}