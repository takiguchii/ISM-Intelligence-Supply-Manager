using System.Globalization;
using System.Net;
using ISM.Application.DTOs.Notifications;
using ISM.Domain.Modules.System.Entities;
using ISM.Domain.Modules.System.Enums;

namespace ISM.Application.Services.Notifications;

public static class NotificationMessageFormatter
{
    public static readonly IReadOnlyList<NotificationScenario> Scenarios = new NotificationScenario[]
    {
        new("connectivity", "Teste de conexão", "Confirma o recebimento no canal escolhido."),
        new("stock-low", "Estoque abaixo do mínimo", "Salmão com 6 kg disponíveis e mínimo de 15 kg."),
        new("stock-risk", "Risco de ruptura em 48 horas", "Consumo médio indica estoque para apenas 1,5 dia."),
        new("supplier-price", "Aumento no preço do fornecedor", "Preço de compra sobe de R$ 50,00 para R$ 60,00 (+20%)."),
        new("supplier-alternative", "Oportunidade de economia", "Outro fornecedor oferece o mesmo insumo por um preço menor."),
        new("dish-margin", "Margem do prato abaixo da meta", "O custo dos ingredientes compromete a margem do prato.")
    };

    private sealed record Content(string Id, string Category, string Priority, string Title, string Summary,
        (string Label, string Value)[] Facts, string Recommendation, string Action, string Path);

    private static Content Example(string id) => id switch
    {
        "connectivity" => new(id, "Conectividade", "Informativo", "Teste de conexão do ISM",
            "Esta mensagem foi solicitada na tela de Notificações para verificar o recebimento neste canal.",
            [], "Confirme o recebimento e acompanhe os próximos alertas do restaurante.", "Abrir configurações", "/configuracoes#notifications"),
        "stock-low" => new(id, "Estoque", "Atenção", "Reposição necessária: salmão abaixo do mínimo",
            "O estoque de segurança já foi comprometido. Sem reposição, a operação fica mais exposta à falta deste ingrediente.",
            [("Produto", "Salmão fresco"), ("Saldo atual", "6 kg"), ("Estoque mínimo", "15 kg"), ("Reposição até o mínimo", "9 kg")],
            "Confira o saldo físico agora e confirme a compra dos 9 kg necessários para recuperar o estoque mínimo.", "Consultar estoque", "/estoque"),
        "stock-risk" => new(id, "Estoque", "Crítico", "AÇÃO IMEDIATA: arroz pode acabar em 1,5 dia",
            "O arroz japonês pode acabar antes da próxima entrega. Se o consumo continuar neste ritmo, pratos que dependem dele poderão ficar indisponíveis e vendas poderão ser perdidas.",
            [("Produto", "Arroz japonês"), ("Saldo atual", "12 kg"), ("Consumo médio", "8 kg/dia"), ("Cobertura estimada", "1,5 dia")],
            "Acione o fornecedor agora, confirme uma entrega antes de o estoque acabar e avalie uma compra emergencial se o prazo não atender.", "Consultar estoque", "/estoque"),
        "supplier-price" => new(id, "Fornecedores", "Atenção", "Custo em alta: salmão ficou 20% mais caro",
            "A próxima compra pode pressionar o caixa e reduzir a margem dos pratos com salmão. O aumento precisa ser avaliado antes de fechar o pedido.",
            [("Fornecedor", "Fornecedor Oceano (exemplo)"), ("Produto", "Salmão fresco"), ("Preço de referência", "R$ 50,00/kg"), ("Preço recente", "R$ 60,00/kg"), ("Variação", "+20%")],
            "Compare cotações antes de confirmar a próxima compra e avalie o impacto nos custos.", "Consultar fornecedores", "/fornecedores"),
        "supplier-alternative" => new(id, "Fornecedores", "Oportunidade", "Alternativa de compra com menor custo",
            "Uma cotação alternativa apresenta economia para a mesma quantidade de arroz japonês.",
            [("Quantidade comparada", "10 kg"), ("Cotação atual", "R$ 120,00"), ("Cotação alternativa", "R$ 105,00"), ("Economia estimada", "R$ 15,00 (12,5%)")],
            "Valide qualidade, frete e prazo de entrega antes de trocar de fornecedor.", "Comparar fornecedores", "/fornecedores"),
        "dish-margin" => new(id, "Cardápio", "Atenção", "Margem em queda: combinado abaixo da meta",
            "Cada venda está gerando uma margem menor que a planejada. Mantido esse custo, o resultado do restaurante pode ficar abaixo da meta.",
            [("Prato", "Combinado da casa (exemplo)"), ("Preço de venda", "R$ 100,00"), ("Custo dos ingredientes", "R$ 45,00"), ("CMV", "45%"), ("Margem bruta", "55%"), ("Meta de margem", "65%")],
            "Revise a ficha técnica, as compras e o preço de venda antes de ajustar o cardápio.", "Revisar cardápio", "/cardapio"),
        _ => throw new InvalidOperationException("Cenário de notificação inválido.")
    };

