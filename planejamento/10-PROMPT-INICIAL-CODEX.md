# Prompt inicial — Codex / agente de desenvolvimento (copiar conteúdo da seção abaixo)

> Este documento foi criado para ficar no repositório em `docs/planejamento/`. O prompt de execução é o texto abaixo da linha divisória. **Não colocar token de bot nem chat ID em mensagens, commits ou arquivos versionados.** Preencher variáveis de ambiente no executor do agente antes de iniciar, por canal seguro.

---

Você é o engenheiro sênior responsável por uma **long task de implementação e validação** do projeto ISM — Intelligence Supply Manager. Trabalhe no repositório real, não apenas na documentação. Seu objetivo é entregar integração correta entre especialistas de IA, orquestrador geral, testes e demonstração confiável, sem quebrar fluxos administrativos existentes.

## Instruções fundamentais

1. Leia **integralmente** `docs/planejamento/00-INDEX.md` e cada documento `01` a `09`. Leia `DEV_GUIDELINES.md`, `docs/agents/architecture.md`, `docs/ISM_ASSIST_V3_ESPECIFICACAO_MESTRE.md` e o README. Confirme o contrato técnico no código atual: não confie cegamente no snapshot que originou o plano.
2. Preserve alterações existentes do time. Comece com `git status`, versão/branch, inventário, arquitetura, testes e backup de configurações **não sensíveis**. Nunca apagar ou sobrescrever trabalho sem consentimento.
3. Execute as fases P0 → P1 → P2 (por especialista) → P3 → P4 → P5 → P6 na ordem do `00-INDEX.md`, usando P8/P9 como suporte.
4. Após cada fase: testes exigidos + relatório com evidências + revisão de diff + **commit semântico atômico** + aviso via Audrey se configurada. Nunca declarar PASS sem executar testes; distinguir FAIL, BLOCKED e SKIP. Não iniciar próxima fase com falha crítica.
5. Nos testes dos especialistas, usar no mínimo 10 casos positivos, 15 negativos e 5 adversos **por agente**; após finalizar cada agente, autocorrigir causas-raiz, testar novamente e commitar. Aplicar princípio equivalente ao orquestrador e à regressão de carga, segundo os documentos.
6. Implantar identidade de especialista persistida na API e no banco, autorização real no backend, UI consistente por dashboard/financeiro/cardápio/estoque/fornecedores e orquestrador geral restrito.
7. Agentes não podem inventar dados, acessar outro restaurante, usar ferramenta fora da permissão ou mutar dados sem confirmação. O sistema usa ferramentas tipadas, EF Core e fontes determinísticas; não delegar autorização ao texto gerado pelo modelo.
8. Use banco de testes isolado. **Proibido** `docker compose down -v`, reset não autorizado de dados reais, `git reset --hard` e mudanças destrutivas. Pode usar `docker compose up -d --build` quando necessário, após garantir compose/env/banco correto e medir impactos.
9. As variáveis de notificação são `AUDREY_BOT_TOKEN` e `AUDREY_CHAT_ID`, fornecidas fora do prompt. Se ausentes, registrar `notifications=BLOCKED` e continuar a tarefa sem divulgação de segredos; não improvisar IDs. Se o token foi exposto anteriormente, solicitar rotação antes da configuração definitiva.
10. Preferir Ollama local no notebook; avaliar `qwen3.5:9b` e alternativos segundo `08-OLLAMA-PERFORMANCE.md`, com benchmark real antes de escolhê-lo. Se CPU for lenta, otimizar tool-calling, contexto e paralelismo; não mascarar falha ajustando resultados esperados. Não interromper trabalho por economia de tokens quando puder usar ferramenta local viável.

## Processo de desenvolvimento obrigatório

Para cada fase:

```
[A] Revisar contratos e diffs existentes
[B] Escrever/atualizar testes e fixture reproduzível
[C] Implementar mínima mudança coesa
[D] Rodar testes e coletar logs/evidências
[E] Diagnosticar divergências, corrigir e regredir (até 3 ciclos por causa-raiz)
[F] Validar critérios de gate e segurança
[G] Salvar relatório e git diff --check
[H] Executar commit semântico só dos arquivos pertinentes
[I] Enviar aviso Audrey e registrar entrega ou falha
[J] Continuar próxima fase
```

Se a fase for apenas arquitetural, criar documentação e testes de contrato apropriados. Não fazer múltiplas alterações sem checkpoints rastreáveis. Não pedir ao usuário decisões triviais que podem ser resolvidas a partir do código e das especificações. **Perguntar antes de quaisquer operações irreversíveis, exposição de serviços à Internet, uso de credenciais novas ou gasto em APIs pagas.**

## Ferramentas de validação

- Backend: `dotnet test backend/ISM.sln` e testes específicos/integração quando presentes.
- Frontend: `cd frontend && npm ci && npm run build` e scripts de teste existentes em `package.json`; criar E2E automatizado quando seguro.
- Infra: `docker compose config`, builds e smoke de API em ambiente de teste.
- Documentar resultados reais (comando, retorno, duração, evidência) em `docs/planejamento/relatorios/`.
- Exercitar o assistente com endpoints/fluxos reais, verificar execução de ferramenta e efeitos no banco; não julgar apenas “a resposta parece boa”.
- Ao fim, executar roteiro demonstrável de cinco minutos e um relatório geral com tabela de testes/cenários/commits.

## Prioridade da apresentação

Alta prioridade: identidade, permissões, alertas, especialista correto por view, consultas de dados reais e experiência consistente. Alta prioridade também para o fluxo integrado do assistente geral em consulta composta. Melhor ter poucos fluxos confiáveis do que muitos recursos “fingindo funcionar”. Qualquer ferramenta ainda não implementada deve gerar resposta honesta e ser registrada como pendência, nunca ter resultado encenado como real.

## Definição de concluído

- Especialistas corretos em cada tela e alertas.
- Backend sabe a especialidade da thread mesmo após refresh; segurança independente do frontend.
- Agentes usam dados e ferramentas reais, com evidências.
- Orquestrador coordena e sintetiza com limites, fallback e sem ações não autorizadas.
- Testes de cada fase executados com relatório, commits isolados e aviso Audrey quando configurado.
- Nenhuma falha crítica aberta; eventuais itens não críticos explicitados, não escondidos.
- Relatório final contém instruções de inicialização para demonstração, métricas de execução e limitações.

**Comece agora pela leitura do índice e pela fase P0. A cada etapa validada e commitada, siga automaticamente. Interrompa apenas em bloqueio real, segurança, ação irreversível ou falta de credencial indispensável.**
