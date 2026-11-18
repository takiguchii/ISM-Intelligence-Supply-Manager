using ISM.Application.DTOs;
using ISM.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ISM.API.Controllers.Users;

[ApiController, Authorize, Route("api/me/profile")]
public sealed class MyProfileController(IUserService users) : ControllerBase
{
    [HttpPut, RequestSizeLimit(300_000)]
    public async Task<ActionResult<MyProfile>> Update(UpdateMyProfileRequest request, CancellationToken ct)
    {
        try
        {
            var profile = await users.UpdateMyProfileAsync(request, ct);
            return profile is null ? NotFound() : Ok(profile);
        }
        catch (ArgumentException error)
        {
            return BadRequest(new { message = error.Message });
        }
    }
}
