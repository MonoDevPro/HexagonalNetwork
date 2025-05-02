using System.Threading.Tasks;
using NetworkHexagonal.Core.Application.Ports;
using NetworkHexagonal.Core.Application.Ports.Input;
using NetworkHexagonal.Core.Application.Ports.Output;

namespace NetworkHexagonal.Core.Application.Services
{
    public class ClientApp : IClientNetworkService
    {
        private readonly IClientNetworkService _clientNetworkService;
        private readonly IPacketSender _packetSender;
        private readonly IPacketRegistry _packetRegistry;
        private readonly INetworkConfiguration _config;

        public event Action? OnConnected;
        public event Action<string>? OnDisconnected;
        public event Action<int>? OnPingReceived;
        public event Action<int>? OnPacketReceived;

        public ClientApp(
            INetworkService networkService,
            IPacketSender packetSender,
            IPacketRegistry packetRegistry,
            INetworkConfiguration config)
        {
            if (networkService is not IClientNetworkService clientNetworkService)
            {
                throw new ArgumentException("networkService must implement IClientNetworkService");
            }

            _clientNetworkService = clientNetworkService;
            _packetSender = packetSender;
            _packetRegistry = packetRegistry;
            _config = config;

            // Registrar eventos de conexão
            _clientNetworkService.OnConnected += () =>
            {
                OnConnected?.Invoke();
            };
            _clientNetworkService.OnDisconnected += (reason) =>
            {
                OnDisconnected?.Invoke(reason);
            };
            _clientNetworkService.OnPingReceived += (ping) =>
            {
                OnPingReceived?.Invoke(ping);
            };
            // Registrar eventos de recebimento de pacotes
            _clientNetworkService.OnPacketReceived += (peerId) =>
            {
                OnPacketReceived?.Invoke(peerId);
            };
        }

        public void Initialize()
        {
            _clientNetworkService.Initialize();
            _clientNetworkService.Configure(_config);
            // Aqui você pode registrar pacotes padrão
        }

        public void Configure(INetworkConfiguration config)
        {
            _clientNetworkService.Configure(config);
        }

        public void Dispose()
        {
            _clientNetworkService.Dispose();
        }

        public async Task ConnectAsync(int timeoutMs = 5000)
        {
            // Supondo que o adapter tenha um método ConnectAsync
            await _clientNetworkService.ConnectAsync(timeoutMs);
            //OnConnected?.Invoke();
        }

        public void Disconnect()
        {
            _clientNetworkService.Disconnect();
            //OnDisconnected?.Invoke();
        }

        public void Update()
        {
            _clientNetworkService.Update();
        }
    }
}
