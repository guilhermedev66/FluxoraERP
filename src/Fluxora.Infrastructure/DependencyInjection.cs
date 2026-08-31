using Fluxora.Application.Common;
using Fluxora.Application.Customers;
using Fluxora.Application.Suppliers;
using Fluxora.Infrastructure.Auditing;
using Fluxora.Infrastructure.Identity;
using Fluxora.Infrastructure.Persistence;
using Fluxora.Infrastructure.Persistence.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Fluxora.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string 'Default' is not configured.");

        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequiredLength = 10;
                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<AppDbContext>();

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<IAuditWriter, AuditWriter>();

        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<CustomerService>();

        services.AddScoped<ISupplierRepository, SupplierRepository>();
        services.AddScoped<SupplierService>();

        return services;
    }
}
