# Apoio — Avisos de etapa concluída pela Audrey (Telegram)

## Objetivo

Enviar ao proprietário um resumo **somente após cada fase aprovada e commitada**, e um aviso final com evidências. A comunicação não dá privilégios ao sistema; é um canal de observabilidade.

## Segredos e integração

- O operador fornecerá `AUDREY_BOT_TOKEN` e `AUDREY_CHAT_ID` por variável de ambiente (ou segredo de CI), **nunca no prompt colado, na documentação, no git ou em log**.
- Se o projeto possuir um endpoint autenticado da própria Audrey para notificações, preferir essa integração segura; se não, usar a API Telegram Bot como transporte para uma mensagem enviada pela identidade da Audrey, **com permissão explícita**. Não inventar endpoint do servidor Audrey.
- **Recomendar rotação** do token de bot caso tenha sido exposto anteriormente em chat, screenshot, terminal ou commit. Não repetir valores em mensagens.
- Validar `chat_id` com allowlist simples, TLS, timeout curto, tratamento de 429 e 5xx com backoff, sem disparos duplicados.
- Garantir que chat de destino seja privado e autorizado; não enviar credenciais, conteúdo de banco, dados reais de clientes ou stdout bruto.

## Contrato da notificação

```
ISM — Etapa P2 Stock concluída
Status: PASS
Commit: <hash curto>
Testes: 30/30 executados (0 falhas críticas)
Tempo: <duração medida>
Relatório: docs/planejamento/relatorios/P2-stock.md
Próxima: P2 Pricing
```

Se a fase falhar, avisar **bloqueio** separadamente com nome e breve causa, sem declarar conclusão. Evitar avisos a cada mensagem de LLM ou tentativa de teste.

## Regras de retry

- Envio com chave idempotente `(runId, phaseId, commitHash, status)`, persistida em log local mínimo sem segredos.
- Até três retentativas com recuo; caso falhe registrar `delivery=failed` no relatório, não segurar execução indefinidamente.
- Conferir retorno real de `sendMessage` (`ok=true`) antes de declarar enviado.

## Protocolo de segurança

1. Ler env e verificar presença sem imprimir valor.
2. Executar mensagem de verificação **somente com autorização do proprietário** antes do processamento longo; registrar confirmação.
3. Para etapas subsequentes, automatizar aviso após gate e commit.
4. Terminado o projeto, enviar resumo final com lista dos commits, status e pendências remanescentes.
5. Não instalar serviços persistentes extras sem necessidade; script temporário ou integração no runner do Codex basta.

## Exemplo operacional (sem secrets)

Uma implementação pode usar `python` stdlib `urllib.request` ou `curl` com segredo apenas no ambiente. Preferir que URL/token não sejam mostrados em listas de processos. Não dar permissões administrativas ao bot nem integrar a lógica de testes de IA à administração remota do servidor.

## Gate

Relatório de entrega `relatorios/AUDREY-notificacoes.md`, com notificações enviadas, timestamps, status/IDs não sensíveis, falhas e política de retenção. Não versionar tokens nem chat ID real em repositório público.
