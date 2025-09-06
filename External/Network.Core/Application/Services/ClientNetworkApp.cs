using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Network.Core.Application.Options;
using Network.Core.Application.Ports.Inbound;
using Network.Core.Application.Ports.Outbound;
using Network.Core.Domain.Enums;
using Network.Core.Domain.Events;
using Network.Core.Domain.Models;

namespace Network.Core.Application.Services;

public class ClientNetworkApp(
    NetworkOptions config,
    IClientNetworkService networkService,
    IPacketSender packetSender,
    IConnectionManager connectionManager,
    IPacketRegistry packetRegistry,
    INetworkEventBus eventBus,
    ILogger<ClientNetworkApp> logger)
    : IClientNetworkApp
{
    // Máquina de estados para controlar o fluxo da conexão
    private enum ConnectionStatus { Disconnected, Connecting, Connected, Reconnecting }
    
    public NetworkOptions Options { get; } = config;
    public INetworkEventBus EventBus { get; } = eventBus;
    public IPacketSender PacketSender { get; } = packetSender;
    public IConnectionManager ConnectionManager { get; } = connectionManager;
    public IPacketRegistry PacketRegistry { get;} = packetRegistry;
    
    // Estado da reconexão
    private ConnectionStatus _status = ConnectionStatus.Disconnected;
    private bool _shouldStayConnected;
    private float _reconnectTimer;
    private int _reconnectAttempts;
    
    public void Initialize()
    {
        // Se inscreve nos eventos de conexão/desconexão para atualizar o estado
        EventBus.Subscribe<ConnectionEvent>(OnConnected);
        EventBus.Subscribe<DisconnectionEvent>(OnDisconnected);
    }

    public async Task<ConnectionResult> ConnectAsync()
    {
        if (_status != ConnectionStatus.Disconnected)
        {
            return new ConnectionResult(false, "Já está conectado ou conectando.");
        }
        
        _shouldStayConnected = true;
        _status = ConnectionStatus.Connecting;
        
        var result = await networkService.ConnectAsync(Options.ServerAddress, Options.ServerPort, Options.ConnectDelayMs);

        // O evento OnConnected/OnDisconnected tratará a transição de estado final.
        if (!result.Success)
        {
            // Se a conexão async falhar imediatamente, inicia a reconexão.
            OnDisconnected(new DisconnectionEvent(-1, DisconnectReason.Timeout));
        }

        return result;
    }
    
    public bool TryConnect(out ConnectionResult result)
    {
        _shouldStayConnected = true;
        
        var success = networkService.TryConnect(Options.ServerAddress, Options.ServerPort, out result);
        if (!success)
        {
            // Se a conexão síncrona falhar, inicia a reconexão em background se aplicável
            OnDisconnected(new DisconnectionEvent(-1 , DisconnectReason.Timeout));
        }
        return success;
    }

    public void Disconnect()
    {
        _shouldStayConnected = false;
        _status = ConnectionStatus.Disconnected;
        _reconnectTimer = 0;
        _reconnectAttempts = 0;
        networkService.Disconnect();
    }

    /// <summary>
    /// O coração da lógica do cliente, chamado a cada frame pelo GameLoop.
    /// </summary>
    public void Update(float deltaTime)
    {
        // Sempre processa os eventos da rede, independentemente do estado.
        networkService.Update();

        // Se estivermos em modo de reconexão, processa a lógica.
        if (_status == ConnectionStatus.Reconnecting)
        {
            _reconnectTimer += deltaTime;

            // Calcula o tempo de espera para a próxima tentativa (backoff exponencial)
            var delay = Math.Min(Options.ReconnectInitialDelayMs / 1000f * Math.Pow(2, _reconnectAttempts), Options.ReconnectMaxDelayMs / 1000f);

            if (_reconnectTimer >= delay)
            {
                _reconnectTimer = 0f;
                _reconnectAttempts++;
                logger.LogInformation("Tentativa de reconexão {attempt}...", _reconnectAttempts);

                // Usa o método síncrono para tentar a conexão.
                // O resultado será recebido através dos eventos OnConnected/OnDisconnected.
                networkService.TryConnect(Options.ServerAddress, Options.ServerPort, out _);
            }
        }
    }
    
    private void OnConnected(ConnectionEvent e)
    {
        logger.LogInformation("Conexão estabelecida com o servidor!");
        _status = ConnectionStatus.Connected;
        _reconnectAttempts = 0;
        _reconnectTimer = 0;
    }
    
    private void OnDisconnected(DisconnectionEvent e)
    {
        // Garante que o estado anterior era conectado para evitar loops em falhas de conexão inicial
        var wasConnected = _status == ConnectionStatus.Connected;
        _status = ConnectionStatus.Disconnected;

        // Só tenta reconectar se a desconexão não foi intencional e a opção está ativa
        if (_shouldStayConnected && Options.AutoReconnect)
        {
            if (wasConnected)
                 logger.LogWarning("Conexão perdida com o servidor. Iniciando reconexão automática...");
            else
                logger.LogWarning("Falha ao conectar. Iniciando reconexão automática...");
            
            _status = ConnectionStatus.Reconnecting;
            _reconnectAttempts = 0;
            _reconnectTimer = 0f;
        }
    }
    
    public void Dispose()
    {
        // Limpa as inscrições dos eventos
        EventBus.Unsubscribe<ConnectionEvent>(OnConnected);
        EventBus.Unsubscribe<DisconnectionEvent>(OnDisconnected);
    }
}

