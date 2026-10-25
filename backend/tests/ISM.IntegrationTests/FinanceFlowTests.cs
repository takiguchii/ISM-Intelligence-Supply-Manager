using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using ISM.Application.DTOs.Finance;
using ISM.Application.Security;
using ISM.Domain.Modules.Finance;

namespace ISM.IntegrationTests;
public sealed class FinanceFlowTests : IClassFixture<FoundationWebApplicationFactory>
{
    private readonly HttpClient _client;
    public FinanceFlowTests(FoundationWebApplicationFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task FinanceCrud_UsesTenantScope_AndReflectsSettledValues()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", FoundationWebApplicationFactory.Token(IsmRoles.Manager, 920));
        var input = new FinanceEntryRequest { Description = "Conta de energia", Type = FinanceEntryType.Expense, Amount = 345.50m, DueDate = DateTime.UtcNow.Date, IsSettled = false };
        var createdResponse = await _client.PostAsJsonAsync("/api/finance", input);
        createdResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var created = (await createdResponse.Content.ReadFromJsonAsync<FinanceEntryResponse>())!;
        var initial = (await _client.GetFromJsonAsync<FinanceOverview>("/api/finance"))!;
        initial.Summary.AccountsPayable.Should().Be(345.50m);
        input.IsSettled = true;
        input.Amount = 400m;
        (await _client.PutAsJsonAsync($"/api/finance/{created.Id}", input)).StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = (await _client.GetFromJsonAsync<FinanceOverview>("/api/finance"))!;
        updated.Summary.AccountsPayable.Should().Be(0);
        updated.Summary.Expenses.Should().Be(400m);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", FoundationWebApplicationFactory.Token(IsmRoles.Manager, 921));
        (await _client.DeleteAsync($"/api/finance/{created.Id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await _client.GetFromJsonAsync<FinanceOverview>("/api/finance"))!.Entries.Should().BeEmpty();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", FoundationWebApplicationFactory.Token(IsmRoles.Waiter, 920));
        (await _client.GetAsync("/api/finance")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", FoundationWebApplicationFactory.Token(IsmRoles.Manager, 920));
        (await _client.DeleteAsync($"/api/finance/{created.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await _client.GetFromJsonAsync<FinanceOverview>("/api/finance"))!.Entries.Should().BeEmpty();
    }
}
