
namespace NetworkHexagonal.Core.Domain.Events;

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