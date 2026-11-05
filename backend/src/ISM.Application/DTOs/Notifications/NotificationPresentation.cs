namespace ISM.Application.DTOs.Notifications;

public sealed record NotificationScenario(string Id, string Label, string Description);
public sealed record NotificationAlertOption(int Id, string Title, string Category);
public sealed record NotificationScenarioCatalog(IReadOnlyList<NotificationScenario> Examples, IReadOnlyList<NotificationAlertOption> Alerts);
public sealed record NotificationPreview(string Subject, string Text, string? Html, string Scenario, bool IsTest, string Source);
