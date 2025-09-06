using Microsoft.Extensions.DependencyInjection;
using Network.Core.Application.Loop;

namespace NetworkExample.Console;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGameService<T>(this IServiceCollection services) 
        where T : class, IInitializable, IUpdatable
    {
        services.AddSingleton<T>();
        services.AddSingleton<IInitializable>(sp => sp.GetRequiredService<T>());
        services.AddSingleton<IUpdatable>(sp => sp.GetRequiredService<T>());
        return services;
    }
}