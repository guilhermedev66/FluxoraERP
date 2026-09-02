using System.Net;
using System.Net.Http.Json;
using Fluxora.Api.Controllers;
using Fluxora.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Fluxora.IntegrationTests.Authorization;

public class LoginLockoutTests(FluxoraApiFactory factory) : IClassFixture<FluxoraApiFactory>
{
    [Fact]
    public async Task Login_RepeatedInvalidPasswords_LocksAccountAndKeepsGenericResponse()
    {
        var email = $"lockout-{Guid.NewGuid():N}@fluxora.test";
        const string password = "Fluxora!Lockout123";
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var created = await userManager.CreateAsync(new ApplicationUser
            {
                UserName = email,
                Email = email,
                DisplayName = "Lockout Test",
            }, password);
            Assert.True(created.Succeeded, string.Join(", ", created.Errors.Select(error => error.Description)));
        }

        var client = factory.CreateClient();
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "Wrong!Password123"));
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Contains("Invalid credentials", await response.Content.ReadAsStringAsync());
        }

        var correctPasswordResponse = await client.PostAsJsonAsync(
            "/api/auth/login", new LoginRequest(email, password));
        Assert.Equal(HttpStatusCode.Unauthorized, correctPasswordResponse.StatusCode);

        await using var verificationScope = factory.Services.CreateAsyncScope();
        var verificationManager = verificationScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await verificationManager.FindByEmailAsync(email);
        Assert.NotNull(user);
        Assert.True(await verificationManager.IsLockedOutAsync(user));
    }
}

public class LoginRateLimitTests(FluxoraApiFactory factory) : IClassFixture<FluxoraApiFactory>
{
    [Fact]
    public async Task Login_ExcessiveRequestsFromOneAddress_ReturnsTooManyRequests()
    {
        var client = factory.CreateClient();
        var statuses = new List<HttpStatusCode>();

        for (var attempt = 0; attempt < 21; attempt++)
        {
            var response = await client.PostAsJsonAsync(
                "/api/auth/login",
                new LoginRequest("unknown@fluxora.test", "Wrong!Password123"));
            statuses.Add(response.StatusCode);
        }

        Assert.Equal(20, statuses.Count(status => status == HttpStatusCode.Unauthorized));
        Assert.Equal(HttpStatusCode.TooManyRequests, statuses[^1]);
    }
}

public class LoginAttemptGuardTests(FluxoraApiFactory factory) : IClassFixture<FluxoraApiFactory>
{
    [Fact]
    public async Task Login_TooManyDistinctTargetsFromOneAddress_ThrottlesFurtherTargetsRegardlessOfCredentials()
    {
        var client = factory.CreateClient();
        var suffix = Guid.NewGuid().ToString("N");

        for (var i = 0; i < 5; i++)
        {
            var response = await client.PostAsJsonAsync(
                "/api/auth/login",
                new LoginRequest($"guard-{suffix}-{i}@fluxora.test", "Wrong!Password123"));
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        // A 6th distinct target from the same source is throttled even with the correct
        // password for an account that genuinely exists - proves the guard runs before any
        // credential check and cannot be used to distinguish real from fake accounts.
        var sixthResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(FluxoraApiFactory.AdminEmail, FluxoraApiFactory.AdminPassword));
        Assert.Equal(HttpStatusCode.TooManyRequests, sixthResponse.StatusCode);

        // Repeating an already-seen target does not consume additional guard budget.
        var repeatResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest($"guard-{suffix}-0@fluxora.test", "Wrong!Password123"));
        Assert.Equal(HttpStatusCode.Unauthorized, repeatResponse.StatusCode);
    }
}

// Each of these gets its own FluxoraApiFactory instance (a fresh in-process rate limiter and
// login-attempt-guard state), rather than sharing LoginAttemptGuardTests' fixture, so their request
// volume can't collide with sibling tests against the shared 20-req/minute per-IP login rate limit.

public class LoginAttemptGuardRaceTests(FluxoraApiFactory factory) : IClassFixture<FluxoraApiFactory>
{
    [Fact]
    public async Task Login_ConcurrentDistinctTargetsFromOneAddress_StillCapsAtFiveDespiteRace()
    {
        var client = factory.CreateClient();
        var suffix = Guid.NewGuid().ToString("N");

        // 10 distinct new targets fired concurrently from the same source: the cap check and the
        // reservation of a slot must be atomic, or every request can observe room under the cap
        // before any of them is recorded and all 10 would be admitted instead of only 5.
        var responses = await Task.WhenAll(Enumerable.Range(0, 10).Select(i => client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest($"race-{suffix}-{i}@fluxora.test", "Wrong!Password123"))));

        Assert.Equal(5, responses.Count(r => r.StatusCode == HttpStatusCode.Unauthorized));
        Assert.Equal(5, responses.Count(r => r.StatusCode == HttpStatusCode.TooManyRequests));
    }
}

public class LoginAttemptGuardSuccessReleaseTests(FluxoraApiFactory factory) : IClassFixture<FluxoraApiFactory>
{
    [Fact]
    public async Task Login_ConcurrentSuccessfulLoginsFromOneAddress_AreNeverCountedAgainstTheCap()
    {
        var client = factory.CreateClient();

        // Successes must release their provisional reservation: repeating the same real account
        // concurrently many times must never itself trip the distinct-target cap.
        var responses = await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(FluxoraApiFactory.AdminEmail, FluxoraApiFactory.AdminPassword))));

        Assert.All(responses, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));
    }
}
