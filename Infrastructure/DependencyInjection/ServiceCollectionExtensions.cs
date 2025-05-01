using Microsoft.Extensions.DependencyInjection;

namespace NetworkHexagonal.Infrastructure.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddNetworkingModule(this IServiceCollection services)
        {
            // ...existing code...
            //
            // Arquivo de configuração de Dependency Injection (DI) do módulo de networking hexagonal.
            //
            // Padrão recomendado: mantenha a configuração de DI em Infrastructure/DependencyInjection para separar
            // infraestrutura do domínio e adapters, facilitando manutenção, testes e extensão.
            //
            // Como usar:
            //
            // 1. No seu projeto de aplicação (ex: ASP.NET, Worker, Console):
            //
            //    using NetworkHexagonal.Infrastructure.DependencyInjection;
            //    services.AddNetworkingModule();
            //
            // 2. Isso registra todos os serviços, adapters e contratos necessários para Client e Server.
            //
            // 3. Consulte a documentação do projeto para exemplos de configuração e inicialização.
            //
            //
            // Se desejar customizar, adicione factories, configurações ou extensões aqui.
            //
            //
            // Localização correta: Infrastructure/DependencyInjection
            //
            // Motivo: Segue o padrão hexagonal, não polui o Core nem Adapters, e centraliza a infraestrutura.
            // ...existing code...

            return services;
        }
    }
}