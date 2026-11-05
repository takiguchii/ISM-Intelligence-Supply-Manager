using FluentAssertions;
using ISM.Application.Services.Notifications;
using ISM.Domain.Modules.System.Entities;
using ISM.Domain.Modules.System.Enums;

namespace ISM.UnitTests;

public sealed class NotificationMessageFormatterTests
{
    [Theory]
    [InlineData("connectivity")]
    [InlineData("stock-low")]
    [InlineData("stock-risk")]
    [InlineData("supplier-price")]
    [InlineData("supplier-alternative")]
    [InlineData("dish-margin")]
    public void Scenarios_HaveEmailAndWhatsAppVersions_AndAreClearlyMarkedAsExamples(string scenario)
    {
        var email = Render(scenario, "email");
        email.Subject.Should().StartWith("[TESTE]");
        email.Html.Should().Contain("DADOS ILUSTRATIVOS").And.Contain("Ação recomendada").And.Contain("https://ism.example.test/");
        email.Text.Should().Contain("Restaurante Exemplo").And.Contain("04/11/2026 às 12:00");
        email.Html!.Length.Should().BeLessThan(30000);
        var whatsapp = Render(scenario, "whatsapp");
        whatsapp.Html.Should().BeNull();
        whatsapp.Text.Should().Contain("*ISM |").And.Contain("*O que fazer:*").And.Contain("DADOS ILUSTRATIVOS");
        whatsapp.Text.Should().NotContain("<html");
    }

    [Fact]
    public void ActualAlerts_AreEscapedAndKeepTheirOwnData_InsteadOfUsingExampleValues()
    {
        var alert = new SystemAlert
        {
            AlertType = SystemAlertType.StockBelowMinimum, Severity = AlertSeverity.Warning,
            Title = "Produto <script>alert(1)</script>", Message = "Saldo real: 2 caixas. <img src=x onerror=alert(1)>"
        };
        var message = NotificationMessageFormatter.Render("stock-low", "email", "Restaurante <b>teste</b>",
            "America/Sao_Paulo", new DateTime(2026, 11, 4, 15, 0, 0), "https://ism.example.test", false, alert);
        message.Subject.Should().NotContain("[TESTE]");
        message.Html.Should().Contain("&lt;script&gt;").And.NotContain("<script>").And.NotContain("<img src=x");
        message.Source.Should().Be("ALERTA AUTOMÁTICO");
        message.Html.Should().NotContain("Saldo atual</th>");
        message.Text.Should().Contain("Saldo real: 2 caixas.");
    }

    [Fact]
    public void Urgency_ReflectsSeverity_WithoutTurningOpportunitiesIntoEmergencies()
    {
        var critical = Render("stock-risk", "email");
        critical.Subject.Should().Contain("[CRÍTICO]").And.Contain("1,5 dia");
        critical.Html.Should().Contain("#7f1d1d").And.Contain("#fef2f2");
        Render("stock-risk", "whatsapp").Text.Should().StartWith("🚨").And.Contain("AÇÃO IMEDIATA");
        var opportunity = Render("supplier-alternative", "whatsapp");
        opportunity.Text.Should().StartWith("💡").And.NotContain("AÇÃO IMEDIATA");
        opportunity.Subject.Should().NotContain("[CRÍTICO]").And.NotContain("[ATENÇÃO]");
    }

    [Fact]
    public void UnsafeActionUrls_AreRejected()
    {
        var act = () => NotificationMessageFormatter.Render("stock-low", "email", "Restaurante",
            "UTC", DateTime.UtcNow, "javascript:alert(1)", true);
        act.Should().Throw<InvalidOperationException>();
    }

    private static ISM.Application.DTOs.Notifications.NotificationPreview Render(string scenario, string channel) =>
        NotificationMessageFormatter.Render(scenario, channel, "Restaurante Exemplo", "America/Sao_Paulo",
            new DateTime(2026, 11, 4, 15, 0, 0), "https://ism.example.test", true);
}
