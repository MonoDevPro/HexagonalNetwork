using NetworkHexagonal.Core.Application.Ports.Inbound;
using NetworkHexagonal.Core.Application.Ports.Outbound;
using NetworkHexagonal.Core.Domain.Events;
using NetworkHexagonal.Core.Domain.Events.Network;

namespace NetworkHexagonal.Core.Application.Services;

public class ServerApp : IServerNetworkApp
{
    private readonly IServerNetworkService _serverNetworkService;

    public INetworkEventBus EventBus { get; }
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
        EventBus = eventBus;
        PacketSender = packetSender;
        PacketRegistry = packetRegistry;
        Configuration = config;

        EventBus.Subscribe<ConnectionRequestEvent>(ProcessConnectionRequest);
    }

    private void ProcessConnectionRequest(ConnectionRequestEvent connectionRequest)
    {
        // Processar solicitação de conexão
        // Exemplo: Verificar se o cliente pode se conectar com X Ip e Y connectionKey
        // Se não puder, rejeitar a conexão

        var ip = connectionRequest.EventArgs.RequestInfo.RemoteEndPoint;
        var connectionKey = connectionRequest.EventArgs.RequestInfo.ConnectionKey;
        
        // Podemos facilmente processar uma blacklist de IPs ou verificar se o cliente já está conectado
        // Também podemos delegar isso para um serviço de autenticação.

        if (connectionKey == Configuration.ConnectionKey)
            // Aceitar a conexão
            connectionRequest.EventArgs.ShouldAccept = true;
        else
            // Rejeitar a conexão
            connectionRequest.EventArgs.ShouldAccept = false;
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
