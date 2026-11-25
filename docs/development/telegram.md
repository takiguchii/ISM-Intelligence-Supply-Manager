# Telegram da ISM

A integração oferece conversa com o assistente existente, comandos de consulta e alertas automáticos. Nesta versão, administradores e gerentes vinculados a um restaurante podem conectar uma conta individual do Telegram. Grupos não recebem dados do restaurante.

## Configuração do servidor

Crie o bot no BotFather e configure apenas no `.env` local ou no gerenciador de segredos do ambiente:

```dotenv
TELEGRAM_ENABLED=true
TELEGRAM_BOT_TOKEN=token-do-botfather
TELEGRAM_BOT_USERNAME=nome_do_seu_bot
```

Nunca versione o token. Ele não é retornado nas APIs da ISM, nem enviado ao frontend. O transporte não usa o logging padrão do HttpClientFactory, pois a API do Telegram exige o token na URL. Logs do worker registram somente o tipo de falha, sem URL, token ou texto das conversas.

Inicie os serviços com `docker compose -p ism up -d`. A migration `AddTelegramIntegration` e `AddTelegramExpectedUser` criam conexões, cursor de leitura e a verificação de identidade, preservando dados existentes. O backend registra o menu de comandos e a descrição do bot quando consegue autenticar. Credenciais inconsistentes ou webhook existente impedem o início do polling; não são removidos webhooks automaticamente.

Este modo usa long polling e não exige publicar localhost, instalar Telegram no Linux ou abrir uma porta de webhook. **Execute somente um backend com Telegram habilitado por bot**. Em uma implantação com várias réplicas, habilite o poller em apenas uma. O cursor é persistido para retomar após reinícios. O processamento das mensagens é sequencial; o timeout do provedor de IA também limita a espera da fila.

## Vínculo do usuário

1. Entre no ISM como gerente ou administrador do restaurante.
2. Abra **Integrações → Telegram**. Confira o ID do bot e informe seu ID de usuário; use `/id` no bot para consultar ambos. Clique em **Conectar Telegram**.
3. Abra o link no Telegram e pressione **Iniciar**. O link tem validade de cinco minutos, é de uso único e deve ser tratado como pessoal. Gerar outro invalida o anterior.
4. O ISM verifica automaticamente a conexão. Os alertas começam pausados e exigem ativação explícita.
5. Ative os alertas e use **Enviar teste pelo Telegram** para conferir o canal. O envio entra na mesma fila e histórico das demais notificações.
6. Para revogar o acesso, use **Desconectar Telegram** no ISM ou `/desvincular` no bot.

O ID do bot informado deve corresponder ao token configurado no servidor. O vínculo só pode ser confirmado pelo ID de usuário informado; digitar o ID não concede acesso por si só. O banco guarda somente o hash do código de vínculo. Chat e código têm índices únicos; a versão da conexão impede consumir o mesmo vínculo concorrentemente. O remetente precisa ser o dono do chat privado. A cada mensagem, a identidade, papel e restaurante são recarregados do banco. Desativação do usuário, troca de restaurante ou perda do papel gerencial encerram o vínculo. A identidade do worker vive em um escopo por mensagem e não altera a identidade de requisições HTTP.

## Comandos e conversa

| Comando | Resultado |
| --- | --- |
| `/ajuda`, `/help`, `/start` | Ajuda e orientação para conectar |
| `/pedidos` | Dez pedidos mais recentes do próprio restaurante |
| `/pedido 123` | Status, itens e total dos itens de um pedido |
| `/cardapio` | Até trinta pratos ativos e seus preços |
| `/alertas` | Dez alertas ativos mais recentes |
| `/id` | IDs do seu usuário e do bot, sem exigir conexão |
| `/agentes` | Orientação para consultar especialistas pelo orquestrador |
| `/conta` | Nome da conta, restaurante e preferência de alertas |
| `/notificar` | Ativa alertas futuros, conforme preferências do ISM |
| `/parar` | Pausa alertas, mantendo consultas disponíveis |
| `/nova` | Inicia outra conversa; não apaga o histórico anterior |
| `/desvincular` | Revoga vínculo e alertas |

Texto livre é enviado ao assistente já existente da ISM, com contexto autorizado do restaurante. A conversa e o histórico são persistidos por usuário. Não existem comandos para alterar estoque, despachar pedidos, efetuar pagamentos ou aprovar propostas. Sugestões e rascunhos do assistente continuam sujeitos à revisão no ISM.

A conversa livre exige um provedor de IA configurado e disponível. Sem ele, os comandos de consulta continuam funcionando e o assistente informa a indisponibilidade, sem inventar respostas. O bot aceita mensagens de texto de até mil caracteres; anexos recebem orientação de ajuda. O teclado interativo oferece Pedidos, Cardápio, Alertas, Agentes, Minha conta e Ajuda. Respostas extensas são divididas em mensagens; títulos e negrito são formatados com HTML escapado, sem executar HTML fornecido pelo modelo.

## IA local no Fedora

`ASSISTANT_AI_ENABLED=true`, `ASSISTANT_AI_MODEL` e `ASSISTANT_OLLAMA_BASE_URL` configuram o provedor padrão; a configuração de IA salva por restaurante tem precedência. Selecione um modelo que esteja instalado. Não é necessário baixar outro modelo só para o Telegram.

