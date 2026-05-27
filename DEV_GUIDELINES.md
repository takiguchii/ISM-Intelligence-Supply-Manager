# ISM - Intelligence Supply Manager: Project Instructions

Este arquivo contém as diretrizes, padrões e fluxos de trabalho fundamentais para o projeto ISM. Estas instruções devem ser seguidas rigorosamente por todos os desenvolvedores.

## 📚 Documentação de Referência

Sempre consulte os guias detalhados antes de grandes alterações:
- [🚀 Guia de Setup](./docs/development/setup.md)
- [🔄 Sincronização Git](./docs/development/git-sync.md)
- [🏗️ Arquitetura Backend](./docs/architecture/backend.md)
- [🛠️ Tooling Backend](./docs/development/backend-tooling.md)

## 🏗 Arquitetura e Estrutura

### Backend (.NET 8)
- **Padrão:** Clean Architecture / Domain-Driven Design (simplificado).
- **Regras Arquiteturais Rígidas (Evitar Desvios):**
    - **APENAS UM ESCOPO:** Remover arquivos não utilizados (ex: endpoints e serviços não relacionados ao escopo de trabalho atual).
    - **Separação de Interfaces:**
      - Interfaces da camada de Aplicação (`IService`) **DEVEM** ficar na pasta `ISM.Application/Interfaces/` (nunca misturadas com as implementações na pasta `Services`).
      - Interfaces de Domínio (`IRepository`) **DEVEM** ficar na pasta `ISM.Domain/Interfaces/`.
- **Camadas:**
    - `ISM.API`: Controllers, Middlewares, Program.cs e configurações de host.
    - `ISM.Application`: Casos de uso, DTOs, Mapeamentos e Interfaces de Serviço (em `Interfaces/`).
    - `ISM.Domain`: Entidades (POCOs), Interfaces de Repositório (em `Interfaces/`) e Regras de Negócio.
    - `ISM.Infrastructure`: Implementação de Repositórios, Contexto do EF Core, Migrations e Integrações.
- **Banco de Dados:** MySQL centralizado em `ISM.Infrastructure/Data`. Mapeamento via Fluent API no `IsmDbContext`.

### Frontend (Nuxt 3)
- **Padrão:** Composition API com TypeScript.
- **UI:** Tailwind CSS para estilização e Element Plus para componentes de interface.
- **Serviços:** Consumo de API centralizado em `frontend/services`.

## 🛠 Padrões de Código e Métricas

### Nomenclatura (Backend)
- Interfaces: Prefixo `I` (ex: `IPlatformModuleRepository`, `IFornecedorService`).
- Assincronismo: Métodos devem terminar em `Async` e retornar `Task` ou `ValueTask`.
- Injeção de Dependência: Utilize métodos de extensão `IServiceCollection.Add[Nome]Services`.

### Nomenclatura (Frontend)
- Componentes: PascalCase (ex: `StatusMetricCard.vue`).
- Composables: Prefixo `use` (ex: `useSystemStatus.ts`).

### Commits e Branchs
- **Commit:** `<prefixo>: <descrição em português>` (ex: `feat: adiciona login`).
    - Prefixos: `feat`, `fix`, `refactor`, `test`, `doc`, `mod`, `css`, `hotfix`.
- **Branch:** `<prefixo>/<área>/<descrição>` (ex: `feat/backend/auth`).

## 🔄 Fluxos de Trabalho (Padronizados)

- **Novas Entidades e CRUDs (Ordem de Criação):**
    1. **Entidade:** Criar em `ISM.Domain/Entities`.
    2. **Interface do Repositório:** Criar em `ISM.Domain/Interfaces`.
    3. **Implementação do Repositório:** Criar em `ISM.Infrastructure/Repositories` e registrar na DI.
    4. **DTO:** Criar em `ISM.Application/DTOs`.
    5. **Interface de Serviço:** Criar em `ISM.Application/Interfaces`.
    6. **Serviço:** Criar em `ISM.Application/Services` (com injeção da interface do repositório) e registrar na DI.
    7. **Mapeamento de Banco:** Adicionar em `IsmDbContext.OnModelCreating`.
    8. **Migration:** Gerar migration (`dotnet ef migrations add ...`).
    9. **Controller:** Criar em `ISM.API/Controllers` chamando a Interface do Serviço.
- **Validação:** Sempre executar `dotnet build` no backend e `npm run lint` no frontend (quando disponível) após alterações.
- **Refatoração:** Ao mover arquivos entre camadas, atualizar os namespaces e garantir que a `ISM.API` continua referenciando corretamente as novas localizações via DI.

## 📦 Versionamento e Commits

- **Idioma:** A descrição do commit deve ser sempre em **Português**.
- **Frequência:** Realizar commits ao finalizar blocos lógicos.
- **Branches:** O usuário cria as branches manualmente. Sugira novas se necessário.
