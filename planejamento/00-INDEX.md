# ISM — Plano mestre de especialistas e orquestrador

**Data de planejamento:** 23/11/2026

**Escopo:** melhorar os agentes administrativos, corrigir a identidade de conversas, implementar orquestração e validar as funcionalidades para a demonstração.

**Base técnica:** snapshot do repositório `ISM-Intelligence-Supply-Manager-develop`, inspecionado antes da geração deste pacote. Não presumir que arquivos e contratos não mudaram na branch de trabalho atual.

## Objetivo de produto

O ISM é uma plataforma B2B SaaS para gestão de restaurantes, em que dados de pedidos, cardápio, estoque, fornecedores e financeiro sustentam agentes especializados. O produto deve demonstrar **consulta real → análise explicável → ação autorizada → resultado verificável**. O assistente geral atua como orquestrador, não como uma fonte independente de dados inventados.

## Documentos e ordem mandatória

| Fase | Documento | Entregável obrigatório | Gate para avançar |
|---|---|---|---|
| P0 — Baseline | [01-DIAGNOSTICO-BASE.md](01-DIAGNOSTICO-BASE.md) | Inventário real, testes iniciais, plano de arquivos | Baseline auditável |
| P1 — Integração | [02-CORRECAO-INTEGRACAO-AGENTES.md](02-CORRECAO-INTEGRACAO-AGENTES.md) | Especialistas acessíveis por tela/alerta, identidade persistida e autorização no backend | Testes funcionais + código commitado |
| P2 — Especialistas | [03-TESTES-ESPECIALISTAS.md](03-TESTES-ESPECIALISTAS.md) | Matrizes por especialista, evidências, correções | 100% dos invariantes críticos + thresholds definidos |
| P3 — Projeto orquestrador | [04-ORQUESTRADOR-ARQUITETURA.md](04-ORQUESTRADOR-ARQUITETURA.md) | Contratos, limites, plano de execução, autorização | Revisão de arquitetura e testes de contrato |
| P4 — Implementação | [05-ORQUESTRADOR-IMPLEMENTACAO.md](05-ORQUESTRADOR-IMPLEMENTACAO.md) | Assistente geral coordenando ferramentas/especialistas | Integração real com evidências |
| P5 — Validação orquestrador | [06-TESTES-ORQUESTRADOR.md](06-TESTES-ORQUESTRADOR.md) | Cenários mono/multidomínio e autocorreção | Sem falha crítica |
| P6 — Integração/carga | [07-TESTES-INTEGRADOS-CARGA.md](07-TESTES-INTEGRADOS-CARGA.md) | Execução concorrente controlada, logs e relatório | Segurança + estabilidade demonstradas |
| Apoio de infraestrutura | [08-OLLAMA-PERFORMANCE.md](08-OLLAMA-PERFORMANCE.md) | Modelo local, benchmark no notebook, modo fallback | Modelo alcançável sem exaurir RAM |
| Apoio de comunicação | [09-AUDREY-TELEGRAM.md](09-AUDREY-TELEGRAM.md) | Notificação ao concluir cada etapa | Confirmação de entrega / registro de falha |
| Orquestração do trabalho | [10-PROMPT-INICIAL-CODEX.md](10-PROMPT-INICIAL-CODEX.md) | Prompt pronto para o Codex | Início autônomo com verificações |

## Contrato geral da tarefa longa

1. Ler os documentos na ordem, consultar o código real e não improvisar contratos ausentes.
2. Gerar `docs/planejamento/relatorios/<fase>.md` com baseline, modificações, evidências, comandos, limitações e pendências. Criar evidências em `docs/planejamento/evidencias/` sem dados pessoais nem segredos.
3. Cada fase de implementação/testes exige commit semântico isolado (`feat:`, `fix:`, `test:`, `docs:`); **somente declarar concluída após commit e testes exigidos**. Se a fase não alterou código, registrar checkpoint e commit documental quando pertinente.
4. **Jamais prosseguir após falha crítica**, falha de autorização, vazamento entre restaurantes, mutação não confirmada, dados inventados ou corrupção de banco. Corrigir, executar a suíte afetada e depois regressão. Não fazer loop infinito: até três tentativas de correção por causa-raiz e, se persistir, registrar bloqueio.
5. Enviar mensagem pela Audrey via Telegram somente após a fase estar validada e commitada; se a notificação falhar, registrar separadamente e prosseguir apenas se não impactar os critérios de qualidade. Nunca expor tokens.
6. Não reescrever dados reais, resetar volumes ou executar operações destrutivas. Usar banco descartável ou fixtures isoladas para testes.
7. Gráficos e valores devem vir de dados estruturados reais com período e origem, não de texto livre do modelo.
8. Priorizar a apresentação: percurso P0/P1/P2/P4 e demonstração de ponta a ponta antes de funcionalidades sofisticadas não essenciais.

## Matriz de agentes por tela

| Tela | Especialista | Interação |
|---|---|---|
| Dashboard | `stock`, `pricing`, `suppliers`, `finance` | Cartões/alertas e botão “Conversar”; contexto associado a cada alerta |
| Financeiro | `finance` | Botão “Conversar com o agente” e consultas financeiras/gráficos com fontes reais |
| Pedidos | nenhum novo | Manter fluxo operacional atual |
| Cardápio | `pricing` | Mascote/cartão compacto, “Conversar”, contexto de prato quando aplicável |
| Estoque | `stock` | Preservar alerta funcional e permitir conversa sem alerta |
| Fornecedores | `suppliers` | “Conversar” disponível mesmo sem alerta |
| Funcionários, Integrações, Configurações | nenhum novo | Preservar interfaces |
| Ícone flutuante | `general` | Orquestrador; direciona e agrega análises |

## Critérios de saída globais

- Identidade/escopo autorizados **no servidor** e persistidos por thread; mesmo agente em abertura, histórico, streaming e recarga.
- Cada especialista acessa ferramentas próprias e consultas compartilhadas estritamente necessárias, não executa comandos financeiros ou administrativos fora de competência.
- Zero mutações sem autorização e zero vazamentos cross-tenant.
- Respostas numéricas com evidência de banco, incluindo casos sem dados.
- Roteiros de demonstração reproduzíveis com dados sintéticos não sensíveis.
- Build frontend/backend, testes automáticos e fluxos end-to-end possíveis no ambiente com registros claros de executado vs não executado.
- Documentar funcionalidades fora de escopo para não apresentá-las como prontas.

## Convenção de resultados

`PASS`: execução real e evidência anexada; `FAIL`: resultado divergente; `BLOCKED`: dependência/infra faltante, **nunca transformar em PASS**; `SKIP`: fora de escopo justificado. Reportar total/percentual e causas.

## Entrega final

Ao fim, produzir `docs/planejamento/relatorios/RELATORIO-FINAL.md` com commits por etapa, provas, instruções de execução, limitações remanescentes e um roteiro de 5 minutos: alerta → chat especialista → recomendação com dado real → ação com aprovação → consulta ao agente geral. Avisar ao usuário via Audrey quando configurada, além do relatório local.
