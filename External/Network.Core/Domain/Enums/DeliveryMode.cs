namespace Network.Core.Domain.Enums;

/// <summary>
/// Modos de entrega de pacotes
/// </summary>
public enum DeliveryMode
{
    Unreliable,
    ReliableUnordered,
    ReliableOrdered,
    ReliableSequenced
}