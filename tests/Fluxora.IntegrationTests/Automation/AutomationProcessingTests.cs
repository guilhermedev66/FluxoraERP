using System.Net.Http.Headers;
using System.Net.Http.Json;
using Fluxora.Api.Controllers;
using Fluxora.Application.Automation;
using Fluxora.Application.Catalog;
using Fluxora.Application.Customers;
using Fluxora.Application.Finance;
using Fluxora.Application.Reporting;
using Fluxora.Application.Sales;
using Fluxora.Domain.Auditing;
using Fluxora.Domain.Finance;
using Fluxora.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Fluxora.IntegrationTests.Automation;

public class AutomationProcessingTests(FluxoraApiFactory factory) : IClassFixture<FluxoraApiFactory>
{
    [Fact]
    public async Task OverdueProcessing_ExecutedTwice_MarksAndAuditsOnlyOnce()
    {
        var client = await CreateAuthenticatedClientAsync();
        var installmentId = await CreatePastDueReceivableAsync(client);

        OverdueProcessingResult first;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            first = await scope.ServiceProvider.GetRequiredService<OverdueProcessingService>().ProcessAsync();
        }

        OverdueProcessingResult second;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            second = await scope.ServiceProvider.GetRequiredService<OverdueProcessingService>().ProcessAsync();
        }

        Assert.True(first.ReceivablesMarked >= 1);
        Assert.Equal(0, second.ReceivablesMarked);
        Assert.Equal(0, second.PayablesMarked);

        await using var verificationScope = factory.Services.CreateAsyncScope();
        var dbContext = verificationScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var installment = await dbContext.ReceivableInstallments.AsNoTracking()
            .SingleAsync(item => item.Id == installmentId);
        Assert.Equal(InstallmentStatus.Overdue, installment.Status);

        var audits = await dbContext.AuditEntries.AsNoTracking()
            .Where(entry => entry.EntityId == installmentId &&
                entry.Action == "ReceivableInstallmentMarkedOverdue")
            .ToListAsync();
        var audit = Assert.Single(audits);
        Assert.Equal(ActorType.System, audit.ActorType);
        Assert.Null(audit.ActorId);
    }

    [Fact]
    public async Task DashboardSnapshotPreparation_ExecutedTwice_CreatesSingleSnapshotAndAudit()
    {
        var summary = new DashboardSummaryDto(
            CurrentBalance: 250m,
            MonthRevenue: 900m,
            MonthExpenses: 650m,
            MonthNet: 250m,
            OverdueReceivablesCount: 2,
            OverdueReceivablesAmount: 100m,
            OverduePayablesCount: 1,
            OverduePayablesAmount: 50m,
            DueTodayCount: 0,
            DueTodayAmount: 0m,
            DueNext30DaysCount: 3,
            DueNext30DaysAmount: 300m);

        DashboardSnapshotPreparationResult first;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            first = await scope.ServiceProvider.GetRequiredService<DashboardSnapshotService>()
                .PrepareAsync(summary);
        }

        DashboardSnapshotPreparationResult second;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            second = await scope.ServiceProvider.GetRequiredService<DashboardSnapshotService>()
                .PrepareAsync(summary);
        }

        Assert.True(first.Created);
        Assert.False(second.Created);
        Assert.Equal(first.Snapshot.Id, second.Snapshot.Id);

        await using var verificationScope = factory.Services.CreateAsyncScope();
        var dbContext = verificationScope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(1, await dbContext.DashboardSnapshots.CountAsync(
            snapshot => snapshot.BusinessDate == first.Snapshot.BusinessDate));
        Assert.Equal(1, await dbContext.AuditEntries.CountAsync(
            entry => entry.EntityId == first.Snapshot.Id && entry.Action == "DashboardSnapshotPrepared"));
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var client = factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(
            FluxoraApiFactory.AdminEmail, FluxoraApiFactory.AdminPassword));
        loginResponse.EnsureSuccessStatusCode();
        var payload = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", payload!.AccessToken);
        return client;
    }

    private static async Task<Guid> CreatePastDueReceivableAsync(HttpClient client)
    {
        var customerResponse = await client.PostAsJsonAsync("/api/customers", new CreateCustomerRequest(
            "Cliente Automacao", TestData.UniqueDocument(), null, null));
        var customer = await customerResponse.Content.ReadFromJsonAsync<CustomerDto>();

        var productResponse = await client.PostAsJsonAsync("/api/products", new CreateProductRequest(
            $"SKU-{Guid.NewGuid():N}"[..12], "Produto Automacao", 100m));
        var product = await productResponse.Content.ReadFromJsonAsync<ProductDto>();

        var orderResponse = await client.PostAsJsonAsync("/api/sales-orders", new CreateSalesOrderRequest(customer!.Id));
        var order = await orderResponse.Content.ReadFromJsonAsync<SalesOrderDto>();
        await client.PostAsJsonAsync($"/api/sales-orders/{order!.Id}/lines", new AddSalesOrderLineRequest(product!.Id, 1));

        var dueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2));
        var approveResponse = await client.PostAsJsonAsync(
            $"/api/sales-orders/{order.Id}/approve",
            new ApproveSalesOrderRequest(1, dueDate));
        approveResponse.EnsureSuccessStatusCode();

        var receivables = await (await client.GetAsync("/api/receivables"))
            .Content.ReadFromJsonAsync<List<ReceivableDto>>();
        return receivables!.Single(receivable => receivable.SalesOrderId == order.Id).Installments[0].Id;
    }
}
