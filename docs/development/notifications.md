# Notificações externas — SMTP e WhatsApp

O envio externo está implementado, mas **desativado e sem credenciais por padrão**. Nenhuma conta de email nem número de WhatsApp foi conectado nesta alteração.

## Configurar email SMTP

Preencha no `.env` local (não versionar credenciais):

```dotenv
SMTP_ENABLED=true
SMTP_HOST=smtp.seu-provedor.com
SMTP_PORT=587
SMTP_SECURITY=StartTls
SMTP_USERNAME=conta@seu-dominio.com
SMTP_PASSWORD=senha-ou-senha-de-aplicativo
SMTP_FROM_ADDRESS=conta@seu-dominio.com
SMTP_FROM_NAME=ISM
```

Use a autenticação que o provedor permitir para essa conta. Este cliente suporta usuário/senha SMTP, inclusive senha de aplicativo quando disponível; OAuth2 não está implementado. Para conexão TLS direta, use `SMTP_PORT=465` e `SMTP_SECURITY=SslOnConnect`. A opção `None` é destinada a servidores SMTP locais de teste.

Depois de alterar o `.env`:

```bash
docker compose up -d backend
```

Em **Configurações → Notificações**, ative email, informe o destinatário, salve e clique em **Enviar e-mail de teste**. O remetente precisa ser autorizado pelo provedor SMTP. A aceitação pelo SMTP não garante que a mensagem fique fora do spam ou chegue à caixa de entrada.

### Usar uma conta Gmail

1. Ative a verificação em duas etapas da conta remetente.
2. Crie uma senha de aplicativo com o nome `ISM` em https://myaccount.google.com/apppasswords.
3. Use `SMTP_HOST=smtp.gmail.com`, `SMTP_PORT=587` e `SMTP_SECURITY=StartTls`.
4. Preencha `SMTP_USERNAME` e `SMTP_FROM_ADDRESS` com o endereço completo da conta.
5. Cole a senha de aplicativo em `SMTP_PASSWORD` no `.env` local e defina `SMTP_ENABLED=true`.
6. Execute `docker compose up -d backend` e envie um teste pela tela de Notificações para um endereço real.

