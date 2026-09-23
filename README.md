# ISM - Intelligence Supply Manager

Repository for the ISM vertical SaaS platform for restaurants.

## Sobre o Projeto

O ISM (Intelligence Supply Manager) é um ecossistema vertical SaaS projetado para a gestão inteligente de suprimentos no setor gastronômico. A aplicação é dividida em um **Backend em .NET 8 (Clean Architecture/DDD)** e um **Frontend em Nuxt 3 (Composition API, Element Plus & Tailwind CSS)**.

## Documentação do Projeto

Toda a documentação técnica e operacional do projeto está organizada na pasta [`/docs`](./docs):

### 🚀 Guia de Desenvolvimento & Setup
- [🚀 Guia de Início Rápido (Setup)](./docs/development/setup.md): Como rodar o sistema com Docker, variáveis de ambiente e pre-commit hooks.
- [🐳 Workflow Docker](./docs/development/docker-workflow.md): Estrutura de containers, sincronização de código e perfis de migrations/reset.
- [🔄 Guia de Sincronização Git](./docs/development/git-sync.md): Como manter suas branches atualizadas com `main` e `develop`.
- [🛠️ Ferramental Backend (.NET / EF Core)](./docs/development/backend-tooling.md): Comandos da CLI do EF Core e pacotes oficiais.

### 🏗️ Arquitetura & Decisões
- [🏗️ Arquitetura Backend](./docs/architecture/backend.md): Visão das camadas Clean Architecture (`ISM.API`, `ISM.Application`, `ISM.Domain`, `ISM.Infrastructure`).
- [📐 Arquitetura Foundation](./docs/architecture/foundation.md): Estrutura geral dos projetos backend e ambiente dockerizado.
- [📝 ADR 0001 - Phase One Foundation](./docs/decisions/0001-phase-one-foundation.md): Registro da decisão arquitetural inicial.

### 📡 APIs & Dados
- [📖 Guia de Endpoints e Swagger API](./docs/api/README.md): Mapeamento completo dos endpoints (Auth, Stock, Suppliers, Menu, Import, Tenants, System).
- [📊 Mapeamento de Dados e Agentes IA](./docs/mapeamento-recepcao-dados-agentes.md): Origem dos dados, auditoria/lineage, importadores CSV/Excel/XML NF-e e consumo por agentes de IA.

### 📜 Diretrizes Globais
- [📋 Diretrizes de Desenvolvimento (DEV_GUIDELINES)](./DEV_GUIDELINES.md): Regras de ouro de arquitetura, nomenclatura, versionamento e migrations.
- [🤝 Guia de Contribuição (CONTRIBUTING)](./CONTRIBUTING.md): Padrões de commit, branching e configuração de Git Hooks.

