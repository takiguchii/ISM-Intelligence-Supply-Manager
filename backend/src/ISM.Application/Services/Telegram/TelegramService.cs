using System.Text.RegularExpressions;
using ISM.Domain.Modules.Orders;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using ISM.Application.Interfaces.Telegram;
using ISM.Application.Interfaces.Assistant;
using ISM.Application.Interfaces.Orders;
using ISM.Application.Security;
using ISM.Domain.Entities;
using ISM.Domain.Interfaces;
using Microsoft.Extensions.Options;

namespace ISM.Application.Services.Telegram;

public sealed class TelegramService(ITelegramRepository repository, IUserRepository users, ICurrentUser current,
    TelegramUserContext identity, IOptions<TelegramOptions> options, IAssistantService assistant,
    IOrderService orders, IDishRepository dishes, ISystemAlertRepository alerts, IRestaurantRepository restaurants, TimeProvider clock) : ITelegramService
{
    private const string Help = "ISM • Assistente\n\n/estoque [produto] — consultar estoque\n/financeiro — resumo financeiro\n/pedidos — últimos pedidos\n/pedido 123 — detalhes do pedido\n/cardapio — pratos disponíveis\n/alertas — alertas ativos\n/notificar — ativar alertas\n/parar — pausar alertas\n/nova — reiniciar conversa\n/desvincular — remover acesso\n/ajuda — este menu\n\nOu escreva sua pergunta normalmente para conversar com o assistente.";
    private static bool Allowed(User? user, int restaurantId) => user is { IsActive: true, Role: "Admin" or "Manager" }
        && (user.RestaurantId == restaurantId || (user.Role == "Admin" && !user.RestaurantId.HasValue));
    private async Task<(User User, int RestaurantId)> RequireUserAsync(CancellationToken ct, int? targetRestaurantId = null)
    {
        var user = await users.GetByIdAsync(current.UserId, ct);
        if (current.IsSuperAdmin)
        {
            if (targetRestaurantId is not > 0 || user is not { IsActive: true, RestaurantId: null, Role: "Admin" }
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
        var connected = belongsToRestaurant && link?.ChatId != null;
        return new(options.Value.IsConfigured, connected, connected && link!.AlertsEnabled, options.Value.BotUsername, options.Value.ResolvedBotId,
            belongsToRestaurant ? link!.ChatId ?? link.ExpectedUserId : null);
    }
    public async Task<TelegramLink> CreateLinkAsync(CancellationToken ct, long? botId = null, long? telegramUserId = null, int? targetRestaurantId = null)
    {
        var (user, restaurantId) = await RequireUserAsync(ct, targetRestaurantId);
        if (!options.Value.IsConfigured) throw new InvalidOperationException("Telegram ainda não configurado no servidor.");
        if (botId.HasValue && botId != options.Value.ResolvedBotId) throw new InvalidOperationException("O ID do bot não corresponde ao bot configurado no servidor.");
        if (!telegramUserId.HasValue || telegramUserId <= 0) throw new InvalidOperationException("O administrador precisa informar um ID de usuário válido do Telegram.");
        var connection = await repository.ForUserAsync(user.Id, ct) ?? new() { UserId = user.Id };
        if (connection.RestaurantId != 0 && connection.RestaurantId != restaurantId
            && (connection.ChatId.HasValue || connection.ExpectedUserId.HasValue || connection.CodeHash is not null))
            throw new InvalidOperationException("Esta conta Telegram já está vinculada a outro restaurante. Desconecte o vínculo atual antes de alterá-lo.");
        if (connection.ChatId.HasValue) throw new InvalidOperationException("Desconecte o Telegram atual antes de conectar outra conta.");
        var existingIdentity = await repository.ForTelegramUserIdAsync(telegramUserId.Value, ct);
        if (existingIdentity is not null && existingIdentity.UserId != user.Id)
            throw new InvalidOperationException("Este ID do Telegram já está vinculado a outra conta ou restaurante.");
        var code = Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant();
        connection.RestaurantId = restaurantId;
        connection.ExpectedUserId = telegramUserId;
        connection.CodeHash = Hash(code);
        connection.CodeExpiresAtUtc = clock.GetUtcNow().UtcDateTime.AddMinutes(5);
        await repository.SaveAsync(connection, ct);
        return new($"https://t.me/{options.Value.BotUsername}?start={code}", connection.CodeExpiresAtUtc.Value);
    }
    public async Task DisconnectAsync(CancellationToken ct, int? targetRestaurantId = null)
    {
        var (user, restaurantId) = await RequireUserAsync(ct, targetRestaurantId);
        var link = await repository.ForUserAsync(user.Id, ct);
        if (link?.RestaurantId == restaurantId) { Clear(link); await repository.SaveAsync(link, ct); }
    }
    private static void Clear(TelegramConnection link)
    {
        link.ExpectedUserId = null; link.ChatId = null; link.CodeHash = null; link.CodeExpiresAtUtc = null;
        link.AlertsEnabled = false; link.ThreadId = null; link.LastMessageAtUtc = null;
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
        var text = message.Text.Trim();
        text = text.ToLowerInvariant() switch { "pedidos" => "/pedidos", "cardápio" => "/cardapio", "alertas" => "/alertas", "ajuda" => "/ajuda", _ => text };
        var split = text.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var command = split.FirstOrDefault()?.Split('@')[0].ToLowerInvariant() ?? "";
        var parameter = split.Length > 1 ? split[1].Trim() : "";
        var link = await repository.ForChatAsync(message.ChatId, ct);
        if (command == "/start" && parameter.Length > 0)
        {
            if (link != null) return "Este Telegram já está conectado. Use /desvincular antes de trocar de conta.";
            if (parameter.Length != 48) return "Vínculo inválido. Gere outro link em Integrações → Telegram.";
            link = await repository.ForCodeAsync(Hash(parameter), ct);
            var user = link == null ? null : await users.GetByIdAsync(link.UserId, ct);
            if (link == null || link.ChatId != null || (!link.CodeExpiresAtUtc.HasValue || link.CodeExpiresAtUtc <= clock.GetUtcNow().UtcDateTime) || !Allowed(user, link.RestaurantId))
                return "Este link expirou ou não é válido. Gere outro no ISM.";
            if (link.ExpectedUserId.HasValue && link.ExpectedUserId != message.SenderId)
                return "Este vínculo foi gerado para outro ID de usuário do Telegram. Confira o ID no ISM; seu código não foi consumido.";
            link.ChatId = message.ChatId; link.CodeHash = null; link.CodeExpiresAtUtc = null;
            link.LastUpdateId = message.UpdateId; link.ThreadId = null; link.AlertsEnabled = false;
            await repository.SaveAsync(link, ct);
            return "Conta ISM conectada! Os alertas começam pausados; ative com /notificar ou no sistema.\n\n" + Help;
        }
        if (link == null)
        {
            var pending = await repository.ForTelegramUserIdAsync(message.SenderId, ct);
            var pendingOwner = pending == null ? null : await users.GetByIdAsync(pending.UserId, ct);
            if (pending is not null && pending.ExpectedUserId == message.SenderId
                && pending.ChatId is null && Allowed(pendingOwner, pending.RestaurantId))
            {
                pending.ChatId = message.ChatId;
                pending.ExpectedUserId = null;
                pending.CodeHash = null;
                pending.CodeExpiresAtUtc = null;
                pending.ThreadId = null;
                await repository.SaveAsync(pending, ct);
                link = pending;
            }
        }
        if (link == null)
        {
            var integrationsUrl = Uri.TryCreate(options.Value.IsmUrl, UriKind.Absolute, out var ismUri)
                && (ismUri.Scheme == Uri.UriSchemeHttp || ismUri.Scheme == Uri.UriSchemeHttps)
                    ? new Uri(ismUri, "/integracoes").ToString()
                    : null;
            return integrationsUrl is null
                ? "Olá! Sou o assistente da ISM. Peça ao administrador do seu restaurante para autorizar seu ID do Telegram em Integrações → Telegram. Somente o administrador pode criar o vínculo; até lá, não consigo consultar dados. Nunca envie sua senha aqui."
                : $"Olá! Sou o assistente da ISM. Para solicitar acesso, peça ao administrador do seu restaurante que autorize seu ID do Telegram em Integrações → Telegram. Somente o administrador pode criar o vínculo; até lá, não consigo consultar dados.\n{integrationsUrl}\nNunca envie sua senha aqui.";
        }
        if (message.UpdateId <= link.LastUpdateId) return null;
        var owner = await users.GetByIdAsync(link.UserId, ct);
        if (!Allowed(owner, link.RestaurantId))
        {
            Clear(link); await repository.SaveAsync(link, ct);
            return "A conexão foi encerrada porque seu acesso ao restaurante mudou. Consulte o administrador.";
        }
        identity.Set(owner!.Id, link.RestaurantId, owner.Role);
        if (link.LastMessageAtUtc > clock.GetUtcNow().UtcDateTime.AddSeconds(-2))
            return "Aguarde alguns segundos antes da próxima mensagem.";
        if (text.Length > 1000) return "Envie uma pergunta de até 1.000 caracteres.";
        link.LastMessageAtUtc = clock.GetUtcNow().UtcDateTime;
        // Marca a entrada antes de executar o assistente: evita repetir ações após reinício.
        link.LastUpdateId = message.UpdateId;
        await repository.SaveAsync(link, ct);
        switch (command)
        {
            case "/start": case "/help": case "/ajuda": return Help;
            case "/desvincular": Clear(link); await repository.SaveAsync(link, ct); return "Conta desconectada. Nenhum novo alerta será enviado a este Telegram.";
            case "/notificar": case "/parar":
                link.AlertsEnabled = command == "/notificar"; await repository.SaveAsync(link, ct);
                return link.AlertsEnabled ? "Alertas ativados conforme suas preferências de estoque, fornecedores e cardápio no ISM." : "Alertas pausados. As consultas continuam disponíveis.";
            case "/nova": link.ThreadId = null; await repository.SaveAsync(link, ct); return "Conversa reiniciada. Como posso ajudar?";
            case "/pedidos":
                var list = await orders.GetAdminPageAsync(1, 10, null, ct);
                return list.Items.Count == 0 ? "Nenhum pedido encontrado." : "Últimos pedidos\n" + string.Join("\n", list.Items.Select(o => $"#{o.Id} • {o.TableReference} • {Status(o.Status)}")) + "\nUse /pedido NUMERO para detalhes.";
            case "/pedido":
                if (!int.TryParse(parameter, out var id) || id <= 0) return "Use /pedido 123.";
                var order = await orders.GetByIdAsync(id, ct);
                return order == null ? "Pedido não encontrado neste restaurante." : $"Pedido #{order.Id} • {order.TableReference}\nStatus: {Status(order.Status)}\n" + string.Join("\n", order.Items.Select(i => $"{i.Quantity}× {i.DishName}")) + $"\nTotal dos itens: {Money(order.Items.Sum(i => i.UnitPrice * i.Quantity))}";
            case "/cardapio":
                var menu = (await dishes.GetAllDishesAsync(link.RestaurantId, cancellationToken: ct)).Where(d => d.IsActive).OrderBy(d => d.DisplayOrder).ToList();
                return menu.Count == 0 ? "Nenhum prato disponível." : "Cardápio\n" + string.Join("\n", menu.Take(30).Select(d => $"{d.Name} • {Money(d.Price)}")) + (menu.Count > 30 ? "\nConfira o cardápio completo no ISM." : "");
            case "/alertas":
                var page = await alerts.GetPagedAsync(link.RestaurantId, 1, 10, isDismissed: false, cancellationToken: ct);
                return page.Items.Count == 0 ? "Nenhum alerta ativo." : "Alertas do restaurante\n" + string.Join("\n\n", page.Items.Select(a => $"{a.Title}\n{a.Message}"));
            case "/estoque":
                text = string.IsNullOrWhiteSpace(parameter)
                    ? "Consulte o estoque e informe quantos produtos estão abaixo do mínimo."
                    : $"Qual é o saldo do produto {parameter}?";
                command = "";
                break;
            case "/financeiro":
                text = "Resuma o faturamento, as receitas e as despesas do mês atual.";
                command = "";
                break;
        }
        if (command.StartsWith('/')) return "Comando não reconhecido. Use /ajuda ou escreva sua pergunta.";
        if (link.ThreadId == null)
        {
            var thread = await assistant.CreateThreadAsync(new(Subject: "Telegram • conversa ISM"), ct);
            if (thread == null) return "Não foi possível iniciar a conversa. Tente novamente.";
            link.ThreadId = thread.Thread.Id; await repository.SaveAsync(link, ct);
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
    private static string Status(OrderStatus status) => status switch
    {
        OrderStatus.Submitted => "Recebido", OrderStatus.Preparing => "Em preparo", OrderStatus.Ready => "Pronto",
        OrderStatus.Completed => "Entregue", OrderStatus.Paid => "Pago", OrderStatus.Cancelled => "Cancelado", _ => "Desconhecido"
    };
    private static string Money(decimal value) => value.ToString("C", CultureInfo.GetCultureInfo("pt-BR"));
}
