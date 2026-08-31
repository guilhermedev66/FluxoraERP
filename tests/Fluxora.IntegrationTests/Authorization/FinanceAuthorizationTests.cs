using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Fluxora.Api.Controllers;
using Fluxora.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Fluxora.IntegrationTests.Authorization;

public class FinanceAuthorizationTests(FluxoraApiFactory factory) : IClassFixture<FluxoraApiFactory>
{
    [Theory]
    [InlineData("/api/payables")]
    [InlineData("/api/receivables")]
    [InlineData("/api/cash-movements")]
    [InlineData("/api/reports/dashboard-summary")]
    public async Task SalesRole_CannotAccessFinanceOrReporting(string path)
    {
        var client = await CreateClientForRoleAsync(AppRoles.Sales);

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/payables")]
    [InlineData("/api/reports/dashboard-summary")]
    public async Task FinanceRole_CanAccessFinanceAndReporting(string path)
    {
        var client = await CreateClientForRoleAsync(AppRoles.Finance);

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private async Task<HttpClient> CreateClientForRoleAsync(string role)
    {
        var email = $"{role.ToLowerInvariant()}-{Guid.NewGuid():N}@fluxora.test";
        const string password = "Fluxora!Role123";

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = new ApplicationUser { UserName = email, Email = email };
            var createResult = await userManager.CreateAsync(user, password);
            Assert.True(createResult.Succeeded, string.Join(", ", createResult.Errors.Select(e => e.Description)));
            var roleResult = await userManager.AddToRoleAsync(user, role);
            Assert.True(roleResult.Succeeded, string.Join(", ", roleResult.Errors.Select(e => e.Description)));
        }

        var client = factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        loginResponse.EnsureSuccessStatusCode();
        var login = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!.AccessToken);
        return client;
    }
}
