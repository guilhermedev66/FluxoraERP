using Fluxora.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
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
        // Minimal-hosting entry points read configuration while Program is executing. UseSetting
        // makes these values available before AddInfrastructure consumes the connection string.
        builder.UseSetting("ConnectionStrings:Default", _container.GetConnectionString());
        builder.UseSetting("Jwt:Key", "integration-test-signing-key-please-32chars+");
        builder.UseSetting("Jwt:Issuer", "Fluxora.Tests");
        builder.UseSetting("Jwt:Audience", "Fluxora.Tests");
        builder.UseSetting("Jwt:ExpirationMinutes", "30");
        builder.UseSetting("Bootstrap:AdminEmail", AdminEmail);
        builder.UseSetting("Bootstrap:AdminPassword", AdminPassword);
        builder.UseSetting("Database:ApplyMigrations", "true");
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        // Touching Services builds the host, which runs Program.cs's migration + seeding
        // block (gated by Database:ApplyMigrations=true above) before any test issues a request.
        await using var scope = Services.CreateAsyncScope();
        _ = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}
