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
    public NetworkOptions Options { get; } = config;
    public INetworkEventBus EventBus { get; } = eventBus;
    public IPacketSender PacketSender { get; } = packetSender;
    public IConnectionManager ConnectionManager { get; } = connectionManager;
    public IPacketRegistry PacketRegistry { get;} = packetRegistry;
    
    private bool _shouldStayConnected;
    private CancellationTokenSource? _reconnectCts;
    
    public void Initialize()
    {
        // Se inscreve no evento de desconexão para disparar a lógica de reconexão
        EventBus.Subscribe<DisconnectionEvent>(OnDisconnected);
    }

    public async Task<ConnectionResult> ConnectAsync()
    {
        CancelReconnectAttempt(); // Cancela qualquer tentativa anterior
        _shouldStayConnected = true; // Define a intenção de estar conectado
        
        return await networkService.ConnectAsync(Options.ServerAddress, Options.ServerPort, Options.ConnectDelayMs);
    }

    public bool TryConnect(out ConnectionResult result)
    {
        CancelReconnectAttempt();
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
        _shouldStayConnected = false; // Define a intenção de desconectar
        CancelReconnectAttempt();
        networkService.Disconnect();
    }

    public void Update()
    {
        networkService.Update();
    }

    public void Dispose()
    {
        CancelReconnectAttempt();
        // Se houver mais algo para limpar, adicione aqui
    }
    
    private void OnDisconnected(DisconnectionEvent e)
    {
        // Só tenta reconectar se a desconexão não foi intencional e a opção está ativa
        if (_shouldStayConnected && Options.AutoReconnect)
        {
            // Evita iniciar múltiplos loops de reconexão
            if (_reconnectCts == null || _reconnectCts.IsCancellationRequested)
            {
                logger.LogInformation("Conexão perdida. Iniciando reconexão automática para {address}:{port}...", Options.ServerAddress, Options.ServerPort);
                _reconnectCts = new CancellationTokenSource();
                _ = ReconnectLoopAsync(_reconnectCts.Token); // Inicia em segundo plano
            }
        }
    }
    
    private async Task ReconnectLoopAsync(CancellationToken token)
    {
        int attempt = 0;
        while (!token.IsCancellationRequested)
        {
            attempt++;
            // Lógica de backoff exponencial para não sobrecarregar o servidor
            var delay = Math.Min(Options.ReconnectInitialDelayMs * Math.Pow(2, attempt), Options.ReconnectMaxDelayMs);
            logger.LogInformation("Tentativa de reconexão {attempt}. Aguardando {delay}ms.", attempt, delay);
            
            try
            {
                await Task.Delay((int)delay, token);
            }
            catch (TaskCanceledException) { break; } // Sai se o cancelamento for solicitado

            if (token.IsCancellationRequested) break;

            var result = await networkService.ConnectAsync(Options.ServerAddress!, Options.ServerPort);
            if (result.Success)
            {
                logger.LogInformation("Reconexão bem-sucedida!");
                break; // Sucesso, sai do loop
            }
        }
    }
    
    private void CancelReconnectAttempt()
    {
        if (_reconnectCts != null)
        {
            _reconnectCts.Cancel();
            _reconnectCts.Dispose();
            _reconnectCts = null;
        }
    }
}
