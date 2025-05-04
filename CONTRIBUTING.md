# Guia de Contribuição

Obrigado por considerar contribuir com o NetworkHexagonal!

## Como contribuir

1. Fork este repositório e crie sua branch (`git checkout -b feature/nome-da-sua-feature`)
2. Faça suas alterações e adicione testes
3. Certifique-se de que todos os testes passam (`dotnet test`)
4. Envie um Pull Request detalhando sua contribuição

## Boas práticas
- Siga o padrão de arquitetura hexagonal já estabelecido
- Documente seu código e adicione comentários quando necessário
- Prefira commits pequenos e descritivos
- Atualize a documentação se necessário

## Arquitetura e Padrões
- Siga rigorosamente a arquitetura hexagonal:
  - O Core (domínio) não deve depender de adapters, infrastructure ou bibliotecas externas.
  - Toda lógica de LiteNetLib e dependências externas deve estar isolada nos adapters.
  - Use o NetworkEventBus apenas para eventos administrativos (conexão, desconexão, erro), nunca para fluxos críticos de gameplay.
  - Adapters não devem expor eventos públicos, apenas publicar eventos no barramento.
- Ao criar ou modificar eventos, siga o padrão de publicação/assinatura via NetworkEventBus.
- Sempre atualize a documentação e os testes ao alterar fluxos de eventos, arquitetura ou interfaces.

## Dúvidas
Abra uma issue para discutir qualquer dúvida ou sugestão antes de começar grandes mudanças.
