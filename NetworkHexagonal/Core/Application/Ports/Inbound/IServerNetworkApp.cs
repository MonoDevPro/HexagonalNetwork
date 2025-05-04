using System;
using NetworkHexagonal.Core.Application.Ports.Outbound;

namespace NetworkHexagonal.Core.Application.Ports.Inbound;

public interface IServerNetworkApp
{
    INetworkEventBus EventBus { get; }
    IPacketSender PacketSender { get; }
    IPacketRegistry PacketRegistry { get; }
    INetworkConfiguration Configuration { get; }
    void Initialize();
    bool Start(int port);
    void Stop();
    void DisconnectPeer(int peerId);
    void Update();
    void Dispose();
}