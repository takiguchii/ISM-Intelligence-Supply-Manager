using ISM.Application.Interfaces.Telegram;
using ISM.Domain.Interfaces;
using Microsoft.Extensions.Options;

namespace ISM.API.HostedServices;

public sealed class TelegramWorker(IServiceScopeFactory scopes, ITelegramTransport transport,
    IOptions<TelegramOptions> options, ILogger<TelegramWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.IsConfigured) return;
        var configured = false;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!configured) { await transport.ConfigureAsync(stoppingToken); configured = true; }
                using var scope = scopes.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<ITelegramRepository>();
                var offset = await repository.GetOffsetAsync(options.Value.BotUsername, stoppingToken);
                var updates = await transport.GetUpdatesAsync(offset, stoppingToken);
                foreach (var update in updates)
                {
                    string? reply;
                    try { reply = await scope.ServiceProvider.GetRequiredService<ITelegramService>().HandleAsync(update, stoppingToken); }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { throw; }
                    catch (Exception ex)
                    {
                        logger.LogWarning("Falha ao atender mensagem Telegram: {ErrorType}", ex.GetType().Name);
                        reply = update.ChatType == "private" && update.ChatId == update.SenderId ? "Não foi possível concluir sua solicitação. Tente novamente ou consulte o ISM." : null;
                    }
                    if (reply != null) await transport.SendAsync(update.ChatId, reply, stoppingToken);
                    using var cursorScope = scopes.CreateScope();
                    await cursorScope.ServiceProvider.GetRequiredService<ITelegramRepository>().SetOffsetAsync(options.Value.BotUsername, update.UpdateId + 1, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
            catch (Exception ex)
            {
                // Nunca registrar exceção/URL completa: podem conter o token do bot.
                logger.LogWarning("Falha no Telegram; nova tentativa em 10 segundos. Tipo: {ErrorType}", ex.GetType().Name);
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }
}
