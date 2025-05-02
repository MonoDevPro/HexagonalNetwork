using System;
using NetworkHexagonal.Adapters.Outbound.Networking.Serializer;

namespace NetworkHexagonal.Core.Application.Ports.Output
{
    /// <summary>
    /// Interface para gerenciamento de callbacks de pacotes.
    /// Esta porta de saída (adaptador secundário) permite que o núcleo da aplicação
    /// registre callbacks para tipos específicos de pacotes sem conhecer a implementação
    /// concreta do mecanismo de comunicação.
    /// </summary>
    public interface IPacketCallbacks
    {
        /// <summary>
        /// Registra um callback para um tipo específico de pacote.
        /// </summary>
        /// <param name="callback">Ação a ser executada quando um pacote deste tipo for recebido</param>
        void RegisterCallback<TPacket>(Action<TPacket> callback)
            where TPacket : IPacket, new();

        /// <summary>
        /// Registra um callback para um tipo específico de pacote com dados do usuário.
        /// </summary>
        /// <typeparam name="TUserData">Tipo dos dados do usuário</typeparam>
        /// <param name="callback">Ação a ser executada quando um pacote deste tipo for recebido</param>
        void RegisterCallback<TPacket, TUserData>(Action<TPacket, TUserData> callback)
            where TPacket : IPacket, new();

        /// <summary>
        /// Invoca o callback correspondente ao pacote lido do INetworkReader com dados do usuário.
        /// </summary>
        /// <param name="reader">Leitor de rede contendo dados do pacote</param>
        /// <param name="userData">Dados do usuário a serem passados para o callback</param>
        void InvokeCallback(BufferNetworkReader reader);

        /// <summary>
        /// Invoca o callback correspondente ao pacote lido do INetworkReader.
        /// </summary>
        void InvokeCallback(BufferNetworkReader reader, object userData);

        /// <summary>
        /// Cancela o registro de um callback para um tipo específico de pacote.
        /// </summary>
        /// <typeparam name="T">Tipo do pacote</typeparam>
        void UnregisterCallback(ulong id);
    }
}
