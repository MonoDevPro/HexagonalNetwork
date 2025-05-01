using Microsoft.Extensions.DependencyInjection;
using NetworkHexagonal.Core.Application.Ports.Input;
using NetworkHexagonal.Core.Application.Ports.Output;

namespace NetworkHexagonal.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNetworkingModule(this IServiceCollection services)
    {
        // Adapters
        services.AddSingleton<INetworkSerializer, Adapters.Outbound.Networking.Serializer.SerializerAdapter>();
        services.AddSingleton<IPacketRegistry, Adapters.Outbound.LiteNetLibAdapter.LiteNetLibAdapter>();
        services.AddSingleton<IPacketSender, Adapters.Outbound.LiteNetLibAdapter.LiteNetLibAdapter>();
        services.AddSingleton<INetworkService, Adapters.Outbound.LiteNetLibAdapter.LiteNetLibAdapter>();

        // Application Layer
        services.AddSingleton<IClientNetworkService, Core.Application.Services.ClientApp>();
        services.AddSingleton<IServerNetworkService, Core.Application.Services.ServerApp>();

        // Configuração de rede pode ser registrada como singleton ou via factory
        services.AddSingleton<INetworkConfiguration, Core.Application.Ports.Output.NetworkConfiguration>();

        return services;
    }
}