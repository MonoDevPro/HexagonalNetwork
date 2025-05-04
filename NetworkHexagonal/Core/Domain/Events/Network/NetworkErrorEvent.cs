namespace NetworkHexagonal.Core.Domain.Events.Network;

/// <summary>
/// Evento disparado quando ocorre um erro de rede
/// Evento de domínio para notificar quando ocorre um erro de rede relevante para a aplicação.
/// Permite logging, métricas, alertas ou lógica de recuperação desacoplada da infraestrutura.
/// Pode ser usado para monitoramento e auditoria de falhas de comunicação.
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