namespace Network.Core.Application.Ports.Outbound;

/// <summary>
/// Interface para configuração da rede
/// </summary>
public interface INetworkConfiguration
{
    int UpdateIntervalMs { get; }
    int DisconnectTimeoutMs { get; }
    string ConnectionKey { get; }

    /// <summary>
    /// Determina se os eventos de rede devem ser processados imediatamente (true) ou
    /// apenas quando Update() for chamado (false). Útil para testes vs ambiente de produção.
    /// </summary>
    bool UseUnsyncedEvents { get; }
}