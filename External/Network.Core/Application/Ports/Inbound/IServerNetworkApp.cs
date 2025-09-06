using Network.Core.Application.Options;
using Network.Core.Application.Ports.Outbound;

namespace Network.Core.Application.Ports.Inbound;

public interface IServerNetworkApp
{
    NetworkOptions Options { get; }
    IConnectionManager ConnectionManager { get; }
    IPacketSender PacketSender { get; }
    IPacketRegistry PacketRegistry { get; }
    INetworkEventBus EventBus { get; }
    bool Start();
    void Stop();
    void DisconnectPeer(int peerId);
    void Update();
    void Dispose();
}