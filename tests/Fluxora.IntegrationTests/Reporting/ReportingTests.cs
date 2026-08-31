using System.Net.Http.Headers;
using System.Net.Http.Json;
using Fluxora.Api.Controllers;
using Fluxora.Application.Catalog;
using Fluxora.Application.Customers;
using Fluxora.Application.Finance;
using Fluxora.Application.Reporting;
using Fluxora.Application.Sales;

namespace Fluxora.IntegrationTests.Reporting;

/// <summary>
/// Proves the reports reflect real data produced through the normal Sales -> Finance flow,
/// not just that the endpoints respond.
/// </summary>
public class ReportingTests(FluxoraApiFactory factory) : IClassFixture<FluxoraApiFactory>
{
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

    [Fact]
    public async Task ApprovedSale_AppearsInRevenueAndTopCustomers()
    {
        var client = await CreateAuthenticatedClientAsync();

        var customerResponse = await client.PostAsJsonAsync("/api/customers", new CreateCustomerRequest(
            "Cliente Relatorios", $"CPF-{Guid.NewGuid():N}", null, null));
        var customer = await customerResponse.Content.ReadFromJsonAsync<CustomerDto>();

        var productResponse = await client.PostAsJsonAsync("/api/products", new CreateProductRequest(
            $"SKU-{Guid.NewGuid():N}"[..12], "Consultoria", 500m, "Servicos"));
        var product = await productResponse.Content.ReadFromJsonAsync<ProductDto>();

        var orderResponse = await client.PostAsJsonAsync("/api/sales-orders", new CreateSalesOrderRequest(customer!.Id));
        var order = await orderResponse.Content.ReadFromJsonAsync<SalesOrderDto>();

        await client.PostAsJsonAsync($"/api/sales-orders/{order!.Id}/lines", new AddSalesOrderLineRequest(product!.Id, Quantity: 1));

        var approveResponse = await client.PostAsJsonAsync($"/api/sales-orders/{order.Id}/approve",
            new ApproveSalesOrderRequest(InstallmentCount: 1, FirstDueDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30))));
        approveResponse.EnsureSuccessStatusCode();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var monthStart = new DateOnly(today.Year, today.Month, 1);

        var revenueResponse = await client.GetAsync($"/api/reports/revenue?from={monthStart:yyyy-MM-dd}&to={today:yyyy-MM-dd}");
        revenueResponse.EnsureSuccessStatusCode();
        var revenue = await revenueResponse.Content.ReadFromJsonAsync<List<PeriodAmountDto>>();

        Assert.Contains(revenue!, r => r.Amount >= 500m);

        var topCustomersResponse = await client.GetAsync($"/api/reports/top-customers?from={monthStart:yyyy-MM-dd}&to={today:yyyy-MM-dd}");
        topCustomersResponse.EnsureSuccessStatusCode();
        var topCustomers = await topCustomersResponse.Content.ReadFromJsonAsync<List<TopCustomerDto>>();

        Assert.Contains(topCustomers!, c => c.CustomerId == customer.Id && c.TotalRevenue == 500m);
    }

    [Fact]
    public async Task Receipt_UpdatesCashFlowAndDashboardBalance()
    {
        var client = await CreateAuthenticatedClientAsync();

        var customerResponse = await client.PostAsJsonAsync("/api/customers", new CreateCustomerRequest(
            "Cliente Caixa", $"CPF-{Guid.NewGuid():N}", null, null));
        var customer = await customerResponse.Content.ReadFromJsonAsync<CustomerDto>();

        var productResponse = await client.PostAsJsonAsync("/api/products", new CreateProductRequest(
            $"SKU-{Guid.NewGuid():N}"[..12], "Produto Caixa", 300m, null));
        var product = await productResponse.Content.ReadFromJsonAsync<ProductDto>();

        var orderResponse = await client.PostAsJsonAsync("/api/sales-orders", new CreateSalesOrderRequest(customer!.Id));
        var order = await orderResponse.Content.ReadFromJsonAsync<SalesOrderDto>();
        await client.PostAsJsonAsync($"/api/sales-orders/{order!.Id}/lines", new AddSalesOrderLineRequest(product!.Id, Quantity: 1));

        var approveResponse = await client.PostAsJsonAsync($"/api/sales-orders/{order.Id}/approve",
            new ApproveSalesOrderRequest(InstallmentCount: 1, FirstDueDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30))));

        var receivablesResponse = await client.GetAsync("/api/receivables");
        var receivables = await receivablesResponse.Content.ReadFromJsonAsync<List<ReceivableDto>>();
        var receivable = receivables!.Single(r => r.SalesOrderId == order.Id);
        var installment = receivable.Installments[0];

        var balanceBeforeResponse = await client.GetAsync("/api/reports/dashboard-summary");
        var balanceBefore = await balanceBeforeResponse.Content.ReadFromJsonAsync<DashboardSummaryDto>();

        var receiptRequest = new HttpRequestMessage(
            HttpMethod.Post, $"/api/receivables/{receivable.Id}/installments/{installment.Id}/receipts")
        {
            Content = JsonContent.Create(new ApplyReceiptRequest(300m, installment.Version)),
        };
        receiptRequest.Headers.Add("Idempotency-Key", $"report-test-{Guid.NewGuid():N}");
        var receiptResponse = await client.SendAsync(receiptRequest);
        receiptResponse.EnsureSuccessStatusCode();

        var balanceAfterResponse = await client.GetAsync("/api/reports/dashboard-summary");
        var balanceAfter = await balanceAfterResponse.Content.ReadFromJsonAsync<DashboardSummaryDto>();

        Assert.Equal(balanceBefore!.CurrentBalance + 300m, balanceAfter!.CurrentBalance);

        var cashFlowResponse = await client.GetAsync("/api/reports/cash-flow?groupByDay=true");
        cashFlowResponse.EnsureSuccessStatusCode();
        var cashFlow = await cashFlowResponse.Content.ReadFromJsonAsync<List<CashFlowPeriodDto>>();

        Assert.Contains(cashFlow!, p => p.Inflow >= 300m);
    }

    [Fact]
    public async Task OverdueInstallment_AppearsInOverdueSummaryAndUpcomingDue()
    {
        var client = await CreateAuthenticatedClientAsync();

        var customerResponse = await client.PostAsJsonAsync("/api/customers", new CreateCustomerRequest(
            "Cliente Vencido", $"CPF-{Guid.NewGuid():N}", null, null));
        var customer = await customerResponse.Content.ReadFromJsonAsync<CustomerDto>();

        var productResponse = await client.PostAsJsonAsync("/api/products", new CreateProductRequest(
            $"SKU-{Guid.NewGuid():N}"[..12], "Produto Vencido", 150m, null));
        var product = await productResponse.Content.ReadFromJsonAsync<ProductDto>();

        var orderResponse = await client.PostAsJsonAsync("/api/sales-orders", new CreateSalesOrderRequest(customer!.Id));
        var order = await orderResponse.Content.ReadFromJsonAsync<SalesOrderDto>();
        await client.PostAsJsonAsync($"/api/sales-orders/{order!.Id}/lines", new AddSalesOrderLineRequest(product!.Id, Quantity: 1));

        // Due date in the past - installment is immediately overdue.
        var pastDueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5));
        await client.PostAsJsonAsync($"/api/sales-orders/{order.Id}/approve",
            new ApproveSalesOrderRequest(InstallmentCount: 1, FirstDueDate: pastDueDate));

        var overdueResponse = await client.GetAsync("/api/reports/overdue");
        overdueResponse.EnsureSuccessStatusCode();
        var overdue = await overdueResponse.Content.ReadFromJsonAsync<OverdueSummaryDto>();

        Assert.True(overdue!.ReceivablesCount >= 1);
        Assert.True(overdue.ReceivablesAmount >= 150m);

        var upcomingResponse = await client.GetAsync("/api/reports/upcoming-due?days=30");
        upcomingResponse.EnsureSuccessStatusCode();
        var buckets = await upcomingResponse.Content.ReadFromJsonAsync<List<DueBucketDto>>();

        var overdueBucket = buckets!.Single(b => b.Bucket == "Overdue");
        Assert.True(overdueBucket.ReceivablesCount >= 1);
    }
}
