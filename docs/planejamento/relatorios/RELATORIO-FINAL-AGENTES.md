# Relatório final — correção dos agentes ISM

**Data:** 25/11/2026

**Modelo de carga:** `qwen3.5:4b` local via Ollama

**Situação:** fases 1–7 concluídas; sem commit.

## Resultado da carga final

Foram executados 20 prompts iniciais por especialista (100 no total). A primeira carga passou em 79/100. As 21 falhas foram classificadas, corrigidas e retestadas; a retentativa final passou em 21/21. A visão consolidada por caso é **100/100**, com histórico da primeira tentativa preservado no JSON.

| Especialista | Prompts | Resultado consolidado | Latência média consolidada |
|---|---:|---:|---:|
| Stock | 20 | 20/20 | 14,1 s |
| Pricing | 20 | 20/20 | 16,0 s |
| Suppliers | 20 | 20/20 | 14,3 s |
| Finance | 20 | 20/20 | 20,8 s |
| General | 20 | 20/20 | 20,2 s |

Das 100 respostas consolidadas, 81 mantiveram síntese Qwen e 19 usaram o formatador determinístico por segurança factual. Detalhes por prompt, respostas inicial e final, modo de resposta, latência e ferramentas estão em [P7-relatorio-final-qwen3.5-4b.json](P7-relatorio-final-qwen3.5-4b.json). A síntese local foi aceita somente quando preservou os fatos verificados; nos casos de consultas compostas em que o Qwen omitiu fatos essenciais, o sistema usou a resposta determinística formatada com os dados das ferramentas. O relatório registra o modo efetivamente utilizado.

## Correções concluídas

- Isolamento por especialista e validação da thread foram reforçados. O endpoint `POST /api/assistant/tools/execute` agora deriva o escopo da thread autorizada no servidor; o campo de especialista enviado pelo cliente não concede autorização. Threads de outro tenant são recusadas e especialistas não podem executar ferramentas de outro domínio.
- Roteamento determinístico cobriu consultas de reposição, margem/custo, pratos ativos no cardápio, preços históricos de fornecedores, pedidos pagos recentes e consultas gerais compostas.
- O contexto de ferramenta de fornecedores deixa claro que os registros são histórico de compras e não cotação atual. A formatação não apresenta timestamps desnecessários nem altera datas/valores.
- A verificação da síntese bloqueia números inventados e exige fatos centrais rotulados. Nomes de entidades consultadas são preservados; sínteses insuficientes recorrem ao formatador determinístico grounded.
- O formatador agora detecta payload sem o formato contratado e retorna uma mensagem segura indicando o schema inesperado, sem expor o payload nem lançar exceção por `GetProperty` ausente.
- A interface foi revisada; o drawer já exibe especialista, origem e quebras de linha. Não foi necessária alteração frontend.

## Pilotos e Groq

O piloto direcionado de fornecedores/financeiro terminou em 8/8 após os ajustes. A rodada intermediária havia exposto omissão de entidade e interpretação indevida do histórico como cotação atual; as instruções e a resposta apoiada em fontes foram corrigidas.

As tentativas com `llama-3.3-70b-versatile` e `qwen/qwen3.8-27b` não chegaram ao modelo: ambas foram bloqueadas por HTTP 403 do Cloudflare, código 1010. Não houve consumo confirmado de requisição/modelo nem resultado de inferência; não foram feitas novas tentativas. A chave compartilhada no chat deve ser rotacionada antes de eventual uso futuro.

## Validação

- `dotnet test backend/ISM.sln --no-restore -v q`: **PASS**, 278 unitários, 53 integração e 1 arquitetura (332 no total).
- Retentativa direcionada da carga Qwen: **PASS**, 21/21.
- Carga inicial Qwen: **79/100**; após retentativas consolidadas: **100/100**.
- `npm run --prefix frontend build`: **PASS**. Permaneceram avisos existentes de base Browserslist desatualizada, comentário PURE de dependência e chunk cliente acima de 500 kB.
- Memória monitorada durante a carga: pico observado de cerca de 5,8 GiB disponíveis e swap em 5,0/8 GiB; snapshot final após a carga: cerca de 6,2 GiB disponíveis e swap em 5,0/8 GiB.
- Nenhum comando `ollama stop`, limpeza de memória, `docker compose`, prune ou remoção de volume foi executado nesta retomada. Banco e containers não foram alterados.

## Estado do repositório

Alterações preexistentes foram preservadas. Não houve commit. A árvore de trabalho contém as mudanças do agente, artefatos de teste/relatórios e a pasta `planejamento/` já não rastreada antes desta retomada.
