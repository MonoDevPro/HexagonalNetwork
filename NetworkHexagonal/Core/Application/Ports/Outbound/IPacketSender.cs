using System;
using NetworkHexagonal.Core.Domain.Enums;
using NetworkHexagonal.Core.Domain.Models;

namespace NetworkHexagonal.Core.Application.Ports.Outbound;

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