Não use a senha normal da conta Google e não inclua a senha de aplicativo no PR. Caso a opção de senhas de aplicativo não apareça, confira as restrições da conta na [documentação do Google](https://support.google.com/accounts/answer/185833?hl=pt-BR).

## WhatsApp por QR Code

A integração usa o serviço local WAHA, que conecta um dispositivo ao WhatsApp Web por QR Code. Não usa a API oficial da Meta. A sessão é compartilhada pelo servidor; para vários restaurantes independentes, é necessário provisionar um gateway por operação ou ampliar a integração para sessões isoladas. Por isso, conectar o remetente pela aplicação exige o papel **administrador da plataforma**, sem restaurante vinculado. O administrador do servidor também pode conectá-lo pelo dashboard local protegido.

Preencha no `.env`:

```dotenv
WHATSAPP_ENABLED=true
WHATSAPP_BASE_URL=http://whatsapp:3000
WHATSAPP_API_KEY=defina-uma-chave-aleatoria-longa
WHATSAPP_SESSION=default
WHATSAPP_PORT=3001
WHATSAPP_DASHBOARD_USERNAME=ism
WHATSAPP_DASHBOARD_PASSWORD=defina-uma-senha-local-forte
```

Você pode gerar uma chave local com `openssl rand -hex 32`. Não versione os valores reais.

Inicie o serviço opcional e recrie o backend para carregar as variáveis:

```bash
docker compose -f docker-compose.yml -f docker-compose.whatsapp.yml up -d whatsapp backend
```

1. Abra `http://localhost:3001/dashboard` e entre com o usuário e senha definidos acima.
2. Configure no dashboard a mesma API key. Crie/inicie a sessão chamada `default`.
3. No celular do remetente, abra **WhatsApp → Dispositivos conectados → Conectar dispositivo** e escaneie o QR Code da sessão.
4. Aguarde a sessão ficar `WORKING`.
5. No ISM, abra **Configurações → Notificações**, ative WhatsApp e informe o número **destinatário**, incluindo `+`, país e DDD, por exemplo `+5511999999999`.
6. Salve e clique em **Enviar WhatsApp de teste**.

O administrador da plataforma também pode iniciar a sessão, consultar seu status e visualizar o QR Code em **Configurações → Notificações**, sem expor a API key ao navegador. O dashboard do WAHA fica publicado apenas em `127.0.0.1`. O volume `whatsapp_sessions` preserva a conexão após recriar o container. A imagem Docker está fixada por digest; atualize-a conscientemente ao atualizar o gateway.

## Comportamento e limites

- Novos alertas de estoque, fornecedores e cardápio geram envios apenas para administradores/gerentes ativos do mesmo restaurante, conforme as preferências individuais.
- Os canais externos começam desativados para todos. Ativá-los não reenvia alertas antigos.
- O alerta e as entregas são persistidos na mesma gravação. O worker processa a fila a cada 15 segundos, sem bloquear o cadastro ou os agentes.
- São feitas até três tentativas, com intervalo crescente. Falhas de configuração aguardam sem consumir tentativas.
- Destinatário alterado, canal desativado, usuário inativo ou alerta dispensado cancelam envios ainda pendentes.
- O histórico mostra somente os envios da conta autenticada. Mensagens aceitas não são reenviadas nos próximos ciclos.
- Uma interrupção entre a aceitação pelo provedor e a gravação do resultado pode causar repetição. Não existe garantia de entrega exatamente uma vez, leitura ou entrega ao aparelho.
- O botão de teste envia somente para o destinatário salvo na própria conta, limitado a uma repetição do mesmo cenário e canal por minuto.
- Não há envio externo sem configurar o servidor e ativar o canal. Credenciais não são retornadas pelas APIs nem incluídas no histórico.

## Rotas

- `GET /api/me/notifications/channels`
- `GET /api/me/notifications/deliveries`
- `POST /api/me/notifications/test` com `{ "channel": "email" }` ou `{ "channel": "whatsapp" }`
- `GET /api/notifications/whatsapp/status` (administrador da plataforma)
- `POST /api/notifications/whatsapp/connect` (administrador da plataforma)
- `GET /api/notifications/whatsapp/qr` (administrador da plataforma, resposta PNG sem cache)

As preferências continuam em `GET/PUT /api/me/notification-preferences` e incluem `emailEnabled`, `emailAddress`, `whatsAppEnabled` e `whatsAppPhone`.

## Notas para a descrição do PR

- Inclui MailKit, fila persistente de notificações, worker com tentativas e histórico, integração WAHA por QR Code e configuração Docker opcional.
- Aplica a migration `AddExternalNotificationDelivery`, sem remover dados existentes.
- **Configuração pendente intencionalmente:** criar a conta remetente, preencher SMTP no `.env`, definir chave/senha do WAHA e conectar o número pelo QR Code.
- Não foram realizados envios a endereços/números reais. Testes cobrem uma conversa SMTP real contra servidor local, o contrato HTTP do WAHA, isolamento entre restaurantes, opt-in, cancelamento, tentativas e ausência de envio sem configuração.

Referências: [MailKit SMTP](https://mimekit.net/docs/html/T_MailKit_Net_Smtp_SmtpClient.htm), [WAHA — envio](https://waha.devlike.pro/docs/how-to/send-messages/), [WAHA — sessões](https://waha.devlike.pro/docs/how-to/sessions/), [WAHA — configuração](https://waha.devlike.pro/docs/how-to/config/).

## Formato das mensagens e apresentação

E-mails novos usam HTML com identidade do ISM, prioridade, resumo, indicadores, recomendação e link para a área correspondente. O SMTP inclui também a versão em texto para clientes sem HTML. WhatsApp usa texto curto, destaques, indicadores e a mesma recomendação. Os alertas automáticos usam os títulos e mensagens reais dos agentes; números dos exemplos não são inseridos em alertas reais.

Em **Configurações → Notificações → Cenários para apresentação**:

1. Escolha conexão, estoque baixo, risco de ruptura, aumento de preço, fornecedor alternativo ou margem do prato.
2. Confira a prévia de e-mail e a de WhatsApp. Nenhuma mensagem é enviada ao trocar a prévia.
3. Confira os destinatários, ative o canal disponível e clique em **Enviar cenário por e-mail** ou **Enviar cenário por WhatsApp**.
4. Acompanhe a fila e o recebimento. Todos os exemplos têm assunto `[TESTE]` e identificação **DEMONSTRAÇÃO • DADOS ILUSTRATIVOS**.
5. Para demonstrar dados atuais, selecione um item em **Alertas reais deste restaurante**. O reenvio será marcado como teste com origem real.

Os testes não criam alertas operacionais nem modificam estoque, compras, cardápio ou pedidos. Para mostrar a geração automática ponta a ponta, provoque uma condição real no cadastro de desenvolvimento e execute o agente correspondente; os envios automáticos usam os mesmos formatos. Registros de envios anteriores continuam preservados e não recebem HTML retroativamente.

Na apresentação, use primeiro estoque baixo (reposição), depois aumento de preço (decisão de compra) e margem do prato (impacto no cardápio). O histórico diferencia os envios de teste. É permitido enviar cenários diferentes em sequência; a repetição do mesmo cenário, alerta de origem e canal é limitada a um envio por minuto.

Os links das notificações apontam para `https://ism.altvia.cloud`, definido em `NotificationLinkService`. As rotas de estoque, fornecedores, cardápio e configurações são acrescentadas conforme o alerta. Não é necessário alterar variáveis de ambiente no servidor; para trocar o domínio, atualize esse serviço e publique o backend.

Rotas adicionais autenticadas:

- `GET /api/me/notifications/scenarios`: exemplos e até 20 alertas ativos do próprio restaurante.
- `POST /api/me/notifications/preview`: `{ "channel": "email", "scenario": "stock-low" }` devolve o conteúdo sem enfileirar envio.
- `POST /api/me/notifications/test`: aceita os mesmos campos. Para um alerta real, use `"scenario": "existing-alert"` e `"alertId": 123`.

Notas para o PR: migration `AddNotificationMessageTemplates`, HTML com alternativa textual, escape de conteúdo dinâmico, links configuráveis, prévia isolada em iframe sem scripts e cenários demonstrativos separados dos dados operacionais. A conexão do WhatsApp permanece desativada até vincular o número definitivo.
