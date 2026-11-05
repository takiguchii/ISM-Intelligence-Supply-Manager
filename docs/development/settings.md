# Configurações

A página `/configuracoes` usa APIs autenticadas e persiste os dados no banco selecionado pela branch. As migrations `AddAccountAndRestaurantSettings` e `AddAccountAppearancePreferences` preservam os cadastros existentes.

## Funcionalidades

- **Geral:** nome, CNPJ, endereço, telefone, contato, preferências regionais, horários, logo e taxas de referência. Administradores e gerentes editam somente seu restaurante. O administrador da plataforma usa os endpoints com ID explícito.
- **Aparência:** tema, densidade de tabelas, escala de fonte, contraste e redução de animações salvos por usuário e aplicados ao entrar. O botão de tema também salva a preferência na conta.
- **Notificações:** preferências individuais de estoque, fornecedores e cardápio filtram a Central de Notificações. O envio externo de email e WhatsApp pode ser ativado após configurar os serviços; veja [Notificações externas](notifications.md).
- **Segurança:** troca de senha com verificação da senha atual, registro de sessões, revogação individual e autenticação TOTP com oito códigos de recuperação de uso único. Trocar a senha ou ativar o 2FA encerra as sessões. Após ativar o 2FA, aguarde o próximo código do autenticador ou use um código de recuperação para entrar.
- **Assinatura:** consulta do plano e alteração demonstrativa com histórico persistente. Não processa pagamentos e não recebe dados de cartão. As mudanças são permitidas em Development ou quando `Billing:DemoEnabled=true`. A troca respeita os limites do plano.

Moeda, idioma, taxas e horários são metadados do estabelecimento: não convertem preços, não traduzem a interface e não alteram automaticamente o fluxo de pedidos. Integrações com marketplaces e serviços de envio exigem implementação e credenciais próprias.

## Rotas

- `GET/PUT /api/me/restaurant/profile`
- `GET/PUT /api/restaurants/{restaurantId}/profile` (administrador da plataforma)
- `GET/PUT /api/me/appearance`
- `GET/PUT /api/me/notification-preferences`
- `POST /api/auth/change-password`
- `GET /api/auth/sessions`
- `DELETE /api/auth/sessions/{id}`
- `GET /api/auth/two-factor`
- `POST /api/auth/two-factor/setup`, `/enable` e `/disable`
- `GET /api/me/restaurant/subscription`
- `POST /api/me/restaurant/subscription/plan`
- `GET /api/restaurants/{restaurantId}/subscription` e `POST /api/restaurants/{restaurantId}/subscription/plan` (administrador da plataforma)

A confirmação do 2FA exige o código correto antes de ativar. A chave é protegida com ASP.NET Core Data Protection; os códigos de recuperação são armazenados somente como hashes. O volume Docker `data_protection_keys` preserva as chaves entre recriações do container. Preserve esse volume junto aos backups do banco para continuar validando autenticadores já cadastrados.

Novos JWTs incluem uma sessão persistente, verificada em toda requisição autenticada. Tokens anteriores à migração continuam válidos até expirar, mas são recusados após troca de senha ou ativação do 2FA. Para aparecer na lista de sessões, saia e entre novamente. Contas com 2FA ativo não podem usar o atalho de token de desenvolvimento.

## Validação

```bash
docker compose exec -T backend dotnet test ISM.sln
docker compose exec -T frontend npm run build
```

`SettingsFlowTests` cobre persistência, validação, isolamento de permissões, aparência, notificações, revogação de sessão, senha, TOTP, recuperação e assinatura demonstrativa usando banco isolado em memória.