    public static string ScenarioFor(SystemAlertType type) => type switch
    {
        SystemAlertType.StockBelowMinimum => "stock-low",
        SystemAlertType.StockRuptureRisk48h => "stock-risk",
        SystemAlertType.SupplierPriceSpike15Percent => "supplier-price",
        SystemAlertType.SupplierBetterAlternative => "supplier-alternative",
        SystemAlertType.DishCmvDefasagem => "dish-margin",
        _ => "connectivity"
    };

    public static NotificationPreview Render(string scenario, string channel, string restaurantName,
        string timeZone, DateTime occurredAtUtc, string publicAppUrl, bool isTest, SystemAlert? alert = null)
    {
        if (channel is not ("email" or "whatsapp")) throw new InvalidOperationException("Canal inválido.");
        var content = Example(alert == null ? scenario : ScenarioFor(alert.AlertType));
        if (alert != null) content = content with
        {
            Title = Line(alert.Title), Summary = alert.Message, Facts = [],
            Recommendation = alert.AlertType == SystemAlertType.StockBelowMinimum
                ? "Confira o saldo físico agora e confirme a reposição necessária para recuperar o estoque mínimo."
                : content.Recommendation,
            Priority = alert.Severity switch { AlertSeverity.Danger => "Crítico", AlertSeverity.Warning => "Atenção", _ => "Informativo" }
        };
        if (!Uri.TryCreate(publicAppUrl.TrimEnd('/') + content.Path, UriKind.Absolute, out var uri) ||
            uri.Scheme is not ("http" or "https") || !string.IsNullOrEmpty(uri.UserInfo))
            throw new InvalidOperationException("Configure um endereço HTTP ou HTTPS válido para os links do ISM.");
        var zone = TimeZoneInfo.TryFindSystemTimeZoneById(timeZone, out var configuredZone)
            ? configuredZone : TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
        var utc = DateTime.SpecifyKind(occurredAtUtc, DateTimeKind.Utc);
        var date = TimeZoneInfo.ConvertTimeFromUtc(utc, zone).ToString("dd/MM/yyyy 'às' HH:mm", CultureInfo.GetCultureInfo("pt-BR"));
        var source = isTest ? alert == null ? "DEMONSTRAÇÃO • DADOS ILUSTRATIVOS" : "TESTE • ALERTA REAL DO RESTAURANTE" : "ALERTA AUTOMÁTICO";
        var disclaimer = isTest
            ? alert == null ? "Cenário ilustrativo para apresentação. Nenhum estoque, preço ou pedido foi alterado."
                : "Reenvio de teste com dados de um alerta real. Nenhum cadastro foi alterado."
            : "Você recebeu esta mensagem conforme as preferências de notificação da sua conta.";
        var name = Line(restaurantName);
        var title = Line(content.Title);
        var critical = content.Priority == "Crítico";
        var warning = content.Priority == "Atenção";
        var urgency = critical ? "AÇÃO IMEDIATA — risco para a operação" : warning ? "ATENÇÃO — exige ação do responsável" : "";
        var subject = $"{(isTest ? "[TESTE] " : "")}{(critical ? "[CRÍTICO] " : warning ? "[ATENÇÃO] " : "")}ISM | {title}";
        var facts = string.Join("\n", content.Facts.Select(f => $"{f.Label}: {f.Value}"));
        var plain = $"ISM • {content.Category}\n{source}\n{urgency}\n\n{name}\nPrioridade: {content.Priority}\n{date} ({zone.Id})\n\n{title}\n{content.Summary}\n" +
            (facts.Length > 0 ? $"\n{facts}\n" : "") + $"\nAção recomendada\n{content.Recommendation}\n\n{content.Action}:\n{uri.AbsoluteUri}\n\n{disclaimer}";
        var whatsapp = $"{(critical ? "🚨" : content.Priority == "Oportunidade" ? "💡" : warning ? "⚠️" : "🔔")} *ISM | {content.Category}*\n{source}\n{(urgency.Length > 0 ? "*" + urgency + "*\n" : "")}\n" +
            $"*Restaurante:* {WhatsApp(name)}\n*Prioridade:* {content.Priority}\n*{WhatsApp(title)}*\n\n{WhatsApp(content.Summary)}\n" +
            (facts.Length > 0 ? "\n" + string.Join("\n", content.Facts.Select(f => $"• *{f.Label}:* {WhatsApp(f.Value)}")) + "\n" : "") +
            $"\n*O que fazer:* {content.Recommendation}\n\n{content.Action}:\n{uri.AbsoluteUri}\n\n_{date} ({zone.Id})_\n{disclaimer}";
        if (channel == "whatsapp") return new(subject, whatsapp, null, content.Id, isTest, source);
        var color = critical ? "#b91c1c" : warning ? "#92400e" : "#047857";
        var surface = critical ? "#fef2f2" : warning ? "#fffbeb" : "#ecfdf5";
        var header = critical ? "#7f1d1d" : "#064e3b";
        var rows = string.Join("", content.Facts.Select(f => $"<tr><th scope=\"row\" style=\"padding:10px 12px;text-align:left;border-bottom:1px solid #e5e7eb;font-weight:500;color:#475569\">{H(f.Label)}</th><td style=\"padding:10px 12px;text-align:right;border-bottom:1px solid #e5e7eb;font-weight:700;color:#0f172a\">{H(f.Value)}</td></tr>"));
        var html = $$"""
        <!doctype html><html lang="pt-BR"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>{{H(title)}}</title></head>
        <body style="margin:0;padding:0;background:#f1f5f9;color:#0f172a;font-family:Arial,Helvetica,sans-serif">
          <div style="display:none;max-height:0;overflow:hidden">{{H(content.Summary)}}</div>
          <table role="presentation" style="width:100%;border-collapse:collapse;background:#f1f5f9"><tr><td align="center" style="padding:24px 12px">
            <table role="presentation" style="width:100%;max-width:600px;border-collapse:collapse;background:#fff;border:1px solid #e2e8f0">
              <tr><td style="padding:26px 28px;background:{{header}};color:white"><div style="font-size:28px;font-weight:800;letter-spacing:2px">ISM</div><div style="font-size:12px;margin-top:6px;color:#fff">Intelligence Supply Manager</div></td></tr>
              <tr><td style="padding:24px 28px 0"><div style="font-size:11px;font-weight:700;letter-spacing:1px;color:#64748b">{{H(source)}}</div>{{(urgency.Length == 0 ? "" : "<div style=\"margin-top:16px;padding:14px;background:" + surface + ";border-left:4px solid " + color + ";color:" + color + ";font-size:15px;font-weight:800\">" + H(urgency) + "</div>")}}<p style="margin:16px 0 8px;font-size:13px;color:{{color}};font-weight:bold">{{H(content.Category)}} · {{H(content.Priority)}}</p><h1 style="margin:0;font-size:24px;line-height:1.3">{{H(title)}}</h1><p style="margin:12px 0 0;color:#64748b;font-size:13px">{{H(name)}}<br>{{H(date)}} ({{H(zone.Id)}})</p></td></tr>
              <tr><td style="padding:20px 28px 0"><p style="margin:0;font-size:15px;line-height:1.7">{{H(content.Summary).Replace("\n", "<br>")}}</p>{{(rows.Length == 0 ? "" : "<table aria-label=\"Indicadores do alerta\" style=\"width:100%;border-collapse:collapse;margin-top:20px;font-size:14px\">" + rows + "</table>")}}</td></tr>
              <tr><td style="padding:24px 28px"><div style="padding:16px;background:{{surface}};border-left:4px solid {{color}}"><strong style="font-size:14px">Ação recomendada</strong><p style="margin:8px 0 0;font-size:14px;line-height:1.6">{{H(content.Recommendation)}}</p></div><p style="margin:24px 0 0"><a href="{{H(uri.AbsoluteUri)}}" style="display:inline-block;background:{{color}};color:white;text-decoration:none;padding:14px 20px;font-size:14px;font-weight:bold;border-radius:6px">{{H(content.Action)}} →</a></p><p style="font-size:11px;color:#64748b;word-break:break-all">Se o botão não abrir, copie este endereço: {{H(uri.AbsoluteUri)}}</p></td></tr>
              <tr><td style="padding:20px 28px;background:#f8fafc;border-top:1px solid #e2e8f0;font-size:12px;line-height:1.6;color:#64748b">{{H(disclaimer)}}<br>Gerencie os canais em Configurações → Notificações.<br>ISM · Gestão inteligente para sua operação.</td></tr>
            </table>
          </td></tr></table>
        </body></html>
        """;
        return new(subject, plain, html, content.Id, isTest, source);
    }

    private static string H(string value) => WebUtility.HtmlEncode(value);
    private static string Line(string value) => value.Replace('\r', ' ').Replace('\n', ' ').Trim();
    private static string WhatsApp(string value) => value.Replace("*", "").Replace("_", "").Replace("~", "").Replace("`", "");
}
