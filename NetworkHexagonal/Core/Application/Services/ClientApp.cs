using System.Threading.Tasks;
using NetworkHexagonal.Core.Application.Ports;
using NetworkHexagonal.Core.Application.Ports.Input;
using NetworkHexagonal.Core.Application.Ports.Output;

namespace NetworkHexagonal.Core.Application.Services
{
    public class ClientApp : IClientNetworkService
    {
        private readonly INetworkService _networkService;
        private readonly IPacketSender _packetSender;
        private readonly IPacketRegistry _packetRegistry;
        private readonly INetworkConfiguration _config;

        public event Action? OnConnected;
        public event Action? OnDisconnected;
        public event Action<IPacket>? OnPacketReceived;

        public ClientApp(
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
            // Aqui você pode registrar pacotes padrão
        }

        public void Configure(INetworkConfiguration config)
        {
            _networkService.Configure(config);
        }

        public void Dispose()
        {
            _networkService.Dispose();
        }

        public async Task ConnectAsync(int timeoutMs = 5000)
        {
            // Supondo que o adapter tenha um método ConnectAsync
            if (_networkService is IClientNetworkService client)
            {
                await client.ConnectAsync(timeoutMs);
            }
            OnConnected?.Invoke();
        }

        public void Disconnect()
        {
            if (_networkService is IClientNetworkService client)
            {
                client.Disconnect();
            }
            OnDisconnected?.Invoke();
        }

        public void Update()
        {
            if (_networkService is IClientNetworkService client)
            {
                client.Update();
            }
        }
    }
}
