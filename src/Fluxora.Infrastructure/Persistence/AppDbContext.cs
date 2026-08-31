using Fluxora.Application.Common;
using Fluxora.Domain.Auditing;
using Fluxora.Domain.Catalog;
using Fluxora.Domain.Customers;
using Fluxora.Domain.Finance;
using Fluxora.Domain.Purchasing;
using Fluxora.Domain.Sales;
using Fluxora.Domain.Suppliers;
using Fluxora.Infrastructure.Identity;
using Fluxora.Infrastructure.Persistence.Configurations;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Fluxora.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>(options), IUnitOfWork
{
    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<Supplier> Suppliers => Set<Supplier>();

    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<SalesOrder> SalesOrders => Set<SalesOrder>();

    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();

    public DbSet<Receivable> Receivables => Set<Receivable>();

    public DbSet<ReceivableInstallment> ReceivableInstallments => Set<ReceivableInstallment>();

    public DbSet<Payable> Payables => Set<Payable>();

    public DbSet<PayableInstallment> PayableInstallments => Set<PayableInstallment>();

    public DbSet<SalesOrderLine> SalesOrderLines => Set<SalesOrderLine>();

    public DbSet<PurchaseOrderLine> PurchaseOrderLines => Set<PurchaseOrderLine>();

    public DbSet<Payment> Payments => Set<Payment>();

    public DbSet<Receipt> Receipts => Set<Receipt>();

    public DbSet<CashMovement> CashMovements => Set<CashMovement>();

    public DbSet<Idempotency.IdempotencyRecord> IdempotencyRecords => Set<Idempotency.IdempotencyRecord>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfiguration(new CustomerConfiguration());
        builder.ApplyConfiguration(new SupplierConfiguration());
        builder.ApplyConfiguration(new AuditEntryConfiguration());
        builder.ApplyConfiguration(new ProductConfiguration());
        builder.ApplyConfiguration(new SalesOrderConfiguration());
        builder.ApplyConfiguration(new SalesOrderLineConfiguration());
        builder.ApplyConfiguration(new PurchaseOrderConfiguration());
        builder.ApplyConfiguration(new PurchaseOrderLineConfiguration());
        builder.ApplyConfiguration(new ReceivableConfiguration());
        builder.ApplyConfiguration(new ReceivableInstallmentConfiguration());
        builder.ApplyConfiguration(new PayableConfiguration());
        builder.ApplyConfiguration(new PayableInstallmentConfiguration());
        builder.ApplyConfiguration(new PaymentConfiguration());
        builder.ApplyConfiguration(new ReceiptConfiguration());
        builder.ApplyConfiguration(new CashMovementConfiguration());
        builder.ApplyConfiguration(new Configurations.IdempotencyRecordConfiguration());
    }

    /// <summary>
    /// Translates EF Core's optimistic-concurrency exception into the application-level
    /// ConcurrencyConflictException, so every caller (any entity with an IsConcurrencyToken)
    /// gets the same 409 behavior without depending on EF Core types itself.
    /// </summary>
    async Task IUnitOfWork.SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyConflictException(
                "The record was modified by another request since it was loaded. Reload and try again.");
        }
    }
}
