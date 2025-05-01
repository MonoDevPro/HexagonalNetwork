# Hexagonal Network Module for MMO (C#)

Este projeto implementa um módulo de networking para jogos MMO em C#, seguindo a arquitetura hexagonal (Ports & Adapters) e utilizando LiteNetLib como transporte. O objetivo é desacoplar o domínio das dependências externas, permitindo fácil extensão, testes e manutenção.

## Estrutura do Projeto

- **Core (Domínio + Ports):**
  - Interfaces e modelos de domínio (`INetworkConfiguration`, `IPacket`, `INetworkSerializable`, `INetworkService`, etc).
  - Contratos para registro, serialização e envio de pacotes.
  - Não possui dependências externas.

- **Adapters:**
  - `LiteNetLibAdapter`: Implementa as interfaces do Core usando LiteNetLib, isolando a dependência.
  - `SerializerAdapter`: Implementa serialização/deserialização desacoplada.

- **Application Layer:**
  - `ClientApp` e `ServerApp`: Orquestram o ciclo de vida de cliente e servidor, consumindo apenas interfaces do Core.

- **Infrastructure:**
  - Configuração de Dependency Injection (DI) centralizada em `Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs`.

- **Testes:**
  - Testes unitários e de integração em `NetworkHexagonal.Tests`.

## Estrutura de Pastas do Projeto

```
NetworkHexagonal.sln
README.md
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

- O Core contém apenas contratos, modelos e lógica de domínio.
- Adapters isolam dependências externas.
- Application Layer orquestra o ciclo de vida de client/server.
- Infrastructure centraliza DI e configurações.
- Testes cobrem domínio, aplicação e integração real dos adapters.

## Como Usar

### 1. Injeção de Dependências

No seu projeto de aplicação:

```csharp
using NetworkHexagonal.Infrastructure.DependencyInjection;
services.AddNetworkingModule();
```

### 2. Exemplo de Inicialização (Client)

```csharp
var client = serviceProvider.GetRequiredService<IClientNetworkService>();
client.Initialize();
await client.ConnectAsync();
client.Update(); // Chame periodicamente no loop principal
```

### 3. Exemplo de Inicialização (Server)

```csharp
var server = serviceProvider.GetRequiredService<IServerNetworkService>();
server.Initialize();
server.Start();
server.Update(); // Chame periodicamente no loop principal
```

### 4. Testes

Execute os testes com:

```sh
dotnet test
```

## Padrões e Boas Práticas

- O Core nunca referencia Adapters ou Infrastructure.
- Adapters isolam dependências externas (ex: LiteNetLib).
- Application Layer só consome interfaces do Core.
- DI centralizada em Infrastructure.
- Testes cobrem domínio, aplicação e integração real dos adapters.

## Extensões Futuras

- Pooling de objetos e buffers.
- Hot-reload de configurações.
- Métricas plugáveis (Prometheus/Grafana).
- Suporte a fallback de transporte (TCP/WebSockets).

## Dependências Externas

Este projeto utiliza o LiteNetLib como submódulo git, localizado em `External/LiteNetLib`.

### Como clonar com submódulos

Ao clonar o repositório, use:

```sh
git clone --recurse-submodules https://github.com/MonoDevPro/HexagonalNetwork.git
```

Se já clonou sem submódulos, inicialize e atualize com:

```sh
git submodule update --init --recursive
```

### Como atualizar o submódulo LiteNetLib

Para buscar as últimas alterações do LiteNetLib:

```sh
cd External/LiteNetLib
git checkout main
git pull origin main
cd ../..
```

Se necessário, faça commit da referência do submódulo no repositório principal:

```sh
git add External/LiteNetLib
git commit -m "chore: atualiza submódulo LiteNetLib"
git push
```

---

Para detalhes sobre as interfaces e exemplos de pacotes, consulte o código-fonte e os testes em `NetworkHexagonal.Tests`.
