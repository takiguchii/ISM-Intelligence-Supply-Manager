# ISM Assist

O ISM Assist é a camada administrativa visual sobre os alertas determinísticos de precificação, estoque e fornecedores. No dashboard, ele substitui os cards fragmentados por um hub único com três especialistas, prioridades, ações auditáveis e conversa contextual. Nas telas de estoque, fornecedores e financeiro, o acesso contextual abre o mesmo Assistente; não existem pedidos ou mutações fictícias. O Ollama é usado somente para explicar e conversar; valores e mutações continuam sob as regras do ISM e a aprovação explícita do gerente.

## Arquitetura

O scheduler existente continua gerando `SystemAlert`. O `AssistantService` transforma esses alertas em propostas tenant-scoped, expõe overview e ações, e usa `OllamaTextProvider` apenas em `/api/assistant/chat`. A única escrita de negócio do MVP é a aprovação de uma proposta `UpdateDishPrice`, que valida o preço original, atualiza `Dish.Price`, incrementa `PriceRevision` e grava `AssistantActionAudit` na mesma transação relacional.

## Ollama local

```bash
ollama --version
ollama serve
ollama pull gemma3:270m
ollama list
curl http://127.0.0.1:11434/api/tags
curl http://127.0.0.1:11434/api/chat \
  -H 'Content-Type: application/json' \
  -d '{"model":"gemma3:270m","messages":[{"role":"user","content":"Responda em português com uma frase curta."}],"stream":false}'
```

Quando o backend roda no host, use `AssistantAi__OllamaBaseUrl=http://127.0.0.1:11434`. No Docker Compose Linux, use `http://host.docker.internal:11434` somente se o host-gateway estiver disponível no ambiente; valide a conectividade de dentro do container. Não configure `OLLAMA_HOST=0.0.0.0` indiscriminadamente nem exponha a porta publicamente.

## Configuração

Ative localmente sem colocar segredos no Git:

```bash
export ASSISTANT_AI_ENABLED=true
export ASSISTANT_OLLAMA_BASE_URL=http://127.0.0.1:11434
export ASSISTANT_AI_TIMEOUT_SECONDS=20
docker compose up -d mysql backend frontend
```

O modelo fixado pelo Compose é `gemma3:270m`. O endpoint nunca é enviado ao frontend. Para desativar a conversa, use `ASSISTANT_AI_ENABLED=false`; alertas e ações determinísticas permanecem disponíveis.

## API e acesso

Todos os endpoints exigem JWT e `RestaurantManagerOrAbove`:

- `GET /api/assistant/status`
- `GET /api/assistant/overview?page=1&limit=10`
- `GET /api/assistant/actions?page=1&limit=10`
- `GET /api/assistant/actions/{id}`
- `POST /api/assistant/actions/{id}/approve`
- `POST /api/assistant/actions/{id}/reject`
- `POST /api/assistant/actions/{id}/review` para concluir alertas operacionais sem mutar estoque/fornecedor
- `POST /api/assistant/chat` com `{ "message": "..." }`

Admin sem `restaurantId` é SuperAdmin de plataforma e não recebe escrita automática em restaurante. Chef/Waiter não passam na política. Troca de preço concorrente retorna `409` e marca a proposta como `Stale`.

## Migration

Aplicação aditiva em ambiente controlado:

```bash
dotnet ef database update \
  --project backend/src/ISM.Infrastructure/ISM.Infrastructure.csproj \
  --startup-project backend/src/ISM.API/ISM.API.csproj
```

Não use `db-reset`, `down -v` ou `database drop` em uma base com dados reais. A migration cria `assistant_proposals`, `assistant_action_audits` e `Dish.PriceRevision`.

## Demonstração segura

Use uma base de demonstração separada com um prato cujo custo total seja R$48, preço R$80 e margem alvo 60%; o cálculo determinístico deve sugerir R$120. Antes da aprovação o preço deve continuar R$80. Depois do `POST .../approve`, confirme R$120 no prato e uma linha de auditoria. Rejeição não deve alterar preço; uma segunda aprovação deve retornar conflito.

O painel visual aparece apenas para Admin/Manager com restaurante. Ele consulta somente a API ISM, mostra propostas reais e exibe `llm` ou `deterministic_fallback` conforme o resultado observado.

O mascote compacto aparece nas telas administrativas fora do dashboard e reage aos estados `idle`, `watching`, `thinking`, `attention`, `approval`, `error` e `offline`. O dashboard usa o hub expandido como superfície principal, evitando duas listas de alertas concorrentes.

## Troubleshooting

- **11434 indisponível:** confira `ollama serve`, `curl /api/tags` e a URL host/container.
- **Modelo faltante:** execute exatamente `ollama pull gemma3:270m`.
- **401/403:** renove a sessão e confirme papel/`restaurantId`.
- **409:** o preço mudou desde a proposta; atualize os alertas e gere uma nova proposta.
- **Timeout/offline:** o chat entra em fallback; nenhuma mutação é executada pelo LLM.
- **Migration:** confirme a connection string e aplique somente migrations pendentes em ambiente controlado.

## Limitações honestas

O modelo de 270M é pequeno e pode responder de forma imprecisa. Ele não calcula margem, não chama ferramentas, não executa SQL e não altera entidades. O contexto é curto e limitado aos alertas do tenant autenticado. A qualidade deve ser validada na apresentação e a fonte de verdade é sempre o banco/regra determinística do ISM.
