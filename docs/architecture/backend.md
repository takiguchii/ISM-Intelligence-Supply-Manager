# Arquitetura do Backend

O backend do ISM segue os princípios da **Clean Architecture** e **Domain-Driven Design (DDD)** simplificado, garantindo que o código seja fácil de manter, testar e evoluir.

## Estrutura de Camadas

A solução está dividida nos seguintes projetos dentro de `backend/src/`:

### 1. ISM.Domain
A camada mais interna e importante. Contém a essência do negócio.
- **Entities:** Modelos de dados do domínio (ex: `PlatformModule`, `BaseEntity`).
- **Interfaces:** Contratos de repositórios que definem como os dados devem ser acessados.
- **Regras de Negócio:** Lógica que pertence puramente ao domínio do problema.

### 2. ISM.Application
Camada que orquestra os casos de uso da aplicação.
- **Services:** Implementação da lógica que coordena as entidades e repositórios.
- **DTOs:** Objetos simples (Data Transfer Objects) usados para enviar e receber dados via API.
- **Interfaces de Serviço:** Contratos que a API utiliza para interagir com a lógica de negócio.

### 3. ISM.Infrastructure
Implementação detalhada de tecnologia.
- **Data/Context:** Configuração do Entity Framework Core (`IsmDbContext`).
- **Migrations:** Scripts de versão do banco de dados MySQL.
- **Repositories:** Implementação real dos acessos ao banco de dados.

### 4. ISM.API
A "casca" externa da aplicação (ASP.NET Core).
- **Controllers:** Define as rotas (URLs) que o frontend irá chamar.
- **Middlewares:** Filtros globais (ex: tratamento de erros).
- **Program.cs:** Onde toda a mágica da Injeção de Dependência acontece.

## Fluxo de uma Requisição

1. O **Frontend** (Nuxt) chama um endpoint (ex: `GET /api/system/status`).
2. O **Controller** na `ISM.API` recebe o chamado.
3. O Controller pede ao **Service** na `ISM.Application` para processar a lógica.
4. O Service busca os dados necessários via **Repository** na `ISM.Infrastructure`.
5. O Repository consulta o **Banco de Dados MySQL**.
6. Os dados retornam transformados em **DTOs** para o Frontend.

---
**Vantagem desta arquitetura:** Se amanhã decidirmos trocar o MySQL pelo PostgreSQL, só precisamos mexer na camada de `Infrastructure`. O resto do sistema nem ficará sabendo da mudança.
