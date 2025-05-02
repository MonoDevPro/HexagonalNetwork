# Estrutura do Projeto Hexagonal Network

```
NetworkHexagonal.sln
README.md
External/
    LiteNetLib/   # Submódulo Git: https://github.com/MonoDevPro/LiteNetLib
Infrastructure/
    DependencyInjection/
        ServiceCollectionExtensions.cs
NetworkHexagonal/
    NetworkHexagonal.csproj
    PROJECT_STRUCTURE.md
    Adapters/
        Outbound/
            LiteNetLibAdapter/
                LiteNetLibAdapter.cs
            Networking/
                Serializer/
    Core/
        Application/
            Ports/
                INetworkReader.cs
                INetworkSerializable.cs
                INetworkWriter.cs
                IPacket.cs
                Input/
                Output/
            Services/
                ClientApp.cs
                ServerApp.cs
        Domain/
            Entities/
            Exceptions/
            ValueObjects/
    Infrastructure/
        DependencyInjection/
            ServiceCollectionExtensions.cs
    Shared/
        Kernel/
        Utils/
NetworkHexagonal.Tests/
    NetworkHexagonal.Tests.csproj
    AdaptersTests/
        AdaptersTests.cs
    CoreTests/
        CoreTests.cs
```

## Submódulo Externo: LiteNetLib

- O projeto utiliza o LiteNetLib como submódulo Git em `External/LiteNetLib`.
- Para clonar o repositório com o submódulo, use:

```sh
git clone --recurse-submodules https://github.com/MonoDevPro/HexagonalNetwork.git
```

- Se já clonou sem submódulos, inicialize e atualize com:

```sh
git submodule update --init --recursive
```

- Para atualizar o submódulo para a última versão:

```sh
cd External/LiteNetLib
git checkout main
git pull origin main
cd ../..
git add External/LiteNetLib
git commit -m "chore: atualiza submódulo LiteNetLib"
git push
```

## Observações
- O Core não referencia Adapters ou Infrastructure.
- Adapters isolam dependências externas.
- Application Layer orquestra o ciclo de vida de client/server.
- Infrastructure centraliza DI e configurações.
- Testes cobrem domínio, aplicação e integração real dos adapters.

## Como usar

1. **Ports ➔ Adapters**  
   Cada interface em `Core/Application/Ports` deve ter correspondência em `Adapters/Inbound` ou `Adapters/Outbound`.  
2. **DI / Composition Root**  
   No seu projeto de inicialização (ex: `Startup.cs` ou similar), faça o mapeamento:  
   ```csharp
   services.AddSingleton<INetworkConfiguration, NetworkConfiguration>();
   services.AddTransient<IPacketRegistry, LiteNetLibPacketRegistry>();
   services.AddScoped<INetworkSerializer, LiteNetLibSerializer>();
   services.AddScoped<IPacketSender, LiteNetLibSender>();
   services.AddScoped<IClientNetworkService, ClientNetworkService>();
   services.AddScoped<IServerNetworkService, ServerNetworkService>();
   ```  
3. **Manutenção**  
   - Mantenha o Core livre de referências a LiteNetLib.  
   - Se precisar trocar de biblioteca, implemente novos Adapters sem alterar `src/Core`.  
4. **Documentação e Automação**  
   - Atualize este arquivo sempre que criar novas Ports ou Adapters.  
   - Ferramentas de CI/agent podem ler este documento para validar convenções.

---

> **Dica:** personalize caminhos e nomes de namespaces conforme sua convenção de naming.