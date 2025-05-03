using NetworkHexagonal.Core.Application.Ports.Inbound;
using NetworkHexagonal.Core.Application.Ports.Outbound;
using NetworkHexagonal.Core.Domain.Events;

namespace NetworkHexagonal.Core.Application.Services
{
    public class ClientApp : IClientApp
    {
        private readonly IClientNetworkService _clientNetworkService;
        private readonly IPacketSender _packetSender;
        private readonly IPacketRegistry _packetRegistry;
        private readonly INetworkConfiguration _config;

        public event Action<ConnectionEvent>? OnConnected;
        public event Action<DisconnectionEvent>? OnDisconnected;
        public event Action<NetworkErrorEvent>? OnError;

        public ClientApp(
            IClientNetworkService networkService,
            IPacketSender packetSender,
            IPacketRegistry packetRegistry,
            INetworkConfiguration config)
        {
            _clientNetworkService = networkService;
            _packetSender = packetSender;
            _packetRegistry = packetRegistry;
            _config = config;

            // Registrar eventos de conexão
            _clientNetworkService.OnConnected += (connectionEvent) =>
            {
                OnConnected?.Invoke(connectionEvent);
            };
            _clientNetworkService.OnDisconnected += (disconnectionEvent) =>
            {
                OnDisconnected?.Invoke(disconnectionEvent);
            };
            _clientNetworkService.OnError += (errorEvent) =>
            {
                OnError?.Invoke(errorEvent);
            };
        }

        public void Initialize()
        {
            
            // Aqui você pode registrar pacotes padrão
        }

        public void Dispose()
        {
            
        }

        public async Task ConnectAsync(string serverAddress, int port, int timeoutMs = 5000)
        {
            // Supondo que o adapter tenha um método ConnectAsync
            await _clientNetworkService.ConnectAsync(serverAddress, port, timeoutMs);
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
