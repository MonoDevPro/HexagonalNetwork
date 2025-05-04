using System;
using NetworkHexagonal.Core.Application.Ports.Outbound;
using NetworkHexagonal.Core.Domain.Models;

namespace NetworkHexagonal.Core.Application.Ports.Inbound;

public interface IClientNetworkApp
{
    INetworkEventBus EventBus { get; }
    IPacketSender PacketSender { get; }
    IPacketRegistry PacketRegistry { get; }
    INetworkConfiguration Configuration { get; }
    void Initialize();
    Task<ConnectionResult> ConnectAsync(string serverAddress, int port, int timeoutMs = 5000);
    void Disconnect();
    void Update();
    void Dispose();
}