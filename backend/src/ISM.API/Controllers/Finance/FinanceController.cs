using ISM.Application.DTOs.Finance;
using ISM.Application.Interfaces.Finance;
using ISM.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace ISM.API.Controllers.Finance;
[ApiController]
[Route("api/finance")]
[Authorize(Policy = IsmPolicies.RestaurantManagerOrAbove)]
public sealed class FinanceController(IFinanceService finance) : ControllerBase
{
    [HttpGet] public async Task<ActionResult<FinanceOverview>> Get(CancellationToken ct) => Ok(await finance.GetAsync(ct));
    [HttpPost] public async Task<ActionResult<FinanceEntryResponse>> Create(FinanceEntryRequest request, CancellationToken ct) => Ok(await finance.CreateAsync(request, ct));
    [HttpPut("{id:int}")] public async Task<ActionResult<FinanceEntryResponse>> Update(int id, FinanceEntryRequest request, CancellationToken ct) { var entry = await finance.UpdateAsync(id, request, ct); return entry is null ? NotFound() : Ok(entry); }
    [HttpDelete("{id:int}")] public async Task<IActionResult> Delete(int id, CancellationToken ct) => await finance.DeleteAsync(id, ct) ? NoContent() : NotFound();
}
