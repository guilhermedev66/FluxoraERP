using Fluxora.Application.Automation;
using Fluxora.Application.Catalog;
using Fluxora.Application.Common;
using Fluxora.Application.Customers;
using Fluxora.Application.Finance;
using Fluxora.Application.Purchasing;
using Fluxora.Application.Reporting;
using Fluxora.Application.Sales;
using Fluxora.Application.Suppliers;
using Fluxora.Infrastructure.Automation;
using Fluxora.Infrastructure.Auditing;
using Fluxora.Infrastructure.Identity;
using Fluxora.Infrastructure.Idempotency;
using Fluxora.Infrastructure.Persistence;
using Fluxora.Infrastructure.Persistence.Repositories;
using Fluxora.Infrastructure.Time;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

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
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<AppDbContext>();

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<IAuditWriter, AuditWriter>();

        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<CustomerService>();
        services.AddScoped<CustomerCsvService>();

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

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IBusinessClock>(sp => new BusinessClock(
            sp.GetRequiredService<TimeProvider>(),
            configuration["Business:TimeZone"] ?? "America/Sao_Paulo"));
        services.AddScoped<IReportingRepository, ReportingRepository>();
        services.AddScoped<ReportingService>();

        services.AddScoped<IOverdueRepository, OverdueRepository>();
        services.AddScoped<OverdueProcessingService>();
        services.AddScoped<IDashboardSnapshotRepository, DashboardSnapshotRepository>();
        services.AddScoped<DashboardSnapshotService>();

        var businessTimeZone = TimeZoneInfo.FindSystemTimeZoneById(
            configuration["Business:TimeZone"] ?? "America/Sao_Paulo");

        services.AddQuartz(quartz =>
        {
            quartz.SchedulerId = "AUTO";
            quartz.SchedulerName = "Fluxora Automation";
            quartz.UseDefaultThreadPool(options => options.MaxConcurrency = 2);
            quartz.UsePersistentStore(store =>
            {
                store.UseProperties = true;
                store.UsePostgres(connectionString);
                store.UseSystemTextJsonSerializer();
                store.UseClustering();
            });

            var overdueJob = new JobKey("overdue-processing", "automation");
            quartz.AddJob<OverdueProcessingJob>(options => options.WithIdentity(overdueJob).StoreDurably());
            quartz.AddTrigger(options => options
                .WithIdentity("overdue-processing-daily", "automation")
                .ForJob(overdueJob)
                .WithCronSchedule(configuration["Automation:OverdueCron"] ?? "0 5 0 * * ?", schedule => schedule
                    .InTimeZone(businessTimeZone)
                    .WithMisfireHandlingInstructionFireAndProceed()));

            var snapshotJob = new JobKey("dashboard-snapshot", "automation");
            quartz.AddJob<DashboardSnapshotJob>(options => options.WithIdentity(snapshotJob).StoreDurably());
            quartz.AddTrigger(options => options
                .WithIdentity("dashboard-snapshot-daily", "automation")
                .ForJob(snapshotJob)
                .WithCronSchedule(configuration["Automation:DashboardSnapshotCron"] ?? "0 15 0 * * ?", schedule => schedule
                    .InTimeZone(businessTimeZone)
                    .WithMisfireHandlingInstructionFireAndProceed()));
        });
        services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);

        return services;
    }
}
