using LiteNetLib;
using Microsoft.Extensions.Logging;
using NetworkHexagonal.Core.Application.Ports.Outbound;
using NetworkHexagonal.Core.Domain.Events.Network;
using NetworkHexagonal.Core.Domain.Models;

namespace NetworkHexagonal.Adapters.Outbound.Network
{
    /// <summary>
    /// Adaptador para processamento de pacotes recebidos pela rede
    /// </summary>
    public class LiteNetLibPacketHandlerAdapter
    {
        private readonly ILogger<LiteNetLibPacketHandlerAdapter> _logger;
        private readonly IPacketRegistry _packetRegistry;
        
        public event Action<NetworkErrorEvent>? OnError;
        
        public LiteNetLibPacketHandlerAdapter(
            IPacketRegistry packetRegistry,
            ILogger<LiteNetLibPacketHandlerAdapter> logger)
        {
            _packetRegistry = packetRegistry;
            _logger = logger;
        }
        
        /// <summary>
        /// Processa um pacote recebido da rede
        /// </summary>
        public void HandleNetworkReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod method)
        {
            try
            {
                // Obtém o ID do pacote do cabeçalho
                
                var networkReader = LiteNetLibReaderAdapter.Pool.Get();
                networkReader.SetSource(reader.RawData);
                networkReader.Reset(reader.Position);
                var packetId = networkReader.ReadULong();
                
                // Cria contexto do pacote
                var context = new PacketContext(peer.Id, channel);
                
                // Processa o pacote
                _packetRegistry.HandlePacket(packetId, networkReader, context);

                networkReader.Recycle();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar pacote de rede do peer {PeerId}", peer.Id);
                OnError?.Invoke(new NetworkErrorEvent(ex.Message, peer.Id));
            }
            finally
            {
                reader.Recycle();
            }
        }
    }
}