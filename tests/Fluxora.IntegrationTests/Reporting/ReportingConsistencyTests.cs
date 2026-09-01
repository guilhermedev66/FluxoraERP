using Fluxora.Application.Common;
using Fluxora.Application.Reporting;
using Fluxora.Domain.Catalog;
using Fluxora.Domain.Customers;
using Fluxora.Domain.Finance;
using Fluxora.Domain.Sales;
using Fluxora.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Fluxora.IntegrationTests.Reporting;

public class ReportingConsistencyTests(FluxoraApiFactory factory) : IClassFixture<FluxoraApiFactory>
{
    [Fact]
    public async Task DashboardSummary_UsesOneSnapshotWhenFinancialCommitLandsBetweenReads()
    {
        const decimal concurrentAmount = 777.77m;

        await using var reportScope = factory.Services.CreateAsyncScope();
        var repository = reportScope.ServiceProvider.GetRequiredService<IReportingRepository>();
        var businessClock = reportScope.ServiceProvider.GetRequiredService<IBusinessClock>();
        var unitOfWork = reportScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var monthStart = new DateOnly(businessClock.Today.Year, businessClock.Today.Month, 1);
        var balanceBefore = await repository.GetCurrentBalanceAsync();
        var revenueBefore = (await repository.GetRevenueByMonthAsync(monthStart, businessClock.Today)).Sum(row => row.Amount);

        var hookedRepository = new AfterBalanceHookRepository(repository, async () =>
        {
            await using var mutationScope = factory.Services.CreateAsyncScope();
            var db = mutationScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var product = Product.Create($"SNP-{Guid.NewGuid():N}"[..12], "Produto Snapshot", 1m, "Snapshot");
            var customer = Customer.Create("Cliente Snapshot", TestData.UniqueDocument(), null, null);
            var sale = SalesOrder.CreateDraft(customer.Id, Guid.Empty);
            sale.AddLine(product.Id, product.Name, 1, concurrentAmount);
            sale.Approve();
            var movement = CashMovement.For(
                CashMovementDirection.Inflow, concurrentAmount, "SnapshotTest", sale.Id);

            db.AddRange(product, customer, sale, movement);
            await db.SaveChangesAsync();
        });
        var service = new ReportingService(hookedRepository, businessClock, unitOfWork);

        var duringCommit = await service.GetDashboardSummaryAsync();

        Assert.Equal(balanceBefore, duringCommit.CurrentBalance);
        Assert.Equal(revenueBefore, duringCommit.MonthRevenue);

        await using var verificationScope = factory.Services.CreateAsyncScope();
        var verificationRepository = verificationScope.ServiceProvider.GetRequiredService<IReportingRepository>();
        var balanceAfter = await verificationRepository.GetCurrentBalanceAsync();
        var revenueAfter = (await verificationRepository.GetRevenueByMonthAsync(
            monthStart, businessClock.Today)).Sum(row => row.Amount);
        Assert.Equal(balanceBefore + concurrentAmount, balanceAfter);
        Assert.Equal(revenueBefore + concurrentAmount, revenueAfter);
    }

    private sealed class AfterBalanceHookRepository(
        IReportingRepository inner,
        Func<Task> afterBalance) : IReportingRepository
    {
        private int _hookInvoked;

        public Task<IReadOnlyList<PeriodAmountDto>> GetRevenueByMonthAsync(
            DateOnly? from, DateOnly? to, CancellationToken cancellationToken = default) =>
            inner.GetRevenueByMonthAsync(from, to, cancellationToken);

        public Task<IReadOnlyList<PeriodAmountDto>> GetExpensesByMonthAsync(
            DateOnly? from, DateOnly? to, CancellationToken cancellationToken = default) =>
            inner.GetExpensesByMonthAsync(from, to, cancellationToken);

        public Task<OverdueSummaryDto> GetOverdueSummaryAsync(
            DateOnly asOf, CancellationToken cancellationToken = default) =>
            inner.GetOverdueSummaryAsync(asOf, cancellationToken);

        public Task<IReadOnlyList<DueBucketDto>> GetUpcomingDueAsync(
            DateOnly asOf, int days, CancellationToken cancellationToken = default) =>
            inner.GetUpcomingDueAsync(asOf, days, cancellationToken);

        public Task<IReadOnlyList<CashFlowPeriodDto>> GetCashFlowAsync(
            DateOnly? from, DateOnly? to, bool groupByDay, CancellationToken cancellationToken = default) =>
            inner.GetCashFlowAsync(from, to, groupByDay, cancellationToken);

        public Task<IReadOnlyList<ProjectedCashFlowPeriodDto>> GetProjectedCashFlowAsync(
            DateOnly asOf, int days, CancellationToken cancellationToken = default) =>
            inner.GetProjectedCashFlowAsync(asOf, days, cancellationToken);

        public Task<IReadOnlyList<TopCustomerDto>> GetTopCustomersAsync(
            DateOnly? from, DateOnly? to, int limit, CancellationToken cancellationToken = default) =>
            inner.GetTopCustomersAsync(from, to, limit, cancellationToken);

        public Task<IReadOnlyList<CategoryExpenseDto>> GetExpensesByCategoryAsync(
            DateOnly? from, DateOnly? to, CancellationToken cancellationToken = default) =>
            inner.GetExpensesByCategoryAsync(from, to, cancellationToken);

        public async Task<decimal> GetCurrentBalanceAsync(CancellationToken cancellationToken = default)
        {
            var balance = await inner.GetCurrentBalanceAsync(cancellationToken);
            if (Interlocked.Exchange(ref _hookInvoked, 1) == 0)
            {
                await afterBalance();
            }

            return balance;
        }
    }
}
