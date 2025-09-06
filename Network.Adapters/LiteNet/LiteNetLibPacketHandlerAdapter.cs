using LiteNetLib;
using Microsoft.Extensions.Logging;
using Network.Core.Application.Ports.Outbound;
using Network.Core.Domain.Events;
using Network.Core.Domain.Models;

namespace Network.Adapters.LiteNet
{
    /// <summary>
    /// Adaptador para processamento de pacotes recebidos pela rede
    /// </summary>
    public class LiteNetLibPacketHandlerAdapter(
        IPacketRegistry packetRegistry,
        ILogger<LiteNetLibPacketHandlerAdapter> logger)
    {
        public event Action<NetworkErrorEvent>? OnError;

        /// <summary>
        /// Processa um pacote recebido da rede
        /// </summary>
        public void HandleNetworkReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod method)
        {
            var networkReader = LiteNetLibReaderAdapter.Pool.Get();
            try
            {
                // Obtém o ID do pacote do cabeçalho
                networkReader.SetSource(reader.RawData);
                networkReader.ResetPosition(reader.Position);
                var packetId = networkReader.ReadULong();
                
                // Cria contexto do pacote
                var context = new PacketContext(peer.Id, channel);
                
                // Processa o pacote
                packetRegistry.HandlePacket(packetId, networkReader, context);

            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro ao processar pacote de rede do peer {PeerId}", peer.Id);
                OnError?.Invoke(new NetworkErrorEvent(ex.Message, peer.Id));
            }
            finally
            {
                networkReader.Recycle();
                reader.Recycle();
            }
        }
    }
}