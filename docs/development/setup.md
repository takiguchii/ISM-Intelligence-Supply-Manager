# Guia de Início Rápido (Setup)

Este guia é destinado a desenvolvedores Backend e Frontend para colocar o ambiente ISM de pé rapidamente, sem necessidade de configurações complexas.

## Pré-requisitos

- **Docker e Docker Compose** instalados (Essencial).
- **Git** configurado.

## Rodando com Docker (Recomendado)

O projeto está configurado para rodar completamente via Docker, garantindo que o banco de dados, o backend e o frontend funcionem em harmonia.

1. **Configurar Variáveis de Ambiente:**
   Copie o arquivo de exemplo na raiz do projeto:
   ```bash
   cp .env.example .env
   ```

2. **Subir os Containers:**
   Execute o comando abaixo na raiz do projeto (isso pode demorar alguns minutos na primeira vez):
   ```bash
   docker compose up --build
   ```

3. **Acessar as Aplicações:**
   - **Frontend:** [http://localhost:3000](http://localhost:3000)
   - **Backend (Documentação Swagger):** [http://localhost:8080/swagger](http://localhost:8080/swagger)

## Guia para Frontend (Sem dor de cabeça)

Se você é do Frontend e só precisa que o backend funcione para você trabalhar:
1. Garanta que o Docker está aberto.
2. Rode `docker compose up`.
3. Se o frontend não carregar dados, verifique no console se há erros de conexão com a porta `8080`.
4. O backend rodando via Docker já configura o banco de dados automaticamente.

## Configurações Técnicas

### Backend (appsettings.json)
O arquivo `backend/src/ISM.API/appsettings.json` já vem configurado para o ambiente de container (apontando para o host `db`). 

Se você optar por rodar o backend localmente (via Visual Studio ou `dotnet run`) fora do Docker, você precisará:
- Ter um MySQL rodando localmente.
- Alterar a ConnectionString no `appsettings.Development.json` para apontar para `localhost`.

---
**Problemas Comuns:**
- **Porta em uso:** Se o MySQL não subir, verifique se você já tem um MySQL/MariaDB rodando na porta `3306`.
- **Banco Vazio:** Na primeira execução, o sistema aplica as "Migrations" automaticamente. Aguarde o backend estabilizar.
