using System;
using NetworkHexagonal.Core.Application.Ports.Outbound;

namespace NetworkHexagonal.Core.Application.Ports.Inbound;

public interface IServerNetworkApp
{
    INetworkConfiguration Configuration { get; }
    IConnectionManager ConnectionManager { get; }
    IPacketSender PacketSender { get; }
    IPacketRegistry PacketRegistry { get; }
    INetworkEventBus EventBus { get; }
    void Initialize();
    bool Start(int port);
    void Stop();
    void DisconnectPeer(int peerId);
    void Update();
    void Dispose();
}