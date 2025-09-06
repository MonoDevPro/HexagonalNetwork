using System.Collections.Concurrent;
using Network.Core.Application.Loop;
using Network.Core.Application.Ports.Inbound;
using Network.Core.Application.Ports.Outbound;
using Network.Core.Domain.Events;
using NetworkExample.Console.Packets;

namespace NetworkExample.Console;

/// <summary>
/// Gerencia a lógica de eventos e input do console para o chat,
/// integrando-se ao ciclo de vida do GameLoop.
/// </summary>
public class ConsoleChatManager : IOrderedInitializable, IOrderedUpdatable
{
    private readonly string _mode;
    private readonly IPacketSender _packetSender;
    private readonly INetworkEventBus _eventBus;
    
    // Estado interno do serviço
    private readonly ConcurrentQueue<string> _inputQueue = new();
    private readonly HashSet<int> _connectedPeers = new();
    private CancellationTokenSource _cts = new();

    // O input deve ser processado após as atualizações de rede
    public int Order => 100;

    private string _username;

    public ConsoleChatManager(string mode, string username, IPacketSender packetSender, INetworkEventBus eventBus)
    {
        _mode = mode.ToLower();
        _username = username;
        _packetSender = packetSender;
        _eventBus = eventBus;
    }


    public Task InitializeAsync(CancellationToken ct = default)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        if (_mode == "server")
        {
            _eventBus.Subscribe<ConnectionEvent>(HandleServerConnection);
            _eventBus.Subscribe<DisconnectionEvent>(HandleServerDisconnection);
        }
        else // client
        {
            _eventBus.Subscribe<DisconnectionEvent>(HandleClientDisconnection);
        }
        
        System.Console.WriteLine("Digite mensagens para enviar. Pressione Ctrl+C para sair.");
        // Inicia uma thread em segundo plano para ler o input sem bloquear o loop principal
        Task.Run(() => ReadInputLoop(_cts.Token), _cts.Token);
        
        return Task.CompletedTask;
    }

    public void Update(float deltaTime)
    {
        // A leitura de input agora acontece em outra thread.
        // O Update apenas processa o que já foi lido.
        while (_inputQueue.TryDequeue(out var input))
        {
            if (string.IsNullOrWhiteSpace(input)) continue;

            var message = new ChatMessage { Sender = _username, Message = input };

            if (_mode == "server")
            {
                // Mostra no console do servidor e transmite para os clientes
                System.Console.ForegroundColor = ConsoleColor.Yellow;
                System.Console.WriteLine($"Servidor: {input}");
                System.Console.ResetColor();

                foreach (var peerId in _connectedPeers)
                {
                    _packetSender.SendPacket(peerId, message);
                }
            }
            else // client
            {
                if (input.Equals("exit", StringComparison.OrdinalIgnoreCase))
                {
                    System.Console.WriteLine("Para sair, por favor use Ctrl+C.");
                    continue;
                }
                _packetSender.SendPacket(0, message); // 0 é o ID do servidor
            }
        }
    }
    
    #region Manipuladores de Eventos
    private void HandleServerConnection(ConnectionEvent e)
    {
        _connectedPeers.Add(e.PeerId);
        System.Console.WriteLine($"Cliente conectado: {e.PeerId}");

        _packetSender.SendPacket(e.PeerId, new ChatMessage { Sender = "Servidor", Message = "Bem-vindo ao chat!" });

        var notification = new ChatMessage { Sender = "Servidor", Message = $"Usuário {e.PeerId} entrou no chat" };
        foreach (var peerId in _connectedPeers.Where(id => id != e.PeerId))
        {
            _packetSender.SendPacket(peerId, notification);
        }
    }

    private void HandleServerDisconnection(DisconnectionEvent e)
    {
        _connectedPeers.Remove(e.PeerId);
        System.Console.WriteLine($"Cliente desconectado: {e.PeerId}");
            
        var notification = new ChatMessage { Sender = "Servidor", Message = $"Usuário {e.PeerId} saiu do chat" };
        foreach (var peerId in _connectedPeers)
        {
            _packetSender.SendPacket(peerId, notification);
        }
    }

    private void HandleClientDisconnection(DisconnectionEvent e)
    {
        System.Console.ForegroundColor = ConsoleColor.Red;
        System.Console.WriteLine($"Desconectado do servidor. Motivo: {e.Reason}. Tentando reconectar...");
        System.Console.ResetColor();
        // A lógica de reconexão será tratada pela ClientNetworkApp.
        // Não devemos cancelar o loop principal aqui.
    }
    #endregion

    // Este método agora roda em uma thread separada.
    private void ReadInputLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                // Esta chamada bloqueante agora não afeta o loop principal.
                var input = System.Console.ReadLine();
                if (input != null)
                {
                    _inputQueue.Enqueue(input);
                }
            }
            catch (IOException) { break; } // Console fechado
            catch (OperationCanceledException) { break; } // Esperado ao encerrar
        }
    }

    public Task StopAsync(CancellationToken ct = default)
    {
        _cts.Cancel();
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        _cts.Dispose();
        return ValueTask.CompletedTask;
    }
}

