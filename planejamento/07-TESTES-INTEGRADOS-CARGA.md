# P6 — Cenários integrados de dia a dia, concorrência e regressão

## Objetivo

Simular administradores legítimos consultando especialistas e assistente geral ao mesmo tempo, enquanto pedidos/cardápio/estoque/financeiro recebem alterações **em um ambiente descartável de testes**. Verificar isolamento, consistência de dados, latência, disponibilidade, comportamento com Ollama CPU e ausência de alterações não autorizadas.

## Preparação obrigatória

- Banco artificial / clone anonimizado sem dados pessoais, fixture repetível, IDs de tenants A e B; nunca conectar testes destrutivos ao banco de produção.
- Medir baseline de CPU, RAM, swap, uso de disco e latência com `free -h`, `top`/`htop`, `docker stats` e instrumentos existentes. Não assumir capacidade ilimitada.
- Desativar por padrão compras, pagamentos reais, notificações reais a terceiros e integração com provedores comerciais.
- Separar cenários API determinísticos de cenários com LLM. Modelos CPU devem ser limitados a 1 inference concorrente até medir capacidade; simulação de 10/20 usuários pode usar mocks para testar API, mas **não representa** throughput real de IA.

## Roteiro de usuário

1. Admin A abre dashboard, clica em alerta de estoque → consulta saldo e risco.
2. Abre alertas de precificação → inspeciona custo e simulação de preço com aprovação desativada.
3. Entra no cardápio → pergunta por pratos mais vendidos → conferência contra pedidos registrados.
4. Entra no financeiro → pede receita e despesa de período → visualiza gráfico lastreado nos dados.
5. Entra em fornecedores → compara histórico para insumo existente.
6. Abre ícone flutuante → pergunta multidomínio e recebe síntese com evidências.
7. Abre thread antiga → verifica identidade e histórico do especialista.
8. Usuário B tenta acessar thread e produto de A → servidor rejeita, inclusive sob concorrência.
9. Inserção nova no estoque/pedidos via API de testes autorizada → consultas refletem atualização; verificar cache/consistência.
10. Induzir timeout do modelo → resposta parcial correta ou fallback explícito, sem fabricação de dados.

## Escada de carga (adaptativa, não destrutiva)

| Passo | API sem IA | Com Ollama local real | Foco |
|---|---:|---:|---|
| L0 | 1 usuário | 1 consulta | smoke e latência base |
| L1 | 5 usuários | 1 requisição LLM em curso | concorrência banco/chat |
| L2 | 10 usuários | 1–2 LLM conforme benchmark | conexão, fila e memória |
| L3 | 20 usuários | somente se recursos permitirem | estresse graduado |

Se RAM/swap, temperatura ou erros aumentarem excessivamente, reduzir a carga ou suspender. Não force 100% de CPU durante horas sem supervisão térmica. Coletar p50/p95, falhas, filas, timeouts, uso RAM/CPU, consultas de DB e acerto funcional. Metas de latência devem ser definidas **após baseline**; não alegar metas atendidas sem medição.

## Casos de regressão e falhas

C01 abrir quatro especialistas simultaneamente em diferentes navegadores; C02 duas threads por usuário; C03 tenant A/B simultâneos; C04 refresh durante geração; C05 provider desligado; C06 banco temporariamente lento; C07 payload gigante rejeitado; C08 prompt injection em fornecedor; C09 tentativa de ação de escrita; C10 confirmação duplicada; C11 mudança de alerta entre telas; C12 recarregar sessão; C13 gráficos com período vazio; C14 data timezone; C15 correlação de logs. Executar cada um em cenário observável.

## Gate

- Zero erro de segurança/mutação não autorizada em todas as fases; zero corrupção de banco.
- Cenários críticos end-to-end passam com fixtures reais; eventual bloqueio de infraestrutura identificado como `BLOCKED`, não `PASS`.
- Performance medida e relatada, com limite operacional recomendado para notebook de 20 GB.
- Evidências de commits e execução. Gerar relatório `P6-integrado-carga.md` e commit `test(assistant): verify end-to-end concurrency and safety`.
- Preparar demo offline e fallback: respostas determinísticas reais caso o modelo não consiga responder no tempo da apresentação. Nunca simular análises que não ocorreram como se fossem ao vivo.
