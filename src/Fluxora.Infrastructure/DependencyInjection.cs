using Fluxora.Application.Catalog;
using Fluxora.Application.Common;
using Fluxora.Application.Customers;
using Fluxora.Application.Finance;
using Fluxora.Application.Purchasing;
using Fluxora.Application.Sales;
using Fluxora.Application.Suppliers;
using Fluxora.Infrastructure.Auditing;
using Fluxora.Infrastructure.Identity;
using Fluxora.Infrastructure.Idempotency;
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

        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ProductService>();

        services.AddScoped<IReceivableRepository, ReceivableRepository>();
        services.AddScoped<IPayableRepository, PayableRepository>();
        services.AddScoped<ICashMovementRepository, CashMovementRepository>();
        services.AddScoped<IIdempotencyStore, IdempotencyStore>();
        services.AddScoped<FinanceQueryService>();
        services.AddScoped<PaymentService>();
        services.AddScoped<ReceiptService>();

        services.AddScoped<ISalesOrderRepository, SalesOrderRepository>();
        services.AddScoped<SalesOrderService>();

        services.AddScoped<IPurchaseOrderRepository, PurchaseOrderRepository>();
        services.AddScoped<PurchaseOrderService>();

        return services;
    }
}
