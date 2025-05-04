using Microsoft.Extensions.Logging;
using LiteNetLib;
using NetworkHexagonal.Core.Application.Ports.Outbound;
using NetworkHexagonal.Core.Domain.Models;
using NetworkHexagonal.Core.Application.Ports.Inbound;
using NetworkHexagonal.Core.Domain.Events.Network;

namespace NetworkHexagonal.Adapters.Outbound.Network
{
    /// <summary>
    /// Adaptador de servidor de rede usando LiteNetLib
    /// </summary>
    public class LiteNetLibServerAdapter : IServerNetworkService
    {
        private readonly NetManager _netManager;
        private readonly EventBasedNetListener _listener;
        private readonly ILogger<LiteNetLibServerAdapter> _logger;
        private readonly LiteNetLibPacketHandlerAdapter _packetHandler;
        private readonly LiteNetLibConnectionManagerAdapter _connectionManager;
        private readonly INetworkConfiguration _config;
        private readonly INetworkEventBus _eventBus;
        
        public LiteNetLibServerAdapter(
            INetworkConfiguration config,
            LiteNetLibPacketHandlerAdapter packetHandler,
            LiteNetLibConnectionManagerAdapter connectionManager,
            ILogger<LiteNetLibServerAdapter> logger,
            INetworkEventBus eventBus)
        {
            _logger = logger;
            _packetHandler = packetHandler;
            _connectionManager = connectionManager;
            _config = config;
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
        /// Inicia o servidor na porta especificada
        /// </summary>
        public bool Start(int port)
        {
            if (_netManager.IsRunning)
            {
                _logger.LogWarning("Servidor já está rodando");
                return false;
            }
            
            try
            {
                bool success = _netManager.Start(port);
                if (success)
                {
                    _logger.LogInformation("Servidor iniciado na porta {Port}", port);
                    return true;
                }
                
                _logger.LogError("Falha ao iniciar servidor na porta {Port}", port);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao iniciar servidor na porta {Port}", port);
                return false;
            }
        }
        
        /// <summary>
        /// Para o servidor e desconecta todos os clientes
        /// </summary>
        public void Stop()
        {
            if (_netManager.IsRunning)
            {
                foreach (var peer in _connectionManager.GetAllPeers())
                {
                    peer.Disconnect();
                }
                
                _netManager.Stop();
                _logger.LogInformation("Servidor parado");
            }
        }
        
        /// <summary>
        /// Desconecta um cliente específico
        /// </summary>
        public void DisconnectPeer(int peerId)
        {
            var peer = _connectionManager.GetPeer(peerId);
            if (peer != null)
            {
                peer.Disconnect();
                _connectionManager.UnregisterPeer(peerId);
                _logger.LogInformation("Peer {PeerId} desconectado pelo servidor", peerId);
            }
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
        /// Configura os manipuladores de evento
        /// </summary>
        private void SetupEventHandlers()
        {
            _listener.ConnectionRequestEvent += HandleConnectionRequest;
            
            _listener.PeerConnectedEvent += peer => {
                _connectionManager.RegisterPeer(peer.Id, peer);
                _logger.LogInformation("Peer conectado: {PeerId}", peer.Id);
                // Publica evento de conexão diretamente no barramento
                _eventBus.Publish(new ConnectionEvent(peer.Id));
            };
            
            _listener.PeerDisconnectedEvent += (peer, info) => {
                _connectionManager.UnregisterPeer(peer.Id);
                _logger.LogInformation("Peer desconectado: {PeerId}, Motivo: {Reason}", 
                    peer.Id, info.Reason);
                // Publica evento de desconexão diretamente no barramento
                _eventBus.Publish(new DisconnectionEvent(peer.Id, 
                    MapDisconnectReasonToDomain(info.Reason)));
            };
            
            _listener.NetworkErrorEvent += (endPoint, error) => {
                _logger.LogError("Erro de rede: {Error} de {EndPoint}", error, endPoint);
                // Publica erro de rede diretamente no barramento
                _eventBus.Publish(new NetworkErrorEvent($"Erro de rede: {error} de {endPoint}"));
            };
            
            _listener.NetworkReceiveEvent += _packetHandler.HandleNetworkReceive;
        }
        
        /// <summary>
        /// Processa solicitações de conexão
        /// </summary>
        private void HandleConnectionRequest(ConnectionRequest request)
        {
            try
            {
                var requestInfo = new ConnectionRequestInfo(
                    request.RemoteEndPoint.ToString(), 
                    request.Data.GetString() ?? _config.ConnectionKey);
                
                var eventArgs = new ConnectionRequestEventArgs(requestInfo);
                // Publica evento de solicitação de conexão diretamente no barramento
                _eventBus.Publish(new ConnectionRequestEvent(eventArgs));
                
                if (eventArgs.ShouldAccept)
                {
                    var peer = request.Accept();
                    _logger.LogInformation("Conexão aceita: {RemoteEndPoint}", request.RemoteEndPoint);
                }
                else
                {
                    request.Reject();
                    _logger.LogInformation("Conexão rejeitada: {RemoteEndPoint}", request.RemoteEndPoint);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar solicitação de conexão de {RemoteEndPoint}", 
                    request.RemoteEndPoint);
                request.Reject();
            }
        }
        
        /// <summary>
        /// Mapeia as razões de desconexão do LiteNetLib para o nosso domínio
        /// </summary>
        private Core.Domain.Enums.DisconnectReason MapDisconnectReasonToDomain(LiteNetLib.DisconnectReason reason)
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
}