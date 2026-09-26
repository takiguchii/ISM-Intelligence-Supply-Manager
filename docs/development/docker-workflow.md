- `frontend`, `backend` e `mysql` rodam como serviços isolados na rede bridge `ism-network`.
- O código-fonte é montado nos containers via bind mounts (`./backend` → `/workspace/backend`, `./frontend` → `/workspace/frontend`).
- Backend utiliza `dotnet watch run` para recompilação automática e hot reload ao alterar arquivos `.cs`.
- Frontend utiliza `nuxt dev` com servidor de desenvolvimento Node.js 22 e suporte a HMR (Hot Module Replacement).
- O container backend aguarda conexões TCP no MySQL (`nc -z mysql 3306`) antes de iniciar.
- O backend calcula um hash de `ISM.sln`, `.csproj` e `packages.lock.json`; um restart só executa restore quando esses manifestos mudam. O cache NuGet permanece no volume local.
- O container frontend faz checagem de hash do `package-lock.json` (`node_modules/.lock_hash`) e usa `npm ci` apenas quando o lock muda.

## Banco isolado por branch

Antes do primeiro `docker compose up`, execute `scripts/sync-branch-db.sh`. O script atualiza somente `ISM_BRANCH_SLUG` no `.env`, preservando secrets, e seleciona o volume MySQL daquela branch. Com os hooks habilitados conforme o `CONTRIBUTING.md`, isso também ocorre após cada checkout. Nenhum container ou volume é removido automaticamente.

Para recuperar um banco com migrations incompatíveis de uma branch, volte ao contexto dela e execute explicitamente:

```bash
docker compose --profile tools run --rm db-reset
```

Esse comando é destrutivo apenas para o banco da branch selecionada.

## Seed de desenvolvimento

Para criar dados locais, defina `SEED_DEMO_DATA=true` e uma `SEED_PASSWORD` no `.env`, depois suba o ambiente. O seed roda apenas em `Development`, é idempotente e cria `admin@ism.com.br`, `gerente@ism.com.br`, `chef@ism.com.br` e `garcom@ism.com.br`, todos com a senha definida localmente.

## Perfis de Ferramentas do Banco (Docker Compose Profiles)

O `docker-compose.yml` inclui perfis de utilitários de banco sob o perfil `tools`:

### 1. Executar Migrations sem subir toda a aplicação
```bash
docker compose --profile tools run --rm db-migrate
```

### 2. Recriar Banco Local do Zero (Drop + Migrate)
```bash
docker compose --profile tools run --rm db-reset
```
