- `frontend`, `backend` e `mysql` rodam como serviços isolados na rede bridge `ism-network`.
- O código-fonte é montado nos containers via bind mounts (`./backend` → `/workspace/backend`, `./frontend` → `/workspace/frontend`).
- Backend utiliza `dotnet watch run` para recompilação automática e hot reload ao alterar arquivos `.cs`.
- Frontend utiliza `nuxt dev` com servidor de desenvolvimento Node.js 22 e suporte a HMR (Hot Module Replacement).
- O container backend aguarda conexões TCP no MySQL (`nc -z mysql 3306`) antes de iniciar.
- O container frontend faz checagem de hash do `package-lock.json` (`node_modules/.lock_hash`) para instalar dependências (`npm install`) apenas quando alteradas.

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

