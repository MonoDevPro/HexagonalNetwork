namespace NetworkHexagonal.Core.Domain.Events.Network;

public readonly record struct ConnectionLatencyEvent
{
    // Evento de domínio para notificar quando a latência de uma conexão é estabelecida com sucesso.
    // Usado para acionar handlers de lógica de negócio, persistência ou métricas.
    // Mantém o domínio desacoplado da infraestrutura de rede.
    public int PeerId { get; }
    public int Latency { get; }

    public ConnectionLatencyEvent(int peerId, int latency)
    {
        PeerId = peerId;
        Latency = latency;
    }
}