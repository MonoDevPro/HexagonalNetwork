using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Network.Adapters;
using Network.Core.Application.Loop;
using Network.Core.Application.Options;
using Network.Core.Application.Ports.Inbound;
using Network.Core.Application.Ports.Outbound;
using NetworkExample.Console.Packets;

namespace NetworkExample.Console;

public class Program
{
    static async Task Main(string[] args)
    {
        System.Console.WriteLine("NetworkHexagonal - Demo de Chat");
        System.Console.WriteLine("-----------------------------");

        if (args.Length == 0 || args[0] == "-h" || args[0] == "--help" || (args[0] != "server" && args[0] != "client"))
        {
            System.Console.WriteLine("Uso: dotnet run -- [server|client]");
            return;
        }

        string mode = args[0].ToLower();
        string username = "Player"; // Default
        
        if (mode == "client")
        {
            System.Console.Write("Digite seu nome de usuário: ");
            username = System.Console.ReadLine() ?? "Player";
        }
        
        // --- Configuração da Injeção de Dependência ---
        
        var services = new ServiceCollection();

        IConfiguration configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddEnvironmentVariables()
            .AddCommandLine(args)
            .Build();

        services.AddSingleton(configuration);

        services.Configure<NetworkOptions>(configuration.GetSection(NetworkOptions.SectionName));
        services.AddSingleton<NetworkOptions>(sp => sp.GetRequiredService<IOptions<NetworkOptions>>().Value);
        
        services.Configure<LoopOptions>(configuration.GetSection(LoopOptions.SectionName));
        services.AddSingleton<LoopOptions>(sp => sp.GetRequiredService<IOptions<LoopOptions>>().Value);

        services.AddLogging(builder =>
        {
            builder.AddConfiguration(configuration.GetSection("Logging"));
            builder.AddConsole();
        });

        if (mode == "server")
            services.AddServerNetworking();
        else if (mode == "client")
            services.AddClientNetworking();
        
        services.AddGameLoopIntegration()
            .AddGameService<NetworkLoopAdapter>(); 

        services.AddSingleton(sp => new ConsoleChatManager(
            mode,
            username,
            sp.GetRequiredService<IPacketSender>(),
            sp.GetRequiredService<INetworkEventBus>()
        ));
        services.AddSingleton<IInitializable>(sp => sp.GetRequiredService<ConsoleChatManager>());
        services.AddSingleton<IUpdatable>(sp => sp.GetRequiredService<ConsoleChatManager>());
        
        await using var serviceProvider = services.BuildServiceProvider();
        
        // --- Execução da Aplicação ---
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
        var packetRegistry = serviceProvider.GetRequiredService<IPacketRegistry>();
        var gameLoop = serviceProvider.GetRequiredService<GameLoop>();
        
        var netOptions = serviceProvider.GetRequiredService<NetworkOptions>();
        logger.LogInformation($"Configurações de Rede: {netOptions.ToString()}");

        // Registra manipuladores de pacotes
        packetRegistry.RegisterHandler<ChatMessage>((message, context) => {
            System.Console.ForegroundColor = ConsoleColor.Green;
            System.Console.WriteLine($"{message.Sender}: {message.Message}");
            System.Console.ResetColor();
        });
        
        // Configura o cancelamento para Ctrl+C
        var cts = new CancellationTokenSource();
        System.Console.CancelKeyPress += (sender, e) =>
        {
            logger.LogInformation("Ctrl+C pressionado. Encerrando...");
            e.Cancel = true; // Impede que o processo termine abruptamente
            cts.Cancel();
        };

        try
        {
            logger.LogInformation("Aplicação iniciada no modo '{Mode}'. Pressione Ctrl+C para sair.", mode);
            // Executa o loop principal da aplicação
            await gameLoop.RunAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Esperado quando o cancelamento é solicitado.
            logger.LogInformation("Loop da aplicação cancelado com sucesso.");
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Ocorreu um erro fatal na aplicação.");
        }
        finally
        {
            logger.LogInformation("Aplicação encerrada.");
        }
    }
}