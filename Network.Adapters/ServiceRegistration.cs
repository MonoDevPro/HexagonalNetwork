using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Network.Adapters.Distributed;
using Network.Adapters.LiteNet;
using Network.Adapters.Resilience;
using Network.Adapters.Security;
using Network.Adapters.Serialization;
using Network.Core.Application.Loop;
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
        services.RegisterSecurityServices();
        services.RegisterResilienceServices();
        
        // Aplicações -> Portas de entrada
        services.AddSingleton<IClientNetworkApp, ClientNetworkApp>();
        
        // Serviços de rede // Portas de entrada
        services.AddSingleton<IClientNetworkService, LiteNetLibClientAdapter>();
        
        return services;
    }
    
    public static IServiceCollection AddServerNetworking(this IServiceCollection services)
    {
        services.RegisterCommonServices();
        services.RegisterSecurityServices();
        services.RegisterResilienceServices();
        
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
        services.AddSingleton<GameLoop>();
        
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
    
    private static IServiceCollection RegisterSecurityServices(this IServiceCollection services)
    {
        services.TryAddSingleton<IPacketEncryption, AesPacketEncryptionAdapter>();
        services.TryAddSingleton<IAuthenticationService, JwtAuthenticationAdapter>();
        services.TryAddSingleton<IRateLimiter, MemoryRateLimiterAdapter>();
        
        return services;
    }
    
    private static IServiceCollection RegisterResilienceServices(this IServiceCollection services)
    {
        services.TryAddSingleton<ICircuitBreaker, CircuitBreakerAdapter>();
        services.TryAddSingleton<IHealthCheckService, HealthCheckAdapter>();
        
        return services;
    }
    
    public static IServiceCollection AddDistributedEventBus(this IServiceCollection services)
    {
        services.TryAddSingleton<IDistributedEventBus, RabbitMqDistributedEventBusAdapter>();
        
        return services;
    }
}