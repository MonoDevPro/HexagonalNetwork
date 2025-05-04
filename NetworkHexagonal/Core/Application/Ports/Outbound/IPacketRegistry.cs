using System;
using NetworkHexagonal.Core.Domain.Models;

namespace NetworkHexagonal.Core.Application.Ports.Outbound;

public delegate void PacketHandlerDelegate(INetworkReader reader, PacketContext context);

/// <summary>
/// Interface para registro de pacotes e seus manipuladores
/// </summary>
public interface IPacketRegistry
{
    void RegisterHandler<T>(Action<T, PacketContext> handler) where T : IPacket, new();
    bool HasHandler(ulong packetId);
    void HandlePacket(ulong packetId, INetworkReader reader, PacketContext context);
}