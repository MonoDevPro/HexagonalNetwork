using Network.Core.Application.Options;
using Network.Core.Application.Ports.Outbound;
using Network.Core.Domain.Models;

namespace Network.Core.Application.Ports.Inbound;

public interface IClientNetworkApp : IDisposable
{
    NetworkOptions Options { get; }
    IConnectionManager ConnectionManager { get; }
    IPacketSender PacketSender { get; }
    IPacketRegistry PacketRegistry { get; }
    INetworkEventBus EventBus { get; }

    void Initialize();
    Task<ConnectionResult> ConnectAsync();
    bool TryConnect(out ConnectionResult result);
    void Disconnect();
    void Update(float deltaTime);
}