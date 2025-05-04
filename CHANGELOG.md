# Changelog

Todas as mudanças notáveis deste projeto serão documentadas neste arquivo.

## [Unreleased]
- Documentação inicial criada
- Estrutura hexagonal implementada
- Adapters para LiteNetLib
- Testes de integração e unitários

## [1.0.0] - 2025-05-03
- Refatoração completa para arquitetura hexagonal (Ports & Adapters)
- Remoção de eventos públicos dos adapters; publicação de eventos agora é feita diretamente via NetworkEventBus
- Introdução do NetworkEventBus para desacoplamento de eventos administrativos (conexão, desconexão, erro)
- Organização dos eventos de domínio em subpasta temática (`Core/Domain/Events/Network`)
- Substituição de ClientApp/ServerApp por ClientNetworkApp/ServerNetworkApp
- Atualização dos ports, services e DI para refletir a nova arquitetura
- Documentação e exemplos atualizados para refletir as mudanças
- Melhoria na cobertura de testes para eventos e camada Application

## [0.1.0] - 2025-05-02
- Primeira versão pública
- Módulo de networking funcional com arquitetura hexagonal
- Suporte a LiteNetLib
- Testes automatizados básicos
