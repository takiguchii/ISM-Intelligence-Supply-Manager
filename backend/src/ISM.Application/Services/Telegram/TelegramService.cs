using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using ISM.Application.Interfaces.Assistant;
using ISM.Application.Interfaces.Orders;
using ISM.Application.Interfaces.Telegram;
using ISM.Application.Security;
using ISM.Domain.Entities;
using ISM.Domain.Interfaces;
using ISM.Domain.Modules.Orders;
using Microsoft.Extensions.Options;

namespace ISM.Application.Services.Telegram;

public sealed class TelegramService(
    ITelegramRepository repository,
    IUserRepository users,
    ICurrentUser current,
    TelegramUserContext identity,
    IOptions<TelegramOptions> options,
    IAssistantService assistant,
    IRestaurantRepository restaurants,
    TimeProvider clock,
    ITelegramTransport transport) : ITelegramService
{
    private const string Help = "🤖 ISM • Assistente Orquestrador\n\nEnvie qualquer pergunta em linguagem natural sobre o restaurante (faturamento, estoque, cardápio, pedidos, etc.) e o assistente consultará os dados em tempo real.\n\nComandos rápidos:\n/nova — Reiniciar conversa\n/desvincular — Remover acesso\n/notificar — Ativar notificações e alertas\n/parar — Pausar notificações\n/ajuda — Ver este menu";

    private static bool Allowed(User? user, int restaurantId) =>
        user is { IsActive: true, Role: "Admin" or "Manager" }
        && (user.RestaurantId == restaurantId || user.Role == "Admin");

    private async Task<(User User, int RestaurantId)> RequireUserAsync(CancellationToken ct, int? targetRestaurantId = null)
    {
        var user = await users.GetByIdAsync(current.UserId, ct);
        if (current.IsSuperAdmin)
        {
            if (targetRestaurantId is not > 0 || user is not { IsActive: true, Role: "Admin" }
                || await restaurants.GetRestaurantByIdAsync(targetRestaurantId.Value, ct) is null)
                throw new UnauthorizedAccessException("Selecione um restaurante válido para configurar o Telegram.");
            return (user, targetRestaurantId.Value);
        }

        if (!current.IsManagerOrAbove || current.RestaurantId is not > 0
            || (targetRestaurantId.HasValue && targetRestaurantId != current.RestaurantId)
            || !Allowed(user, current.RestaurantId.Value))
            throw new UnauthorizedAccessException("A integração exige um gerente ou administrador do restaurante.");
        return (user!, current.RestaurantId.Value);
    }

    private static string Hash(string code) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(code)));

    public async Task<TelegramStatus> StatusAsync(CancellationToken ct, int? targetRestaurantId = null)
    {
        var (user, restaurantId) = await RequireUserAsync(ct, targetRestaurantId);
        var link = await repository.ForUserAsync(user.Id, ct);
        var belongsToRestaurant = link?.RestaurantId == restaurantId;
        var connected = belongsToRestaurant && (link?.ExpectedUserId != null || link?.ChatId != null);
        var username = options.Value.BotUsername;
        var botUrl = !string.IsNullOrWhiteSpace(username) ? $"https://t.me/{username}" : null;

        return new(
            options.Value.IsConfigured,
            connected,
            connected && (link?.AlertsEnabled ?? false),
            username,
            options.Value.ResolvedBotId,
            belongsToRestaurant ? (link!.ExpectedUserId ?? link.ChatId) : null,
            botUrl);
    }

    public async Task<TelegramStatus> SaveIdentityAsync(long telegramUserId, CancellationToken ct, int? targetRestaurantId = null)
    {
        var (user, restaurantId) = await RequireUserAsync(ct, targetRestaurantId);
        if (!options.Value.IsConfigured) throw new InvalidOperationException("Telegram ainda não configurado no servidor.");
        if (telegramUserId <= 0) throw new InvalidOperationException("O administrador precisa informar um ID de usuário válido do Telegram.");

        var existingIdentity = await repository.ForTelegramUserIdAsync(telegramUserId, ct);
        if (existingIdentity is not null && existingIdentity.UserId != user.Id)
            throw new InvalidOperationException("Este ID do Telegram já está vinculado a outra conta ou restaurante.");

        var connection = await repository.ForUserAsync(user.Id, ct) ?? new() { UserId = user.Id };
        connection.RestaurantId = restaurantId;
        connection.ExpectedUserId = telegramUserId;
        connection.ChatId = telegramUserId; // Para chat privado no Telegram, ChatId == UserId
        connection.CodeHash = null;
        connection.CodeExpiresAtUtc = null;
        connection.AlertsEnabled = true;

        await repository.SaveAsync(connection, ct);
        return await StatusAsync(ct, targetRestaurantId);
    }

    public async Task<TestTelegramResult> TestConnectionAsync(CancellationToken ct, int? targetRestaurantId = null)
    {
        var (user, restaurantId) = await RequireUserAsync(ct, targetRestaurantId);
        if (!options.Value.IsConfigured)
            return new(false, "O bot do Telegram não está configurado no servidor.");

        var link = await repository.ForUserAsync(user.Id, ct);
        if (link is null || (!link.ChatId.HasValue && !link.ExpectedUserId.HasValue))
            return new(false, "Nenhum ID de Telegram salvo para este usuário. Salve o ID antes de testar a conexão.");

        var targetChatId = link.ChatId ?? link.ExpectedUserId!.Value;
        var testMessage = "🤖 <b>ISM Bot • Teste de Conexão</b>\n\nConexão estabelecida com sucesso!\n\nSeu ID foi autorizado no ISM. A partir de agora, qualquer mensagem ou dúvida enviada por aqui será respondida pelo Assistente Orquestrador em tempo real.";

        try
        {
            await transport.SendAsync(targetChatId, testMessage, ct);
            return new(true, "Mensagem de teste enviada com sucesso para o seu Telegram! Verifique a conversa com o bot.");
        }
        catch (Exception)
        {
            var username = options.Value.BotUsername;
            return new(false, $"Não foi possível entregar a mensagem no Telegram. Certifique-se de que você já abriu a conversa com o bot (@{username}) e clicou em 'Iniciar' (/start).");
        }
    }

    public async Task<TelegramLink> CreateLinkAsync(CancellationToken ct, long? botId = null, long? telegramUserId = null, int? targetRestaurantId = null)
    {
        if (telegramUserId.HasValue && telegramUserId > 0)
        {
            await SaveIdentityAsync(telegramUserId.Value, ct, targetRestaurantId);
        }
        var username = options.Value.BotUsername;
        var url = !string.IsNullOrWhiteSpace(username) ? $"https://t.me/{username}" : "";
        return new(url, DateTime.UtcNow.AddYears(1));
    }

    public async Task DisconnectAsync(CancellationToken ct, int? targetRestaurantId = null)
    {
        var (user, restaurantId) = await RequireUserAsync(ct, targetRestaurantId);
        var link = await repository.ForUserAsync(user.Id, ct);
        if (link?.RestaurantId == restaurantId)
        {
            Clear(link);
            await repository.SaveAsync(link, ct);
        }
    }

    private static void Clear(TelegramConnection link)
    {
        link.ExpectedUserId = null;
        link.ChatId = null;
        link.CodeHash = null;
        link.CodeExpiresAtUtc = null;
        link.AlertsEnabled = false;
        link.ThreadId = null;
        link.LastMessageAtUtc = null;
    }

    public async Task SetAlertsAsync(bool enabled, CancellationToken ct, int? targetRestaurantId = null)
    {
        var (user, restaurantId) = await RequireUserAsync(ct, targetRestaurantId);
        var link = await repository.ForUserAsync(user.Id, ct);
        if (link?.ChatId == null || link.RestaurantId != restaurantId) throw new InvalidOperationException("Conecte seu Telegram primeiro.");
        link.AlertsEnabled = enabled;
        await repository.SaveAsync(link, ct);
    }

    public async Task<string?> HandleAsync(TelegramIncoming message, CancellationToken ct)
    {
        // Grupos e mensagens enviadas por terceiros nunca recebem dados do restaurante.
        if (message.ChatType != "private" || message.ChatId != message.SenderId) return null;
        var text = (message.Text ?? "").Trim();

        // 1. Busca conexão pelo ChatId ou pelo ExpectedUserId salvo
        var link = await repository.ForChatAsync(message.ChatId, ct)
                ?? await repository.ForTelegramUserIdAsync(message.SenderId, ct);

        // Suporte legado se receber /start com código
        var split = text.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var command = split.FirstOrDefault()?.Split('@')[0].ToLowerInvariant() ?? "";
        var parameter = split.Length > 1 ? split[1].Trim() : "";

        if (link == null && command == "/start" && parameter.Length == 48)
        {
            link = await repository.ForCodeAsync(Hash(parameter), ct);
            var u = link == null ? null : await users.GetByIdAsync(link.UserId, ct);
            if (link != null && link.CodeExpiresAtUtc > clock.GetUtcNow().UtcDateTime && Allowed(u, link.RestaurantId))
            {
                link.ChatId = message.ChatId;
                link.ExpectedUserId = message.SenderId;
                link.CodeHash = null;
                link.CodeExpiresAtUtc = null;
                link.LastUpdateId = message.UpdateId;
                await repository.SaveAsync(link, ct);
            }
            else
            {
                link = null;
            }
        }

        // Se o usuário ainda não foi cadastrado no ISM:
        if (link == null)
        {
            var integrationsUrl = Uri.TryCreate(options.Value.IsmUrl, UriKind.Absolute, out var ismUri)
                && (ismUri.Scheme == Uri.UriSchemeHttp || ismUri.Scheme == Uri.UriSchemeHttps)
                    ? new Uri(ismUri, "/integracoes").ToString()
                    : null;

            return integrationsUrl is null
                ? $"Olá! Sou o assistente da ISM.\n\nSeu ID do Telegram é: <code>{message.SenderId}</code>\n\nPeça ao administrador do seu restaurante para autorizar seu ID do Telegram em Integrações → Telegram. Somente o administrador pode criar o vínculo; até lá, não consigo consultar dados. Nunca envie sua senha aqui."
                : $"Olá! Sou o assistente da ISM.\n\nSeu ID do Telegram é: <code>{message.SenderId}</code>\n\nPara solicitar acesso, peça ao administrador do seu restaurante que autorize seu ID do Telegram em Integrações → Telegram. Somente o administrador pode criar o vínculo; até lá, não consigo consultar dados.\n{integrationsUrl}\nNunca envie sua senha aqui.";
        }

        // Garante que ChatId e ExpectedUserId estejam memorizados e preservados
        link.ChatId = message.ChatId;
        link.ExpectedUserId = message.SenderId;

        if (message.UpdateId <= link.LastUpdateId) return null;

        var owner = await users.GetByIdAsync(link.UserId, ct);
        if (!Allowed(owner, link.RestaurantId))
        {
            Clear(link);
            await repository.SaveAsync(link, ct);
            return "A conexão foi encerrada porque seu acesso ao restaurante mudou. Consulte o administrador.";
        }

        identity.Set(owner!.Id, link.RestaurantId, owner.Role);

        if (link.LastMessageAtUtc > clock.GetUtcNow().UtcDateTime.AddSeconds(-2))
            return "Aguarde alguns segundos antes da próxima mensagem.";

        if (text.Length > 2000) return "Envie uma pergunta de até 2.000 caracteres.";

        link.LastMessageAtUtc = clock.GetUtcNow().UtcDateTime;
        link.LastUpdateId = message.UpdateId;
        await repository.SaveAsync(link, ct);

        // Tratamento de comandos de controle
        switch (command)
        {
            case "/start":
                return "🤖 <b>Olá! Sua conta está conectada ao ISM.</b>\n\n" +
                       "Eu sou o seu <b>Assistente Orquestrador</b> inteligente.\n" +
                       "Você pode me perguntar qualquer coisa sobre o seu restaurante diretamente por aqui (ex.: <i>'Qual o faturamento deste mês?'</i>, <i>'Como está o estoque de salmão?'</i>, <i>'Quais pratos mais vendem?'</i>).\n\n" +
                       "• Use /nova para reiniciar o histórico da conversa.\n" +
                       "• Use /ajuda para ver as instruções.\n\n" +
                       "Como posso ajudar você agora?";

            case "/help":
            case "/ajuda":
                return Help;

            case "/desvincular":
                Clear(link);
                await repository.SaveAsync(link, ct);
                return "Conta desconectada. Nenhum novo alerta ou mensagem será enviado a este Telegram.";

            case "/notificar":
            case "/parar":
                link.AlertsEnabled = command == "/notificar";
                await repository.SaveAsync(link, ct);
                return link.AlertsEnabled
                    ? "Alertas ativados conforme suas preferências de estoque, fornecedores e cardápio no ISM."
                    : "Alertas pausados. As consultas continuam disponíveis.";

            case "/nova":
                link.ThreadId = null;
                await repository.SaveAsync(link, ct);
                return "Conversa reiniciada. Como posso ajudar?";
        }

        // REDIRECIONAMENTO DIRETO PARA O ASSISTENTE ORQUESTRADOR
        if (link.ThreadId == null)
        {
            var thread = await assistant.CreateThreadAsync(new(Subject: "Telegram • conversa ISM"), ct);
            if (thread == null) return "Não foi possível iniciar a conversa. Tente novamente.";
            link.ThreadId = thread.Thread.Id;
            await repository.SaveAsync(link, ct);
        }

        var conversation = await assistant.SendMessageAsync(link.ThreadId.Value, text, ct);
        var answer = conversation?.Messages.LastOrDefault(m => m.Role == "assistant");
        if (answer == null) return "Não foi possível responder agora. Tente novamente ou use os comandos de consulta.";

        if (answer.Source != "Dados financeiros do ISM") return answer.Content;

        var formatted = Regex.Replace(answer.Content, @"R\$ (-?\d+(?:\.\d+)?)", match => Money(decimal.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture)));
        return formatted.Replace("Faturamento do mês, baseado em pedidos pagos:", "**Financeiro • mês atual**\n\n• Pedidos pagos:")
            .Replace(". Outras receitas liquidadas:", "\n• Outras receitas liquidadas:")
            .Replace(". Despesas liquidadas:", "\n• Despesas liquidadas:")
            .Replace(". Contas a receber:", "\n• Contas a receber:")
            .Replace(". Contas a pagar:", "\n• Contas a pagar:")
            .Replace(". Fonte:", "\n\nFonte:");
    }

    private static string Money(decimal value) => value.ToString("C", CultureInfo.GetCultureInfo("pt-BR"));
}
