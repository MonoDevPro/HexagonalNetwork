using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Network.Core.Application.Ports.Inbound;
using Network.Core.Application.Ports.Outbound;
using Network.Core.Domain.Events;
using Network.Core.Domain.Models;
using NetworkHexagonal.Infrastructure.DependencyInjection;

namespace NetworkTests.Console;

public class Program
{
    // Exemplo de pacote para comunicação cliente-servidor
    public class ChatMessage : IPacket, ISerializable
    {
        public string Sender { get; set; }
        public string Message { get; set; }
            
        public void Serialize(INetworkWriter writer)
        {
            writer.WriteString(Sender);
            writer.WriteString(Message);
        }
            
        public void Deserialize(INetworkReader reader)
        {
            Sender = reader.ReadString();
            Message = reader.ReadString();
        }
    }

    public class NetworkConfiguration : INetworkConfiguration
    {
        public int UpdateIntervalMs { get; set; } = 15;
        public int DisconnectTimeoutMs { get; set; } = 5000;
        public string ConnectionKey { get; set; } = "default_key";
        public bool UseUnsyncedEvents { get; } = true;
    }
        
    static async Task Main(string[] args)
    {
        System.Console.WriteLine("NetworkHexagonal - Demo de Chat");
        System.Console.WriteLine("-----------------------------");
            
        var services = new ServiceCollection();
            
        // Configura logging
        services.AddLogging(configure => configure.AddConsole());

        // Registra configuração de rede
        services.AddSingleton<INetworkConfiguration, NetworkConfiguration>();
            
        // Registra serviços de rede
        services.AddNetworking();
            
        // Constrói o provedor de serviços
        var serviceProvider = services.BuildServiceProvider();
            
        // Obtém logger para o programa principal
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
            
        if (args.Length == 0 || args[0] == "-h" || args[0] == "--help")
        {
            System.Console.WriteLine("Uso: dotnet run -- [server|client] [porta] [endereço]");
            System.Console.WriteLine("  server    - Inicia em modo servidor");
            System.Console.WriteLine("  client    - Inicia em modo cliente");
            System.Console.WriteLine("  porta     - Porta para servidor ou conexão (padrão: 9050)");
            System.Console.WriteLine("  endereço  - Endereço para conexão (apenas cliente, padrão: localhost)");
            return;
        }
            
        string mode = args[0].ToLower();
        int port = args.Length > 1 ? int.Parse(args[1]) : 9050;
        string address = args.Length > 2 ? args[2] : "localhost";
            
        // Obtém serviços de rede
        var packetRegistry = serviceProvider.GetRequiredService<IPacketRegistry>();
            
        // Registra manipuladores de pacotes
        packetRegistry.RegisterHandler<ChatMessage>((message, context) => {
            System.Console.ForegroundColor = ConsoleColor.Green;
            System.Console.WriteLine($"{message.Sender}: {message.Message}");
            System.Console.ResetColor();
        });
            
        // Inicia modo selecionado
        if (mode == "server")
        {
            await RunServer(serviceProvider, port);
        }
        else if (mode == "client")
        {
            await RunClient(serviceProvider, address, port);
        }
        else
        {
            System.Console.WriteLine($"Modo inválido: {mode}");
            System.Console.WriteLine("Use 'server' ou 'client'");
        }
    }
        
