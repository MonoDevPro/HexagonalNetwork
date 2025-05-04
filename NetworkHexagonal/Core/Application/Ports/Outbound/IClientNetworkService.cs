using NetworkHexagonal.Core.Domain.Events.Network;
using NetworkHexagonal.Core.Domain.Models;

namespace NetworkHexagonal.Core.Application.Ports.Outbound;

/// <summary>
/// Interface para o serviço de rede do cliente
/// </summary>
public interface IClientNetworkService
{

    Task<ConnectionResult> ConnectAsync(string serverAddress, int port, int timeoutMs = 5000);
    void Disconnect();
    void Update();
}