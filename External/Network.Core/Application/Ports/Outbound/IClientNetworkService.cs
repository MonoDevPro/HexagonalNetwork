using Network.Core.Domain.Models;

namespace Network.Core.Application.Ports.Outbound;

/// <summary>
/// Interface para o serviço de rede do cliente
/// </summary>
public interface IClientNetworkService
{
    bool TryConnect(string serverAddress, int port, out ConnectionResult result);
    void Disconnect();
    void Update();
}