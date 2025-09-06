using Network.Core.Domain.Enums;
using Network.Core.Domain.Models;

namespace Network.Core.Application.Ports.Outbound;

/// <summary>
/// Interface para o serviço de envio de pacotes
/// </summary>
public interface IPacketSender
{
    bool SendPacket<T>(int peerId, T packet, DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered)
        where T : IPacket;
    bool Broadcast<T>(T packet, DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered)
        where T : IPacket;
}