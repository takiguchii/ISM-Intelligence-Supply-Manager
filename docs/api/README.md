# Documentação da API ISM

A API do ISM é desenvolvida em ASP.NET Core (.NET 8) e expõe a interface REST para o frontend Nuxt 3 e integrações externas.

## Swagger UI

Durante o desenvolvimento local (via Docker ou `dotnet run`), a documentação interativa do Swagger está acessível em:
- **Swagger UI:** [http://localhost:8080/swagger](http://localhost:8080/swagger)

---

## Mapeamento de Endpoints

### 🔐 Autenticação & Sessão (`/api/auth`, `/api/dev`)
- `POST /api/auth/login` — Autenticação por e-mail e senha (retorna JWT e Refresh Token).
- `POST /api/auth/register` — Cadastro de novos usuários/restaurantes.
- `GET  /api/auth/me` — Retorna dados do usuário autenticado no contexto atual.
- `POST /api/auth/refresh-token` — Renovação de JWT via token de atualização.
- `POST /api/dev/auth/login-as` — Endpoint de desenvolvimento para alternar identidade sem credenciais reais.

### ⚙️ Sistema & Healthcheck (`/api/system`)
- `GET  /api/system/status` — Status do backend, uptime e conectividade com o MySQL.

### 📦 Gestão de Estoque & Produtos (`/api/stock/products`)
- `GET    /api/stock/products` — Lista produtos do restaurante autenticado.
- `GET    /api/stock/products/{id}` — Obtém detalhes de um produto.
- `POST   /api/stock/products` — Cadastra um novo produto no estoque.
- `PUT    /api/stock/products/{id}` — Atualiza os dados de um produto.
- `DELETE /api/stock/products/{id}` — Remove/desativa um produto.

### 🚚 Fornecedores (`/api/suppliers`)
- `GET    /api/suppliers` — Lista fornecedores cadastrados.
- `GET    /api/suppliers/{id}` — Detalhes do fornecedor.
- `POST   /api/suppliers` — Cadastra novo fornecedor.
- `PUT    /api/suppliers/{id}` — Atualiza fornecedor.
- `DELETE /api/suppliers/{id}` — Remove fornecedor.

### 🍽️ Cardápio: Categorias & Pratos (`/api/menu/*`)
- `GET    /api/menu/categories` — Lista categorias do cardápio.
- `POST   /api/menu/categories` — Cria nova categoria.
- `PUT    /api/menu/categories/{id}` — Atualiza categoria.
- `DELETE /api/menu/categories/{id}` — Remove categoria.
- `GET    /api/menu/dishes` — Lista pratos do cardápio.
- `GET    /api/menu/dishes/{id}` — Obtém detalhes e ficha técnica do prato.
- `POST   /api/menu/dishes` — Cria prato.
- `PUT    /api/menu/dishes/{id}` — Atualiza prato.
- `DELETE /api/menu/dishes/{id}` — Remove prato.

### 📥 Importação de Dados & Auditoria (`/api/import`)
- `POST /api/import/stock/products` — Importa produtos via CSV/Excel (`multipart/form-data`).
- `POST /api/import/suppliers` — Importa fornecedores via CSV/Excel (`multipart/form-data`).
- `POST /api/import/nfe/xml` — Importa nota fiscal de entrada via XML NF-e (SEFAZ).
- `GET  /api/import/history` — Consulta o histórico de lotes importados (Audit Lineage).
- `GET  /api/import/history/{importId}` — Detalhes e log de erros de um lote específico.

### 🏢 Restaurante & Configurações de IA (`/api/me/restaurant/*`, `/api/restaurants/*`)
- `GET / PUT /api/me/restaurant/profile` — Consulta e atualiza o perfil do restaurante logado.
- `GET / PUT /api/me/restaurant/ai-config` — Consulta e atualiza a chave/modelo de IA (Gemini/OpenAI).
- `POST     /api/me/restaurant/ai-config/test` — Testa a conexão com a API de IA configurada.

### 👥 Usuários & Permissões (`/api/users`)
- `GET  /api/users` — Lista usuários com acesso ao restaurante.
- `POST /api/users` — Convida/cadastra novo colaborador.

