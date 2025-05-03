# Estrutura do Projeto Hexagonal Network

## Visão Geral

Este projeto implementa um módulo de rede para aplicações distribuídas usando arquitetura hexagonal (ou ports and adapters), permitindo total desacoplamento entre a lógica de negócios e as implementações específicas de rede.

## Estrutura de Diretórios

```
NetworkHexagonal.sln
README.md
External/
    LiteNetLib/   # Submódulo Git: https://github.com/MonoDevPro/LiteNetLib
NetworkHexagonal/
    NetworkHexagonal.csproj
    PROJECT_STRUCTURE.md
    Adapters/
        Outbound/
            Network/
                LiteNetLibClientAdapter.cs
                LiteNetLibServerAdapter.cs
                LiteNetLibConnectionManagerAdapter.cs
                LiteNetLibPacketHandlerAdapter.cs
                LiteNetLibPacketRegistryAdapter.cs
                LiteNetLibPacketSenderAdapter.cs
            Serialization/
                NetworkReaderAdapter.cs
                NetworkWriterAdapter.cs
                LiteNetLibSerializerAdapter.cs
            Util/
                FastBitConverter.cs
                ObjectPool.cs
    Core/
        Application/
            Ports/
                Inbound/
                    IClientApp.cs
                    IServerApp.cs
                Outbound/
                    INetworkReader.cs
                    INetworkWriter.cs
                    INetworkSerializer.cs
                    INetworkServices.cs
            Services/
                ClientApp.cs
                ServerApp.cs
                NetworkEventBus.cs
        Domain/
            Enums/
                DeliveryMode.cs
                DisconnectReason.cs
            Events/
                NetworkEvents.cs
            Models/
                ConnectionRequestEventArgs.cs
                ConnectionRequestInfo.cs
                ConnectionResult.cs
                IPacket.cs
                NetworkModels.cs
                PacketContext.cs
            Exceptions/
                NetworkExceptions.cs
    Infrastructure/
        DependencyInjection/
            ServiceCollectionExtensions.cs
NetworkTests/
    NetworkTests.csproj
    AdaptersTests/
        Network/
            LiteNetLibPacketRegistryAdapterTests.cs
            NetworkDisconnectionTests.cs
            NetworkIntegrationTest.cs
        Serialization/
            SerializerTests.cs
    CoreTests/
        Application/
            NetworkEventBusTests.cs
```

## Arquitetura Hexagonal (Ports & Adapters)

O projeto segue o padrão de arquitetura hexagonal com:

### Core (Hexágono)

- **Domain**: Contém os modelos, eventos, exceções e enumerações relacionados ao domínio da rede.
- **Application**: Contém a lógica de negócios e define as portas (interfaces) que o domínio expõe e consome.
  - **Ports/Inbound**: Interfaces que o mundo externo usa para interagir com o aplicativo.
  - **Ports/Outbound**: Interfaces que o aplicativo usa para interagir com serviços externos.
  - **Services**: Implementações das interfaces inbound que orquestram a lógica de negócios.

### Adapters

- **Outbound Adapters**: Implementações de portas de saída que interagem com recursos externos (LiteNetLib).
  - **Network**: Adapta a biblioteca LiteNetLib para as interfaces definidas no core.
  - **Serialization**: Fornece implementações de serialização/deserialização de rede.
  - **Util**: Classes utilitárias para otimização e pooling.

### Infrastructure

- **DependencyInjection**: Configuração de injeção de dependência para conectar portas às implementações de adaptadores.

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

## Componentes Principais

### Core Domain

- **IPacket**: Interface fundamental para objetos que podem ser serializados e transmitidos pela rede.
- **NetworkEvents**: Define eventos padronizados para conexão, desconexão e comunicação de rede.
- **DeliveryMode**: Define os modos de entrega de pacotes (Unreliable, ReliableOrdered, etc.).

### Core Application

- **IClientApp/IServerApp**: Interfaces inbound para interagir com cliente e servidor.
- **ClientApp/ServerApp**: Implementações que orquestram os serviços de rede.
- **NetworkEventBus**: Intermediário para gerenciar eventos de rede.

### Adapters

- **LiteNetLibClientAdapter/LiteNetLibServerAdapter**: Adaptadores para cliente e servidor usando LiteNetLib.
- **LiteNetLibPacketRegistryAdapter**: Gerencia registro e processamento de pacotes.
- **LiteNetLibSerializerAdapter**: Implementa serialização baseada em LiteNetLib.

## Testes

- **NetworkIntegrationTest**: Testes de integração para comunicação cliente-servidor.
- **SerializerTests**: Testes unitários para serialização/deserialização.
- **NetworkEventBusTests**: Testes para o sistema de eventos de rede.

## Observações
- O Core não referencia Adapters ou Infrastructure diretamente.
- Adapters isolam dependências externas completamente.
- Application Layer orquestra o ciclo de vida de client/server usando apenas interfaces.
- Infrastructure centraliza DI e configurações.
- Testes cobrem domínio, aplicação e integração real dos adapters.

## Como usar

1. **Ports ➔ Adapters**  
   Cada interface em `Core/Application/Ports` deve ter correspondência em `Adapters/Inbound` ou `Adapters/Outbound`.  
2. **DI / Composition Root**  
   No seu projeto de inicialização (ex: `Startup.cs` ou similar), faça o mapeamento:  
   ```csharp
   services.AddNetworking();
   ```  
3. **Manutenção**  
   - Mantenha o Core livre de referências a LiteNetLib.  
   - Se precisar trocar de biblioteca, implemente novos Adapters sem alterar `Core`.  
4. **Documentação**  
   - Consulte arquivos em `/docs` para documentação adicional sobre componentes específicos.