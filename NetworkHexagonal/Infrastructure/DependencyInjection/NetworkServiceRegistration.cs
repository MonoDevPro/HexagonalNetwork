using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Network.Adapters.LiteNet;
using Network.Adapters.Serialization;
using Network.Core.Application.Ports.Inbound;
using Network.Core.Application.Ports.Outbound;
using Network.Core.Application.Services;

namespace NetworkHexagonal.Infrastructure.DependencyInjection
{
    /// <summary>
    /// Extensões para registro dos serviços de rede na injeção de dependências
    /// </summary>
    public static class NetworkServiceRegistration
    {
        /// <summary>
        /// Adiciona os serviços de rede à coleção de serviços
        /// </summary>
        public static IServiceCollection AddNetworking(this IServiceCollection services)
        {
            // Configuração - usando TryAddSingleton para não sobrescrever se já existir (importante para testes)
            services.TryAddSingleton<INetworkConfiguration, NetworkConfiguration>();
            
            // Barramento de eventos
            services.AddSingleton<INetworkEventBus, NetworkEventBus>();

            // Aplicações -> Portas de entrada
            services.AddSingleton<IServerNetworkApp, ServerApp>();
            services.AddSingleton<IClientNetworkApp, ClientNetworkApp>();
            
            // Adaptadores
            services.AddSingleton<INetworkSerializer, SerializerAdapter>();
            services.AddSingleton<LiteNetLibConnectionManagerAdapter>();
            services.AddSingleton<IConnectionManager>(sp => sp.GetRequiredService<LiteNetLibConnectionManagerAdapter>());
            
            // Manipulador de pacotes
            services.AddSingleton<LiteNetLibPacketHandlerAdapter>();
            
            // Serviços de rede // Portas de entrada
            services.AddSingleton<IClientNetworkService, LiteNetLibClientAdapter>();
            services.AddSingleton<IServerNetworkService, LiteNetLibServerAdapter>();
            services.AddSingleton<IPacketSender, LiteNetLibPacketSenderAdapter>();
            services.AddSingleton<IPacketRegistry, LiteNetLibPacketRegistryAdapter>();
            
            return services;
        }
    }
    
    /// <summary>
    /// Configuração padrão para os serviços de rede
    /// </summary>
    public class NetworkConfiguration : INetworkConfiguration
    {
        public int UpdateIntervalMs { get; set; } = 15;
        public int DisconnectTimeoutMs { get; set; } = 5000;
        public string ConnectionKey { get; set; } = "ConnectionKey";
        public bool UseUnsyncedEvents { get; set; } = false; // Padrão: eventos processados apenas via Update()
    }
}