using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Network.Adapters.LiteNet;
using Network.Adapters.Loop;
using Network.Adapters.Serialization;
using Network.Core.Application.Loop;
using Network.Core.Application.Options;
using Network.Core.Application.Ports.Inbound;
using Network.Core.Application.Ports.Outbound;
using Network.Core.Application.Services;

namespace Network.Adapters;

/// <summary>
/// Extensões para registro dos serviços de rede na injeção de dependências
/// </summary>
public static class ServiceRegistration
{
    public static IServiceCollection AddClientNetworking(this IServiceCollection services)
    {
        services.RegisterCommonServices();
        
        // Aplicações -> Portas de entrada
        services.AddSingleton<IClientNetworkApp, ClientNetworkApp>();
        
        // Serviços de rede // Portas de entrada
        services.AddSingleton<IClientNetworkService, LiteNetLibClientAdapter>();
        
        return services;
    }
    
    public static IServiceCollection AddServerNetworking(this IServiceCollection services)
    {
        services.RegisterCommonServices();
        
        // Aplicações -> Portas de entrada
        services.AddSingleton<IServerNetworkApp, ServerApp>();
        
        // Serviços de rede // Portas de entrada
        services.AddSingleton<IServerNetworkService, LiteNetLibServerAdapter>();
        
        return services;
    }
    
    public static IServiceCollection AddNetworkPerformanceMonitor(this IServiceCollection services)
    {
        services.TryAddSingleton<PerformanceMonitor>();
        
        return services;
    }
    
    public static IServiceCollection AddGameLoopIntegration(this IServiceCollection services)
    {
        services.TryAddSingleton<GameLoop>();
        
        services.AddSingleton<NetworkLoopAdapter>();
        services.AddSingleton<IOrderedInitializable>(sp => sp.GetRequiredService<NetworkLoopAdapter>());
        services.AddSingleton<IOrderedUpdatable>(sp => sp.GetRequiredService<NetworkLoopAdapter>());
        
        return services;
    }
    
    private static IServiceCollection RegisterCommonServices(this IServiceCollection services)
    {
        services.TryAddSingleton<INetworkEventBus, NetworkEventBus>();
        services.TryAddSingleton<INetworkSerializer, SerializerAdapter>();
        services.TryAddSingleton<LiteNetLibConnectionManagerAdapter>();
        services.TryAddSingleton<IConnectionManager>(sp => sp.GetRequiredService<LiteNetLibConnectionManagerAdapter>());
        services.TryAddSingleton<LiteNetLibPacketHandlerAdapter>();
        services.TryAddSingleton<IPacketSender, LiteNetLibPacketSenderAdapter>();
        services.TryAddSingleton<IPacketRegistry, LiteNetLibPacketRegistryAdapter>();
        
        return services;
    }
}