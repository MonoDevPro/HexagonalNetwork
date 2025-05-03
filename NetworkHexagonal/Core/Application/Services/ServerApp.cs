using NetworkHexagonal.Core.Application.Ports.Outbound;
using NetworkHexagonal.Core.Domain.Events;

namespace NetworkHexagonal.Core.Application.Services
{
    public interface IServerApp
    {
        public event Action<ConnectionRequestEvent> OnConnectionRequest;
        public event Action<ConnectionEvent> OnPeerConnected;
        public event Action<DisconnectionEvent> OnPeerDisconnected;
        public event Action<NetworkErrorEvent> OnError;
        void DisconnectPeer(int peerId);
        void Dispose();
        void Initialize();
        void Start(int port);
        void Stop();
        void Update();
    }

    public class ServerApp : IServerApp
    {
        private readonly IServerNetworkService _serverNetworkService;
        private readonly IPacketSender _packetSender;
        private readonly IPacketRegistry _packetRegistry;
        private readonly INetworkConfiguration _config;

        public event Action<ConnectionRequestEvent>? OnConnectionRequest;
        public event Action<ConnectionEvent>? OnPeerConnected;
        public event Action<DisconnectionEvent>? OnPeerDisconnected;
        public event Action<NetworkErrorEvent>? OnError;

        public ServerApp(
            IServerNetworkService networkService,
            IPacketSender packetSender,
            IPacketRegistry packetRegistry,
            INetworkConfiguration config)
        {
            _serverNetworkService = networkService;
            _packetSender = packetSender;
            _packetRegistry = packetRegistry;
            _config = config;

            // Registrar eventos
            _serverNetworkService.OnConnectionRequest += (request) =>
            {
                OnConnectionRequest?.Invoke(request);
            };
            _serverNetworkService.OnPeerConnected += (peer) =>
            {
                OnPeerConnected?.Invoke(peer);
            };
            _serverNetworkService.OnPeerDisconnected += (peer) =>
            {
                OnPeerDisconnected?.Invoke(peer);
            };
            _serverNetworkService.OnError += (error) =>
            {
                OnError?.Invoke(error);
            };
        }

        public void Initialize()
        {
            // Registrar pacotes padrão se necessário
        }

        public void Dispose()
        {
            // Desregistrar pacotes se necessário
        }

        public void Start(int port)
        {
            _serverNetworkService.Start(port);
            // Disparar evento de inicialização se necessário
        }

        public void Stop()
        {
            _serverNetworkService.Stop();
            // Disparar evento de parada se necessário
        }

        public void Update()
        {
            _serverNetworkService.Update();
        }

        public void DisconnectPeer(int peerId)
        {
            _serverNetworkService.DisconnectPeer(peerId);
        }
    }
}
