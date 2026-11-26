# Assistente geral

## Responsabilidade

Atender conversas gerais do restaurante e responder com base em dados atuais do ISM. O especialista `general` pode coordenar consultas de estoque, precificação, fornecedores e financeiro; conversas de especialista permanecem limitadas ao seu domínio.

## Caminho de uma mensagem

1. `AssistantController` valida a requisição e obtém a conversa dentro do restaurante e usuário autenticados.
2. `AssistantService.SendMessageAsync` grava a mensagem, resolve contexto e bloqueia perguntas fora do escopo de uma conversa especializada.
3. Solicitações reconhecidas são convertidas em intenções por `AgentIntentRouter` e passam pela ferramenta allowlistada.
4. Consultas que combinam domínios podem seguir para o orquestrador.
5. Para perguntas sem rota determinística, o serviço monta contexto factual e histórico limitado e chama o provedor LLM configurado.
6. Se o provedor falha, responde com fallback explícito. Nenhuma ferramenta mutável é chamada pelo LLM.

## Conversas especializadas

Os IDs válidos são `general`, `stock`, `pricing`, `suppliers` e `finance`. Se a intenção detectada não corresponde ao especialista da conversa, a mensagem é recusada com orientação para abrir o Assistente geral. Mensagens com imagem são encaminhadas ao provedor visual com contexto e prompt restritos; não acionam ferramentas.

## Restrições

- O usuário precisa ter papel gerencial e restaurante válido.
- Só fatos fornecidos pelo sistema podem sustentar respostas factuais.
- O histórico enviado ao modelo é limitado às últimas oito mensagens; no modo especializado, somente mensagens do usuário são incluídas.
- A criação/confirmação de automações tem fluxo próprio, com rascunho e dry-run, descrito em [orquestrador](orquestrador.md).

## Implementação

`AssistantController.cs`, `AssistantService.cs`, `AssistantIntentCoordinator.cs`, `AgentIntentRouter.cs` e `AssistantSpecialists.cs`.
