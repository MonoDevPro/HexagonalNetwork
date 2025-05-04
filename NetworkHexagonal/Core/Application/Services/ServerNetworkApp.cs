using NetworkHexagonal.Core.Application.Ports.Inbound;
using NetworkHexagonal.Core.Application.Ports.Outbound;
using NetworkHexagonal.Core.Domain.Events;

namespace NetworkHexagonal.Core.Application.Services;

public class ServerApp : IServerNetworkApp
{
    private readonly IServerNetworkService _serverNetworkService;
    private readonly INetworkEventBus _eventBus;

    public IPacketSender PacketSender { get; }
    public IPacketRegistry PacketRegistry { get;}
    public INetworkConfiguration Configuration { get; }

    public ServerApp(
        IServerNetworkService networkService,
        IPacketSender packetSender,
        IPacketRegistry packetRegistry,
        INetworkConfiguration config,
        INetworkEventBus eventBus)
    {
        _serverNetworkService = networkService;
        _eventBus = eventBus;
        PacketSender = packetSender;
        PacketRegistry = packetRegistry;
        Configuration = config;
    }

    public void Initialize()
    {
        // Registrar pacotes padrão se necessário
    }

    public void Dispose()
    {
        // Desregistrar pacotes se necessário
    }

    public bool Start(int port)
    {
        return _serverNetworkService.Start(port);
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
