# Documentação oficial do ISM

Esta pasta contém a documentação funcional, técnica e operacional do Intelligence Supply Manager. O índice abaixo é a fonte de navegação para desenvolvimento, operação e auditoria.

## Produto e arquitetura

- [Especificação ISM Assist V3](ISM_ASSIST_V3_ESPECIFICACAO_MESTRE.md)
- [Arquitetura do backend](architecture/backend.md)
- [Arquitetura de fundação](architecture/foundation.md)
- [Arquitetura e catálogo dos agentes](agents/architecture.md)
- [Fluxo de recepção, proveniência e consumo de dados](mapeamento-recepcao-dados-agentes.md)

## Integrações e operação

- [Documentação da API](api/README.md)
- [Configuração e setup](development/setup.md)
- [Fluxo Docker](development/docker-workflow.md)
- [Ferramentas do backend](development/backend-tooling.md)
- [Configurações](development/settings.md)
- [Notificações externas](development/notifications.md)
- [Sincronização Git](development/git-sync.md)

## Segurança e governança

- [Perfis e autorização](security/perfis-e-autorizacao.md)
- [Dívidas de segurança da fundação](security/foundation-hardening-debt.md)
- [ADR 0001 — Foundation](decisions/0001-phase-one-foundation.md)

## Experiência visual e multimodalidade

- [Cocou e experiência do Assistente](visuals/assistant.md)
- [Corpus oficial de recibos para QA](Imagens%20de%20notas/manifest.json)

## Convenções

Os documentos descrevem o comportamento implementado ou uma decisão arquitetural aprovada. Checkpoints de execução, prompts de agentes, logs de uma tarefa e relatórios temporários não fazem parte desta documentação oficial.