Se o Ollama nativo escuta apenas `127.0.0.1:11434`, o backend Docker não consegue usar esse loopback. Para desenvolvimento, há uma ponte opcional restrita ao IP privado do gateway Docker, que encaminha somente `/api/tags` e `/api/chat`, sem alterar o serviço Ollama:

```bash
python3 scripts/development/ollama_docker_bridge.py --bind 172.17.0.1
```

Confira antes o gateway da sua instalação; não use um endereço público ou `0.0.0.0`. O Compose já resolve `host.docker.internal`. Para manter a ponte durante a sessão do usuário no Fedora:

```bash
systemd-run --user --unit=ism-ollama-bridge --collect \
  /usr/bin/python3 "$PWD/scripts/development/ollama_docker_bridge.py" --bind 172.17.0.1
```

Pare com `systemctl --user stop ism-ollama-bridge`. Essa unidade é temporária e não configura inicialização após reboot. Em produção, use o provedor configurado para a rede do backend; não dependa dessa ponte de desenvolvimento.

## Alertas, privacidade e limites

Alertas novos de estoque, fornecedores e cardápio enfileiram entregas junto com a gravação do alerta. Os filtros individuais já existentes são respeitados. O worker confere vínculo, chat destinatário, papel, restaurante, opt-in e alerta não descartado antes de enviar. Desconexão ou desativação cancela entregas pendentes. Alertas antigos não são reenviados ao conectar.

Há até três tentativas, com espera crescente, e histórico em Configurações → Notificações. O teste pode ser repetido uma vez por minuto para o mesmo cenário. Aceitação pela API não significa leitura pelo usuário. Uma interrupção entre envio e persistência do resultado ainda pode repetir uma notificação; não há garantia de entrega exatamente uma vez.

Mensagens de entrada de uma conexão são marcadas antes de executar o assistente, para evitar repetir operações ao reiniciar. Em caso de falha após essa marcação, a resposta pode não chegar e o usuário precisará repetir a pergunta. Uma nova mensagem é limitada a cada dois segundos. O histórico das conversas fica no ISM; mensagens transmitidas também ficam sujeitas ao tratamento do Telegram. Desvincular encerra novos acessos, mas não apaga mensagens já entregues.

## APIs e testes

- `GET /api/me/telegram`: configuração e estado da própria conexão.
- `POST /api/me/telegram/link` com `{ "botId": 12345, "telegramUserId": 67890 }`: gera link temporário, com resposta sem cache e limite de requisições.
- `DELETE /api/me/telegram`: desconecta a própria conta.
- `PUT /api/me/telegram/alerts` com `{ "enabled": true }`: altera opt-in.
- `POST /api/me/notifications/test` com `{ "channel": "telegram", "scenario": "connectivity" }`: enfileira teste.
- Prévia e histórico usam as rotas de notificações existentes, também aceitando `telegram`.

Execute `dotnet test backend/ISM.sln`, incluindo `TelegramIntegrationTests`, e `npm run build` em `frontend`. Os testes de Telegram não usam tokens reais: verificam vínculo, hash, expiração, uso único, isolamento de restaurante, grupos, remetente incorreto, revogação, concorrência na gravação do vínculo, conversa persistente, limite de testes e alertas com opt-in/cancelamento.

Referências: [Bot API](https://core.telegram.org/bots/api), [deep links de bots](https://core.telegram.org/bots/features#deep-linking).

A tela de Integrações separa Conexões, Importar dados, Revisar importação (quando existe prévia) e Histórico. Conexões indisponíveis ficam recolhidas em Em breve; os ícones e a paleta Zinc/Indigo são preservados. As abas suportam navegação por setas, Home e End.

A conversa do Telegram usa `IAssistantService.SendMessageAsync`, que passa por `AssistantIntentCoordinator`, `IAgentIntentRouter` e `IAgentToolRegistry`. Nenhum endereço de agente local é fixado no canal Telegram; mover o transporte dos agentes para a nuvem deve preservar esses contratos. Perguntas financeiras recebem apresentação em tópicos e moeda brasileira, mantendo os valores retornados pelo agente.

## Validação da implementação — 25/11/2026

- Backend: 278 testes aprovados (213 unitários, 64 de integração e 1 de arquitetura), incluindo 15 casos específicos de Telegram.
- Frontend: 32 testes e script de autorização/navegação aprovados; build de produção concluído.
- Em tela: IDs inválidos/corretos, vínculo confirmado no Telegram, teclado interativo, cardápio, pedidos, detalhe de pedido, alertas, conversa com IA local e pergunta financeira roteada ao orquestrador.
- Alertas reais gerados a partir de um produto temporário chegaram pelo bot, além da mensagem de teste. O produto foi removido, os alertas de teste dispensados e o vínculo demo revogado.
- Nova tela: revisão de CSV de demonstração sem confirmar gravação, histórico, navegação por teclado e largura de celular.
- Token real permaneceu no `.env`, fora do Git. Os registros de conversa e entregas do teste permanecem no histórico da conta de demonstração.
