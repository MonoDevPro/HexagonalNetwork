using System;
using LiteNetLib;
using Microsoft.Extensions.Logging;
using NetworkHexagonal.Adapters.Outbound.Serialization;
using NetworkHexagonal.Core.Application.Ports.Outbound;
using NetworkHexagonal.Core.Domain.Enums;
using NetworkHexagonal.Core.Domain.Models;

namespace NetworkHexagonal.Adapters.Outbound.Network
{
    /// <summary>
    /// Adaptador para envio de pacotes usando LiteNetLib
    /// </summary>
    public class LiteNetLibPacketSenderAdapter : IPacketSender
    {
        private readonly ILogger<LiteNetLibPacketSenderAdapter> _logger;
        private readonly INetworkSerializer _serializer;
        private readonly LiteNetLibConnectionManagerAdapter _connectionManager;

        public LiteNetLibPacketSenderAdapter(
            INetworkSerializer serializer,
            LiteNetLibConnectionManagerAdapter connectionManager,
            ILogger<LiteNetLibPacketSenderAdapter> logger)
        {
            _serializer = serializer;
            _connectionManager = connectionManager;
            _logger = logger;
        }

        /// <summary>
        /// Envia um pacote para um peer específico
        /// </summary>
        public bool SendPacket<T>(int peerId, T packet, DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered)
            where T : IPacket
        {
            try
            {
                var peer = _connectionManager.GetPeer(peerId);
                if (peer == null)
                {
                    _logger.LogWarning("Não é possível enviar pacote para peer inexistente {PeerId}", peerId);
                    return false;
                }

                var writer = _serializer.Serialize(packet);
                peer.Send(writer.Data, MapDeliveryMode(deliveryMode));

                _logger.LogTrace("Enviado pacote do tipo {PacketType} para peer {PeerId}",
                        packet.GetType().Name, peerId);

                // Recycle the writer to the pool
                writer.Recycle();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao enviar pacote do tipo {PacketType} para peer {PeerId}",
                    packet.GetType().Name, peerId);
                return false;
            }
        }

        /// <summary>
        /// Envia um pacote para todos os peers conectados
        /// </summary>
        public bool Broadcast<T>(T packet, DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered)
            where T : IPacket
        {
            try
            {
                var peers = _connectionManager.GetAllPeers();
                if (peers.Count == 0)
                {
                    _logger.LogWarning("Não há peers conectados para broadcast");
                    return false;
                }

                var writer = _serializer.Serialize(packet);
                var liteNetLibDelivery = MapDeliveryMode(deliveryMode);

                foreach (var peer in peers)
                    peer.Send(writer.Data, liteNetLibDelivery);

                _logger.LogTrace("Broadcast de pacote do tipo {PacketType} para {PeerCount} peers",
                    packet.GetType().Name, peers.Count);

                // Recycle the writer to the pool
                writer.Recycle();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao fazer broadcast de pacote do tipo {PacketType}",
                    packet.GetType().Name);
                return false;
            }
        }

        /// <summary>
        /// Mapeia os modos de entrega do domínio para os modos do LiteNetLib
        /// </summary>
        private DeliveryMethod MapDeliveryMode(DeliveryMode mode)
        {
            return mode switch
            {
                DeliveryMode.Unreliable => DeliveryMethod.Unreliable,
                DeliveryMode.ReliableUnordered => DeliveryMethod.ReliableUnordered,
                DeliveryMode.ReliableOrdered => DeliveryMethod.ReliableOrdered,
                DeliveryMode.ReliableSequenced => DeliveryMethod.ReliableSequenced,
                _ => DeliveryMethod.ReliableOrdered
            };
        }
    }
}