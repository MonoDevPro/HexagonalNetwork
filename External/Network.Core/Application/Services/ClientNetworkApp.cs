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
    private enum ConnectionStatus { Disconnected, Connecting, Connected, Reconnecting }
    
    public NetworkOptions Options { get; } = config;
    public INetworkEventBus EventBus { get; } = eventBus;
    public IPacketSender PacketSender { get; } = packetSender;
    public IConnectionManager ConnectionManager { get; } = connectionManager;
    public IPacketRegistry PacketRegistry { get;} = packetRegistry;

    private ConnectionStatus _status = ConnectionStatus.Disconnected;
    private bool _shouldStayConnected;
    private float _reconnectTimer;
    private int _reconnectAttempts;

    private TaskCompletionSource<ConnectionResult>? _connectionTcs;

    public void Initialize()
    {
        EventBus.Subscribe<ConnectionEvent>(OnConnected);
        EventBus.Subscribe<DisconnectionEvent>(OnDisconnected);
    }

    public Task<ConnectionResult> ConnectAsync()
    {
        if (_status != ConnectionStatus.Disconnected)
        {
            return Task.FromResult(new ConnectionResult(false, "Já está conectado ou conectando."));
        }

        _shouldStayConnected = true;
        _status = ConnectionStatus.Connecting;
        _connectionTcs = new TaskCompletionSource<ConnectionResult>();

        // Tenta iniciar a conexão de forma síncrona
        if (!networkService.TryConnect(Options.ServerAddress, Options.ServerPort, out var initialResult))
        {
            // Se a falha for imediata, já resolve a Task
            OnDisconnected(new DisconnectionEvent(-1, DisconnectReason.Rejected)); // Simula uma desconexão
            _connectionTcs.TrySetResult(initialResult);
        }
        else
        {
            // Se a tentativa foi iniciada, aguarda o timeout ou os eventos de rede
            StartTimeoutTask(Options.ConnectDelayMs);
        }

        return _connectionTcs.Task;
    }

    public bool TryConnect(out ConnectionResult result)
    {
        _shouldStayConnected = true;
        return networkService.TryConnect(Options.ServerAddress, Options.ServerPort, out result);
    }

    public void Disconnect()
    {
        _shouldStayConnected = false;
        networkService.Disconnect();
    }

    public void Update(float deltaTime)
    {
        networkService.Update();

        if (_status == ConnectionStatus.Reconnecting)
        {
            _reconnectTimer += deltaTime;
            var delay = Math.Min(Options.ReconnectInitialDelayMs / 1000f * Math.Pow(2, _reconnectAttempts), Options.ReconnectMaxDelayMs / 1000f);

            if (_reconnectTimer >= delay)
            {
                _reconnectTimer = 0f;
                _reconnectAttempts++;
                logger.LogInformation("Tentativa de reconexão {attempt}...", _reconnectAttempts);
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
        _connectionTcs?.TrySetResult(new ConnectionResult(true, string.Empty));
    }

    private void OnDisconnected(DisconnectionEvent e)
    {
        var wasConnected = _status == ConnectionStatus.Connected;
        var wasConnecting = _status == ConnectionStatus.Connecting;
        _status = ConnectionStatus.Disconnected;
        
        if(wasConnecting)
        {
            _connectionTcs?.TrySetResult(new ConnectionResult(false, e.Reason.ToString()));
        }

        if (_shouldStayConnected && Options.AutoReconnect)
        {
            logger.LogWarning(wasConnected ? "Conexão perdida. Iniciando reconexão..." : "Falha ao conectar. Iniciando reconexão...");
            _status = ConnectionStatus.Reconnecting;
            _reconnectAttempts = 0;
            _reconnectTimer = 0f;
        }
    }

    private async void StartTimeoutTask(int timeoutMs)
    {
        await Task.Delay(timeoutMs);
        // Se a Task ainda não foi completada (nem por sucesso, nem por falha), completa com timeout.
        if (_connectionTcs != null && !_connectionTcs.Task.IsCompleted)
        {
            logger.LogWarning("Timeout de conexão atingido.");
            // Força a desconexão para limpar o estado do adaptador
            networkService.Disconnect();
            // O evento de desconexão vai chamar OnDisconnected e resolver a Task
        }
    }

    public void Dispose()
    {
        EventBus.Unsubscribe<ConnectionEvent>(OnConnected);
        EventBus.Unsubscribe<DisconnectionEvent>(OnDisconnected);
    }
}