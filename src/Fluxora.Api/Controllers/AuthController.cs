using System.Security.Claims;
using Fluxora.Api.Auth;
using Fluxora.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Fluxora.Api.Controllers;

public sealed record LoginRequest(string Email, string Password);

public sealed record LoginResponse(string AccessToken, DateTime ExpiresAtUtc);

[ApiController]
[Route("api/auth")]
public class AuthController(
    UserManager<ApplicationUser> userManager,
    JwtTokenService tokenService,
    Microsoft.Extensions.Options.IOptions<JwtOptions> jwtOptions) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null || !await userManager.CheckPasswordAsync(user, request.Password))
        {
            return Unauthorized(new ProblemDetails { Title = "Invalid credentials.", Status = StatusCodes.Status401Unauthorized });
        }

        var roles = await userManager.GetRolesAsync(user);
        var token = tokenService.CreateToken(user, roles);

        return Ok(new LoginResponse(token, DateTime.UtcNow.AddMinutes(jwtOptions.Value.ExpirationMinutes)));
    }

    [HttpGet("me")]
    [Authorize]
    public IActionResult Me() => Ok(new
    {
        Id = User.FindFirstValue(ClaimTypes.NameIdentifier),
        Name = User.Identity?.Name,
        Roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value),
    });
}
