using NetworkHexagonal.Core.Application.Ports.Inbound;
using NetworkHexagonal.Core.Application.Ports.Outbound;
using NetworkHexagonal.Core.Domain.Models;

namespace NetworkHexagonal.Core.Application.Services;

public class ClientNetworkApp : IClientNetworkApp
{
    public INetworkEventBus EventBus { get; }
    public IPacketSender PacketSender { get; }
    public IConnectionManager ConnectionManager { get; }
    public IPacketRegistry PacketRegistry { get;}
    public INetworkConfiguration Configuration { get; }
    private readonly IClientNetworkService _clientNetworkService;

    public ClientNetworkApp(
        IClientNetworkService networkService,
        IPacketSender packetSender,
        IConnectionManager connectionManager,
        IPacketRegistry packetRegistry,
        INetworkConfiguration config,
        INetworkEventBus eventBus)
    {
        _clientNetworkService = networkService;
        EventBus = eventBus;
        PacketSender = packetSender;
        ConnectionManager = connectionManager;
        PacketRegistry = packetRegistry;
        Configuration = config;
        // Eventos do adapter removidos: publicação agora é feita diretamente no adapter via NetworkEventBus.
        // Nenhum registro de eventos necessário aqui.
    }

    public void Initialize()
    {
        // Registrar pacotes padrão se necessário
    }

    public void Dispose()
    {
        // Desregistrar pacotes se necessário
    }

    public async Task<ConnectionResult> ConnectAsync(string serverAddress, int port, int timeoutMs = 5000)
    {
        // Supondo que o adapter tenha um método ConnectAsync
        return await _clientNetworkService.ConnectAsync(serverAddress, port, timeoutMs);
    }

    public void Disconnect()
    {
        _clientNetworkService.Disconnect();
    }

    public void Update()
    {
        _clientNetworkService.Update();
    }
}
