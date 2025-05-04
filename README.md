# Hexagonal Network Module for MMO (C#)

Este projeto implementa um módulo de networking para jogos MMO em C#, seguindo a arquitetura hexagonal (Ports & Adapters) e utilizando LiteNetLib como transporte. O objetivo é desacoplar o domínio das dependências externas, permitindo fácil extensão, testes e manutenção.

## Arquitetura Hexagonal

- **Core (Domínio + Ports):**
  - Interfaces e modelos de domínio (ex: `INetworkConfiguration`, `IPacket`, `INetworkSerializable`, `IPacketRegistry`, `IClientNetworkApp`, `IServerNetworkApp`,
  `IPacketRegistry`, `IPacketSender`, etc).
  - Contratos para registro, serialização, recebimento e envio de pacotes.
  - Não possui dependências externas ou referências a LiteNetLib.

- **Adapters:**
  - Implementam as interfaces do Core usando LiteNetLib, isolando a dependência.
  - Toda lógica de serialização, envio/recebimento de pacotes e callbacks de rede ficam aqui.

- **Application Layer:**
  - `ClientNetworkApp` e `ServerNetworkApp`: Orquestram o ciclo de vida de cliente e servidor, consumindo apenas interfaces do Core.
  - Utilizam o `NetworkEventBus` para desacoplar a propagação de eventos de rede.

- **Infrastructure:**
  - Configuração de Dependency Injection (DI) centralizada em `Infrastructure/DependencyInjection/NetworkServiceRegistration.cs`.

- **Testes:**
  - Testes unitários e de integração em `NetworkTests/`.

## Estrutura de Pastas do Projeto

```
NetworkHexagonal.sln
README.md
NetworkHexagonal/
    Adapters/
        Outbound/
            Network/
                LiteNetLibServerAdapter.cs
                LiteNetLibClientAdapter.cs
                ...
        ...
    Core/
        Application/
            Ports/
                Inbound/
                Outbound/
            Services/
                ClientNetworkApp.cs
                ServerNetworkApp.cs
                NetworkEventBus.cs
        Domain/
            Events/
                Network/
                    ConnectionEvent.cs
                    DisconnectionEvent.cs
                    NetworkErrorEvent.cs
                    ConnectionRequestEvent.cs
            ...
    Infrastructure/
        DependencyInjection/
            NetworkServiceRegistration.cs
    Shared/
        Kernel/
        Utils/
NetworkTests/
    AdaptersTests/
    CoreTests/
    ...
```

- O Core contém apenas contratos, modelos e lógica de domínio.
- Adapters isolam dependências externas.
- Application Layer orquestra o ciclo de vida de client/server e eventos.
- Infrastructure centraliza DI e configurações.
- Testes cobrem domínio, aplicação e integração real dos adapters.

## NetworkEventBus

O `NetworkEventBus` é um barramento de eventos in-memory utilizado para desacoplar a propagação de eventos de rede (ex: conexões, desconexões, erros) entre adapters, application e domínio.

> **Atenção:**
> O `NetworkEventBus` **não deve ser utilizado para eventos de gameplay, processamento de pacotes em alta frequência ou qualquer fluxo que exija latência mínima e throughput máximo** (ex: movimentação de jogadores, ações em tempo real). Para esses casos, utilize mecanismos diretos e otimizados, evitando overhead de abstração e casting.

## Como Usar

### 1. Injeção de Dependências

No seu projeto de aplicação:

```csharp
using NetworkHexagonal.Infrastructure.DependencyInjection;
services.AddNetworkingModule();
```

### 2. Exemplo de Inicialização (Client)

```csharp
var clientApp = serviceProvider.GetRequiredService<ClientNetworkApp>();
await clientApp.ConnectAsync(serverAddress, port);
clientApp.Update(); // Chame periodicamente no loop principal
```

### 3. Exemplo de Inicialização (Server)

```csharp
var serverApp = serviceProvider.GetRequiredService<ServerNetworkApp>();
serverApp.Start(port);
serverApp.Update(); // Chame periodicamente no loop principal
```

### 4. Assinando eventos de rede

```csharp
// Exemplo: assinando eventos de conexão
networkEventBus.Subscribe<ConnectionEvent>(evt =>
{
    Console.WriteLine($"Peer conectado: {evt.PeerId}");
});
```

### 5. Testes

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

Para detalhes sobre as interfaces e exemplos de pacotes, consulte o código-fonte e os testes em `NetworkTests/`.
