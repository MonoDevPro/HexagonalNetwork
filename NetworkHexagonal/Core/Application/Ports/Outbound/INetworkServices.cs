using NetworkHexagonal.Core.Domain.Enums;
using NetworkHexagonal.Core.Domain.Events;
using NetworkHexagonal.Core.Domain.Models;

namespace NetworkHexagonal.Core.Application.Ports.Outbound
{
    public delegate void PacketHandlerDelegate(INetworkReader reader, PacketContext context);
    
    /// <summary>
    /// Interface para configuração da rede
    /// </summary>
    public interface INetworkConfiguration
    {
        int UpdateIntervalMs { get; }
        int DisconnectTimeoutMs { get; }
        string ConnectionKey { get; }
        
        /// <summary>
        /// Determina se os eventos de rede devem ser processados imediatamente (true) ou
        /// apenas quando Update() for chamado (false). Útil para testes vs ambiente de produção.
        /// </summary>
        bool UseUnsyncedEvents { get; }
    }
    
    /// <summary>
    /// Interface para o serviço de rede do cliente
    /// </summary>
    public interface IClientNetworkService
    {
        Task<ConnectionResult> ConnectAsync(string serverAddress, int port, int timeoutMs = 5000);
        void Disconnect();
        void Update();
        
        event Action<ConnectionEvent> OnConnected;
        event Action<DisconnectionEvent> OnDisconnected;
        event Action<NetworkErrorEvent> OnError;
    }
    
    /// <summary>
    /// Interface para o serviço de rede do servidor
    /// </summary>
    public interface IServerNetworkService
    {
        bool Start(int port);
        void Stop();
        void DisconnectPeer(int peerId);
        void Update();
        
        event Action<ConnectionRequestEvent> OnConnectionRequest;
        event Action<ConnectionEvent> OnPeerConnected;
        event Action<DisconnectionEvent> OnPeerDisconnected;
        event Action<NetworkErrorEvent> OnError;
    }
    
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
    
    /// <summary>
    /// Interface para registro de pacotes e seus manipuladores
    /// </summary>
    public interface IPacketRegistry
    {
        void RegisterHandler<T>(Action<T, PacketContext> handler) where T : IPacket, new();
        bool HasHandler(ulong packetId);
        void HandlePacket(ulong packetId, INetworkReader reader, PacketContext context);
    }
    
    /// <summary>
    /// Interface para gerenciamento de conexões
    /// </summary>
    public interface IConnectionManager
    {
        int GetConnectedPeerCount();
    }
}