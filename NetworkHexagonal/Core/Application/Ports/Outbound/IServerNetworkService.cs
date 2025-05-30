using System;
using NetworkHexagonal.Core.Domain.Events;

namespace NetworkHexagonal.Core.Application.Ports.Outbound;

/// <summary>
/// Interface para o serviço de rede do servidor
/// </summary>
public interface IServerNetworkService
{
    
    bool Start(int port);
    void Stop();
    void DisconnectPeer(int peerId);
    void Update();
}