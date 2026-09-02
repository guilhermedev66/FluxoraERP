using System.Security.Claims;
using Fluxora.Api.Auth;
using Fluxora.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Fluxora.Api.Controllers;

public sealed record LoginRequest(string Email, string Password);

public sealed record LoginResponse(string AccessToken, DateTime ExpiresAtUtc);

[ApiController]
[Route("api/auth")]
public class AuthController(
    UserManager<ApplicationUser> userManager,
    JwtTokenService tokenService,
    ILoginAttemptGuard loginAttemptGuard,
    Microsoft.Extensions.Options.IOptions<JwtOptions> jwtOptions) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        var sourceIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var loginAttemptLease = loginAttemptGuard.Reserve(sourceIp, request.Email);
        if (loginAttemptLease.Throttled)
        {
            return StatusCode(StatusCodes.Status429TooManyRequests);
        }

        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null || await userManager.IsLockedOutAsync(user))
        {
            loginAttemptLease.ConfirmFailure();
            return Unauthorized(new ProblemDetails { Title = "Invalid credentials.", Status = StatusCodes.Status401Unauthorized });
        }

        if (!await userManager.CheckPasswordAsync(user, request.Password))
        {
            await userManager.AccessFailedAsync(user);
            loginAttemptLease.ConfirmFailure();
            return Unauthorized(new ProblemDetails { Title = "Invalid credentials.", Status = StatusCodes.Status401Unauthorized });
        }

        loginAttemptLease.Release();
        await userManager.ResetAccessFailedCountAsync(user);

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
