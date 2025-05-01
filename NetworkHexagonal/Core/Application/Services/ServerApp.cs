using NetworkHexagonal.Core.Application.Ports;
using NetworkHexagonal.Core.Application.Ports.Input;
using NetworkHexagonal.Core.Application.Ports.Output;

namespace NetworkHexagonal.Core.Application.Services
{
    public class ServerApp : IServerNetworkService
    {
        private readonly INetworkService _networkService;
        private readonly IPacketSender _packetSender;
        private readonly IPacketRegistry _packetRegistry;
        private readonly INetworkConfiguration _config;

        public event Action<int> OnPeerConnected;
        public event Action<int> OnPeerDisconnected;
        public event Action<int, IPacket> OnPacketReceivedFromPeer;

        public ServerApp(
            INetworkService networkService,
            IPacketSender packetSender,
            IPacketRegistry packetRegistry,
            INetworkConfiguration config)
        {
            _networkService = networkService;
            _packetSender = packetSender;
            _packetRegistry = packetRegistry;
            _config = config;
        }

        public void Initialize()
        {
            _networkService.Initialize();
            _networkService.Configure(_config);
            // Registrar pacotes padrão se necessário
        }

        public void Configure(INetworkConfiguration config)
        {
            _networkService.Configure(config);
        }

        public void Dispose()
        {
            _networkService.Dispose();
        }

        public void Start()
        {
            if (_networkService is IServerNetworkService server)
            {
                server.Start();
            }
            // Disparar evento de inicialização se necessário
        }

        public void Stop()
        {
            if (_networkService is IServerNetworkService server)
            {
                server.Stop();
            }
            // Disparar evento de parada se necessário
        }

        public void Update()
        {
            if (_networkService is IServerNetworkService server)
            {
                server.Update();
            }
        }

        public void DisconnectPeer(int peerId)
        {
            if (_networkService is IServerNetworkService server)
            {
                server.DisconnectPeer(peerId);
            }
        }
    }
}
