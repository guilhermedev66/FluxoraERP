using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Fluxora.Infrastructure.Persistence;

/// <summary>
/// Design-time factory used only by `dotnet ef` to generate migrations without needing the
/// full application host. Never used at runtime - the real connection string always comes
/// from configuration/environment via DependencyInjection.AddInfrastructure.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Database=fluxora_design;Username=design;Password=design");
        return new AppDbContext(optionsBuilder.Options);
    }
}
