# Estrutura do Projeto: NetworkHexagonal

Este documento descreve a estrutura de pastas e responsabilidades do módulo de networking MMO em C# com arquitetura hexagonal.

## Estrutura de Pastas

```
NetworkHexagonal/
├── Adapters/
│   ├── Inbound/
│   └── Outbound/
│       └── Network/
│           ├── LiteNetLibServerAdapter.cs
│           ├── LiteNetLibClientAdapter.cs
│           └── ...
├── Core/
│   ├── Application/
│   │   ├── Ports/
│   │   │   ├── Inbound/
│   │   │   └── Outbound/
│   │   └── Services/
│   │       ├── ClientNetworkApp.cs
│   │       ├── ServerNetworkApp.cs
│   │       └── NetworkEventBus.cs
│   └── Domain/
│       └── Events/
│           └── Network/
│               ├── ConnectionEvent.cs
│               ├── DisconnectionEvent.cs
│               ├── NetworkErrorEvent.cs
│               └── ConnectionRequestEvent.cs
├── Infrastructure/
│   └── DependencyInjection/
│       └── NetworkServiceRegistration.cs
├── Shared/
│   ├── Kernel/
│   └── Utils/
├── NetworkTests/
│   ├── AdaptersTests/
│   └── CoreTests/
└── README.md
```

## Responsabilidades

- **Core:** Apenas contratos, modelos e lógica de domínio. Sem dependências externas.
- **Adapters:** Implementação de ports do Core, isolando dependências externas (ex: LiteNetLib).
- **Application:** Orquestra ciclo de vida e eventos via NetworkEventBus.
- **Infrastructure:** Configuração de DI e orquestração.
- **Shared:** Utilitários e código comum.
- **Tests:** Testes unitários e de integração.

## Observações

- O barramento de eventos (`NetworkEventBus`) deve ser usado apenas para eventos administrativos (conexão, desconexão, erro), nunca para fluxos críticos de gameplay.
- O Core não referencia adapters ou infraestrutura.
- Adapters não expõem eventos públicos, apenas publicam no barramento.
