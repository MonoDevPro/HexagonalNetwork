using System.Net;
using NetworkHexagonal.Core.Application.Ports;
using NetworkHexagonal.Core.Application.Ports.Input;
using NetworkHexagonal.Core.Application.Ports.Output;

namespace NetworkHexagonal.Core.Application.Services
{
    public class ServerApp : IServerNetworkService
    {
        private readonly IServerNetworkService _serverNetworkService;
        private readonly IPacketSender _packetSender;
        private readonly IPacketRegistry _packetRegistry;
        private readonly INetworkConfiguration _config;
        
        public event Action<bool, IPEndPoint>? OnConnectionRequest;
        public event Action<int>? OnPeerConnected;
        public event Action<int>? OnPeerDisconnected;
        public event Action<int, int>? OnPingReceivedFromPeer;
        public event Action<int>? OnPacketReceivedFromPeer;

        public ServerApp(
            INetworkService networkService,
            IPacketSender packetSender,
            IPacketRegistry packetRegistry,
            INetworkConfiguration config)
        {
            if (networkService is not IServerNetworkService serverNetworkService)
            {
                throw new ArgumentException("networkService must implement IServerNetworkService");
            }
            
            _serverNetworkService = serverNetworkService;
            _packetSender = packetSender;
            _packetRegistry = packetRegistry;
            _config = config;

            // Registrar eventos de conexão
            _serverNetworkService.OnConnectionRequest += (accept, endPoint) =>
            {
                OnConnectionRequest?.Invoke(accept, endPoint);
            };
            _serverNetworkService.OnPeerConnected += (peerId) =>
            {
                OnPeerConnected?.Invoke(peerId);
            };
            _serverNetworkService.OnPeerDisconnected += (peerId) =>
            {
                OnPeerDisconnected?.Invoke(peerId);
            };
            _serverNetworkService.OnPingReceivedFromPeer += (peerId, ping) =>
            {
                OnPingReceivedFromPeer?.Invoke(peerId, ping);
            };
            // Registrar eventos de recebimento de pacotes
            _serverNetworkService.OnPacketReceivedFromPeer += (peerId) =>
            {
                OnPacketReceivedFromPeer?.Invoke(peerId);
            };
        }

        public void Initialize()
        {
            _serverNetworkService.Initialize();
            _serverNetworkService.Configure(_config);
            // Registrar pacotes padrão se necessário
        }

        public void Configure(INetworkConfiguration config)
        {
            _serverNetworkService.Configure(config);
        }

        public void Dispose()
        {
            _serverNetworkService.Dispose();
        }

        public void Start()
        {
            _serverNetworkService.Start();
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
