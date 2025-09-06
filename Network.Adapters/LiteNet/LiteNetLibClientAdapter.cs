using LiteNetLib;
using Microsoft.Extensions.Logging;
using Network.Core.Application.Ports.Inbound;
using Network.Core.Application.Ports.Outbound;
using Network.Core.Domain.Events;
using Network.Core.Domain.Models;

namespace Network.Adapters.LiteNet;

/// <summary>
/// Adaptador de cliente de rede usando LiteNetLib
/// </summary>
public class LiteNetLibClientAdapter : IClientNetworkService
{
    private readonly NetManager _netManager;
    private readonly EventBasedNetListener _listener;
    private readonly ILogger<LiteNetLibClientAdapter> _logger;
    private readonly LiteNetLibPacketHandlerAdapter _packetHandler;
    private readonly INetworkConfiguration _config;
    private readonly LiteNetLibConnectionManagerAdapter _connectionManager;
    private readonly INetworkEventBus _eventBus;
    private NetPeer? _serverPeer;
    private TaskCompletionSource<ConnectionResult>? _connectionTcs;
        
    public LiteNetLibClientAdapter(
        INetworkConfiguration config, 
        LiteNetLibPacketHandlerAdapter packetHandler,
        LiteNetLibConnectionManagerAdapter connectionManager,
        ILogger<LiteNetLibClientAdapter> logger,
        INetworkEventBus eventBus)
    {
        _logger = logger;
        _packetHandler = packetHandler;
        _config = config;
        _connectionManager = connectionManager;
        _eventBus = eventBus;
            
        _listener = new EventBasedNetListener();
        _netManager = new NetManager(_listener)
        {
            UpdateTime = config.UpdateIntervalMs,
            DisconnectTimeout = config.DisconnectTimeoutMs,
            UnsyncedEvents = config.UseUnsyncedEvents // Usa a configuração para determinar o modo de eventos
        };
            
        _packetHandler.OnError += error => _eventBus.Publish(error); // Publica erro diretamente no barramento
            
        SetupEventHandlers();
    }
        
    /// <summary>
    /// Conecta a um servidor
    /// </summary>
    public Task<ConnectionResult> ConnectAsync(string serverAddress, int port, int timeoutMs = 5000)
    {
        if (_netManager.IsRunning)
        {
            return Task.FromResult(new ConnectionResult(false, "Cliente já está rodando"));
        }
            
        try
        {
            _connectionTcs = new TaskCompletionSource<ConnectionResult>();
                
            _netManager.Start();
                
            // Usa a chave de conexão da configuração em vez de uma string literal
            _serverPeer = _netManager.Connect(serverAddress, port, _config.ConnectionKey);
                
            // Inicia tarefa de timeout
            _ = StartTimeoutTask(timeoutMs);
                
            return _connectionTcs.Task;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao conectar ao servidor");
            return Task.FromResult(new ConnectionResult(false, ex.Message));
        }
    }
        
    /// <summary>
    /// Desconecta do servidor
    /// </summary>
    public void Disconnect()
    {
        if (_serverPeer != null)
        {
            _serverPeer.Disconnect();
            _connectionManager.UnregisterPeer(0); // Desregistra o servidor ao desconectar
            _serverPeer = null;
        }
            
        if (_netManager.IsRunning)
        {
            _netManager.Stop();
        }
            
        _logger.LogInformation("Cliente desconectado do servidor");
    }
        
    /// <summary>
    /// Atualiza o estado da rede
    /// </summary>
    public void Update()
    {
        if (_netManager.IsRunning)
        {
            _netManager.PollEvents();
        }
    }
        
    /// <summary>
    /// Inicia uma tarefa para controlar o timeout da conexão
    /// </summary>
    private async Task StartTimeoutTask(int timeoutMs)
    {
        await Task.Delay(timeoutMs);
        _connectionTcs?.TrySetResult(new ConnectionResult(false, "Timeout de conexão"));
    }
        
    /// <summary>
    /// Configura os manipuladores de evento
    /// </summary>
    private void SetupEventHandlers()
    {
        _listener.PeerConnectedEvent += peer => {
            _logger.LogInformation("Conectado ao servidor: {PeerId}", peer.Id);
            _connectionTcs?.TrySetResult(new ConnectionResult(true, string.Empty)); // Corrigido: string vazia em vez de null
                
            // Registra o peer do servidor com ID 0 para o cliente poder referenciá-lo facilmente
            _connectionManager.RegisterPeer(0, peer);
            _serverPeer = peer;
                
            // Publica evento de conexão diretamente no barramento
            _eventBus.Publish(new ConnectionEvent(peer.Id));
        };
            
        _listener.PeerDisconnectedEvent += (peer, info) => {
            _logger.LogInformation("Desconectado do servidor: {PeerId}, Motivo: {Reason}", 
                peer.Id, info.Reason);
                
            // Remove o peer do servidor do registro ao desconectar
            _connectionManager.UnregisterPeer(0);
            _serverPeer = null;
                
            // Publica evento de desconexão diretamente no barramento
            _eventBus.Publish(new DisconnectionEvent(peer.Id, 
                MapDisconnectReasonToDomain(info.Reason)));
        };
            
        _listener.NetworkErrorEvent += (endPoint, error) => {
            _logger.LogError("Erro de rede: {Error} de {EndPoint}", error, endPoint);
            // Publica erro de rede diretamente no barramento
            _eventBus.Publish(new NetworkErrorEvent($"Erro de rede: {error} de {endPoint}"));
        };
            
        _listener.NetworkLatencyUpdateEvent += _connectionManager.OnConnectionLatencyEvent;
            
        _listener.NetworkReceiveEvent += _packetHandler.HandleNetworkReceive;
    }
        
    /// <summary>
    /// Mapeia as razões de desconexão do LiteNetLib para o nosso domínio
    /// </summary>
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