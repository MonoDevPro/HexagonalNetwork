using LiteNetLib;
using Microsoft.Extensions.Logging;
using Network.Core.Application.Options;
using Network.Core.Application.Ports.Inbound;
using Network.Core.Application.Ports.Outbound;
using Network.Core.Domain.Events;
using Network.Core.Domain.Models;

namespace Network.Adapters.LiteNet;

/// <summary>
/// Adaptador de cliente de rede usando LiteNetLib (versão síncrona simplificada)
/// </summary>
public class LiteNetLibClientAdapter : IClientNetworkService
{
    private readonly NetManager _netManager;
    private readonly EventBasedNetListener _listener;
    private readonly ILogger<LiteNetLibClientAdapter> _logger;
    private readonly LiteNetLibPacketHandlerAdapter _packetHandler;
    private readonly NetworkOptions _options;
    private readonly LiteNetLibConnectionManagerAdapter _connectionManager;
    private readonly INetworkEventBus _eventBus;
    private NetPeer? _serverPeer;

    public LiteNetLibClientAdapter(
        NetworkOptions options,
        LiteNetLibPacketHandlerAdapter packetHandler,
        LiteNetLibConnectionManagerAdapter connectionManager,
        ILogger<LiteNetLibClientAdapter> logger,
        INetworkEventBus eventBus)
    {
        _logger = logger;
        _packetHandler = packetHandler;
        _options = options;
        _connectionManager = connectionManager;
        _eventBus = eventBus;

        _listener = new EventBasedNetListener();
        _netManager = new NetManager(_listener)
        {
            UpdateTime = options.UpdateIntervalMs,
            DisconnectTimeout = options.DisconnectTimeoutMs,
            UnsyncedEvents = options.UseUnsyncedEvents
        };

        _packetHandler.OnError += error => _eventBus.Publish(error);
        SetupEventHandlers();
    }

    /// <summary>
    /// Tenta iniciar uma conexão com o servidor. O resultado será notificado via eventos.
    /// </summary>
    public bool TryConnect(string serverAddress, int port, out ConnectionResult result)
    {
        if (_netManager.IsRunning)
        {
            result = new ConnectionResult(false, "Cliente já está rodando ou conectando.");
            return false;
        }

        try
        {
            _netManager.Start();
            _serverPeer = _netManager.Connect(serverAddress, port, _options.ConnectionKey);

            if (_serverPeer == null)
            {
                // Falha imediata ao tentar conectar (ex: endereço inválido)
                _netManager.Stop();
                result = new ConnectionResult(false, "Falha ao iniciar conexão. Verifique o endereço e a porta.");
                return false;
            }

            result = new ConnectionResult(true, string.Empty);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exceção ao tentar conectar ao servidor.");
            result = new ConnectionResult(false, ex.Message);
            return false;
        }
    }

    public void Disconnect()
    {
        if (_serverPeer != null)
        {
            _serverPeer.Disconnect();
            // O evento de desconexão cuidará da limpeza
        }
        
        if (_netManager.IsRunning)
        {
            _netManager.Stop();
        }
        _logger.LogInformation("Cliente desconectado do servidor");
    }

    public void Update()
    {
        if (_netManager.IsRunning)
        {
            _netManager.PollEvents();
        }
    }

    private void SetupEventHandlers()
    {
        _listener.PeerConnectedEvent += peer => {
            _logger.LogInformation("Conectado ao servidor: {PeerId}", peer.Id);
            _connectionManager.RegisterPeer(0, peer);
            _serverPeer = peer;
            _eventBus.Publish(new ConnectionEvent(peer.Id));
        };

        _listener.PeerDisconnectedEvent += (peer, info) => {
            _logger.LogInformation("Desconectado do servidor: {PeerId}, Motivo: {Reason}",
                peer.Id, info.Reason);
            _connectionManager.UnregisterPeer(0);
            _serverPeer = null;

            // Se o NetManager ainda estiver rodando (desconexão inesperada), paramos para permitir uma nova tentativa de conexão limpa.
            if (_netManager.IsRunning)
            {
                _netManager.Stop();
            }

            _eventBus.Publish(new DisconnectionEvent(peer.Id,
                MapDisconnectReasonToDomain(info.Reason)));
        };

        _listener.NetworkErrorEvent += (endPoint, error) => {
            _logger.LogError("Erro de rede: {Error} de {EndPoint}", error, endPoint);
            _eventBus.Publish(new NetworkErrorEvent($"Erro de rede: {error} de {endPoint}"));
        };

        _listener.NetworkLatencyUpdateEvent += _connectionManager.OnConnectionLatencyEvent;
        _listener.NetworkReceiveEvent += _packetHandler.HandleNetworkReceive;
    }

    private Network.Core.Domain.Enums.DisconnectReason MapDisconnectReasonToDomain(LiteNetLib.DisconnectReason reason)
    {
        return reason switch
        {
            LiteNetLib.DisconnectReason.Timeout => Core.Domain.Enums.DisconnectReason.Timeout,
            LiteNetLib.DisconnectReason.ConnectionRejected => Core.Domain.Enums.DisconnectReason.Rejected,
            LiteNetLib.DisconnectReason.DisconnectPeerCalled => Core.Domain.Enums.DisconnectReason.ConnectionClosed,
            LiteNetLib.DisconnectReason.RemoteConnectionClose => Core.Domain.Enums.DisconnectReason.RemoteClose,
            _ => Core.Domain.Enums.DisconnectReason.Unknown
        };
    }
}