using Network.Core.Application.Ports.Outbound;
using Network.Core.Domain.Models;

namespace Network.Core.Application.Ports.Inbound;

public interface IClientNetworkApp
{
    INetworkConfiguration Configuration { get; }
    IConnectionManager ConnectionManager { get; }
    IPacketSender PacketSender { get; }
    IPacketRegistry PacketRegistry { get; }
    INetworkEventBus EventBus { get; }
    void Initialize();
    Task<ConnectionResult> ConnectAsync(string serverAddress, int port, int timeoutMs = 5000);
    void Disconnect();
    void Update();
    void Dispose();
}