    static async Task RunServer(IServiceProvider serviceProvider, int port)
    {
        var serverService = serviceProvider.GetRequiredService<IServerNetworkApp>();
        var packetSender = serviceProvider.GetRequiredService<IPacketSender>();
        var eventBus = serviceProvider.GetRequiredService<INetworkEventBus>();
            
        System.Console.WriteLine($"Iniciando servidor na porta {port}...");
        if (!serverService.Start(port))
        {
            System.Console.WriteLine($"Falha ao iniciar servidor na porta {port}");
            return;
        }
            
        System.Console.WriteLine("Servidor iniciado! Aguardando conexões...");
        System.Console.WriteLine("Pressione Ctrl+C para sair");
            
        // Armazena peers conectados
        var connectedPeers = new System.Collections.Generic.HashSet<int>();
            
        // Configura manipuladores de eventos

        eventBus.Subscribe<ConnectionEvent>(e => {
            connectedPeers.Add(e.PeerId);
            System.Console.WriteLine($"Cliente conectado: {e.PeerId}");

            // Envia mensagem de boas-vindas
            packetSender.SendPacket(e.PeerId, new ChatMessage { 
                Sender = "Servidor", 
                Message = "Bem-vindo ao chat!" 
            });

            // Notifica outros clientes
            foreach (var peerId in connectedPeers)
            {
                if (peerId != e.PeerId)
                {
                    packetSender.SendPacket(peerId, new ChatMessage { 
                        Sender = "Servidor", 
                        Message = $"Usuário {e.PeerId} entrou no chat" 
                    });
                }
            }
        });

        eventBus.Subscribe<DisconnectionEvent>(e => {
            connectedPeers.Remove(e.PeerId);
            System.Console.WriteLine($"Cliente desconectado: {e.PeerId}");
                
            // Notifica outros clientes
            foreach (var peerId in connectedPeers)
            {
                packetSender.SendPacket(peerId, new ChatMessage { 
                    Sender = "Servidor", 
                    Message = $"Usuário {e.PeerId} saiu do chat" 
                });
            }
        });
            
        // Executa atualizações de rede em loop
        var cancellationTokenSource = new CancellationTokenSource();
        System.Console.CancelKeyPress += (sender, e) => {
            e.Cancel = true;
            cancellationTokenSource.Cancel();
        };
            
        // Thread para ler entrada do servidor e enviar para todos os clientes
        _ = Task.Run(() => {
            while (!cancellationTokenSource.Token.IsCancellationRequested)
            {
                string input = System.Console.ReadLine();
                    
                if (!string.IsNullOrEmpty(input))
                {
                    var message = new ChatMessage { 
                        Sender = "Servidor", 
                        Message = input 
                    };
                        
                    foreach (var peerId in connectedPeers)
                    {
                        packetSender.SendPacket(peerId, message);
                    }
                        
                    // Exibe também no console do servidor
                    System.Console.ForegroundColor = ConsoleColor.Yellow;
                    System.Console.WriteLine($"Servidor: {input}");
                    System.Console.ResetColor();
                }
            }
        });
            
        // Loop principal para atualizações de rede
        while (!cancellationTokenSource.Token.IsCancellationRequested)
        {
            serverService.Update();
            await Task.Delay(15);
        }
            
        System.Console.WriteLine("Encerrando servidor...");
        serverService.Stop();
    }
        
    static async Task RunClient(IServiceProvider serviceProvider, string address, int port)
    {
        var clientService = serviceProvider.GetRequiredService<IClientNetworkApp>();
        var packetSender = serviceProvider.GetRequiredService<IPacketSender>();
        var eventBus = serviceProvider.GetRequiredService<INetworkEventBus>();
            
        System.Console.WriteLine($"Conectando ao servidor {address}:{port}...");
            
        var result = await clientService.ConnectAsync(address, port);
        if (!result.Success)
        {
            System.Console.WriteLine($"Falha ao conectar: {result.ErrorMessage}");
            return;
        }
            
        System.Console.WriteLine("Conectado ao servidor!");
        System.Console.WriteLine("Digite mensagens para enviar ou 'exit' para sair");
            
        // Executa atualizações de rede em loop
        var cancellationTokenSource = new CancellationTokenSource();
        System.Console.CancelKeyPress += (sender, e) => {
            e.Cancel = true;
            cancellationTokenSource.Cancel();
        };

        // Configura manipuladores de eventos
        eventBus.Subscribe<DisconnectionEvent>(e => {
            System.Console.WriteLine($"Desconectado do servidor: {e.Reason}");
            cancellationTokenSource.Cancel();
        });
            
        // Thread para ler entrada do usuário e enviar para o servidor
        _ = Task.Run(() => {
            // Solicita nome de usuário
            System.Console.Write("Digite seu nome de usuário: ");
            string username = System.Console.ReadLine();
                
            while (!cancellationTokenSource.Token.IsCancellationRequested)
            {
                string input = System.Console.ReadLine();
                    
                if (input?.ToLower() == "exit")
                {
                    cancellationTokenSource.Cancel();
                    continue;
                }
                    
                if (!string.IsNullOrEmpty(input))
                {
                    var message = new ChatMessage { 
                        Sender = username, 
                        Message = input 
                    };
                        
                    packetSender.SendPacket(0, message); // 0 é o ID do servidor
                }
            }
        });
            
        // Loop principal para atualizações de rede
        while (!cancellationTokenSource.Token.IsCancellationRequested)
        {
            clientService.Update();
            await Task.Delay(15);
        }
            
        System.Console.WriteLine("Desconectando...");
        clientService.Disconnect();
    }
}