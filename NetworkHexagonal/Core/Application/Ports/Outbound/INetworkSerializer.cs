using NetworkHexagonal.Core.Domain.Models;

namespace NetworkHexagonal.Core.Application.Ports.Outbound
{
    /// <summary>
    /// Interface para serialização/deserialização de pacotes de rede
    /// </summary>
    public interface INetworkSerializer
    {
        /// <summary>
        /// Registra um tipo de pacote para serialização
        /// </summary>
        void RegisterPacketType<T>() where T : IPacket, new();
        
        /// <summary>
        /// Obtém o identificador único de um tipo de pacote
        /// </summary>
        ulong GetPacketId<T>() where T : IPacket;
        
        /// <summary>
        /// Serializa um pacote para envio
        /// </summary>
        object Serialize<T>(T packet) where T : IPacket;
        
        /// <summary>
        /// Deserializa um pacote recebido para o tipo específico
        /// </summary>
        T Deserialize<T>(INetworkReader reader) where T : IPacket, new();
        
        /// <summary>
        /// Deserializa um pacote recebido com base no ID
        /// </summary>
        IPacket Deserialize(ulong packetId, INetworkReader reader);
    }
}