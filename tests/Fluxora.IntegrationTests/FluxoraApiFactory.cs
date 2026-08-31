using Fluxora.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace Fluxora.IntegrationTests;

public class FluxoraApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("fluxora_test")
        .WithUsername("fluxora")
        .WithPassword("fluxora")
        .Build();

    public const string AdminEmail = "admin@fluxora.test";
    public const string AdminPassword = "Fluxora!Test123";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = _container.GetConnectionString(),
                ["Jwt:Key"] = "integration-test-signing-key-please-32chars+",
                ["Jwt:Issuer"] = "Fluxora.Tests",
                ["Jwt:Audience"] = "Fluxora.Tests",
                ["Jwt:ExpirationMinutes"] = "30",
                ["Bootstrap:AdminEmail"] = AdminEmail,
                ["Bootstrap:AdminPassword"] = AdminPassword,
                ["Database:ApplyMigrations"] = "true",
            });
        });
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        // Touching Services builds the host, which runs Program.cs's migration + seeding
        // block (gated by Database:ApplyMigrations=true above) before any test issues a request.
        _ = Services.GetRequiredService<AppDbContext>();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}
