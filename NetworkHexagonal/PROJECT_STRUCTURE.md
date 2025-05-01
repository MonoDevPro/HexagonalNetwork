# Estrutura do Projeto

Este documento mapeia a organização de pastas e componentes para facilitar tanto desenvolvedores humanos quanto agentes (Copilot, CI, etc.) a entenderem onde cada peça deve ser implementada.

NetworkHexagonal/
├── Core/                       # Núcleo da aplicação (Domínio + Ports)
│   ├── Domain/                 # Lógica de negócio pura
│   │   ├── Entities/           # Entidades do domínio (ex: Packet, Peer, etc.)
│   │   ├── ValueObjects/       # Objetos-valor (ex: NetworkAddress, PeerId)
│   │   └── Exceptions/         # Exceções de domínio personalizadas
│   │
│   └── Application/            # Casos de uso / Interactors
│       ├── Ports/              # Interfaces (Input & Output Ports)
│       │   ├── Input/          # Input Ports: comandos e queries do Core
│       │   └── Output/         # Output Ports: serviços que o Core usa
│       └── Services/           # Implementações dos casos de uso
│
├── Adapters/                   # Implementações de Ports (Infraestrutura)
│   ├── Outbound/               # Driven Adapters (Core → externo)
│   │   ├── Networking/         # LiteNetLibAdapter
│   │   │   ├── Configuration/  # Mapeia INetworkConfiguration
│   │   │   ├── Packet/         # IPacketRegistry
│   │   │   ├── Serializer/     # INetworkSerializer
│   │   │   └── Sender/         # IPacketSender
│   │   │
│   │   └── SerializerAdapter/  # Adapter genérico de serialização
│   │
│   └── Inbound/                # Driving Adapters (externo → Core)
│       └── Messaging/          # Eventos de rede para Input Ports
│           └── Subscribers/    # Inscrições nos eventos
│
└── Infrastructure/             # Infraestrutura
    └── DependencyInjection/    # Configuração de injeção de dependência
│
├── Shared/                     # Código compartilhado entre camadas
│   ├── Kernel/                 # Flags, enums e DTOs genéricos
│   └── Utils/                  # Helpers, extensions e utilitários
│
└── tests/                      # Projetos de teste
    ├── CoreTests/              # Unit tests (Domain + Application)
    └── AdaptersTests/          # Integration tests (Adapters)

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