using System.Net.Http.Headers;
using System.Net.Http.Json;
using Fluxora.Api.Controllers;
using Fluxora.Application.Catalog;
using Fluxora.Application.Common;
using Fluxora.Application.Customers;
using Fluxora.Application.Finance;
using Fluxora.Application.Purchasing;
using Fluxora.Application.Reporting;
using Fluxora.Application.Sales;
using Fluxora.Application.Suppliers;
using Fluxora.Domain.Catalog;
using Fluxora.Domain.Customers;
using Fluxora.Domain.Finance;
using Fluxora.Domain.Purchasing;
using Fluxora.Domain.Sales;
using Fluxora.Domain.Suppliers;
using Fluxora.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

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
            "Cliente Relatorios", TestData.UniqueDocument(), null, null));
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
            "Cliente Caixa", TestData.UniqueDocument(), null, null));
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
            "Cliente Vencido", TestData.UniqueDocument(), null, null));
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

    [Fact]
    public async Task Reports_BucketUtcInstantsByBusinessLocalCalendar()
    {
        const decimal revenueAmount = 4321.23m;
        const decimal expenseAmount = 3210.12m;
        const decimal cashAmount = 2109.01m;
        var boundaryInstant = new DateTime(2026, 9, 1, 0, 30, 0, DateTimeKind.Utc);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var product = Product.Create($"TZ-{Guid.NewGuid():N}"[..12], "Produto Fuso", 1m, "Fuso");
            var customer = Customer.Create("Cliente Fuso", TestData.UniqueDocument(), null, null);
            var supplier = Supplier.Create("Fornecedor Fuso", TestData.UniqueDocument(), null, null);

            var sale = SalesOrder.CreateDraft(customer.Id, Guid.Empty);
            sale.AddLine(product.Id, product.Name, 1, revenueAmount);
            sale.Approve();
            var purchase = PurchaseOrder.CreateDraft(supplier.Id, Guid.Empty);
            purchase.AddLine(product.Id, product.Name, 1, expenseAmount, product.Category);
            purchase.Confirm();
            var movement = CashMovement.For(CashMovementDirection.Inflow, cashAmount, "TimezoneTest", Guid.NewGuid());

            db.AddRange(product, customer, supplier, sale, purchase, movement);
            db.Entry(sale).Property(order => order.ApprovedAtUtc).CurrentValue = boundaryInstant;
            db.Entry(purchase).Property(order => order.ConfirmedAtUtc).CurrentValue = boundaryInstant;
            db.Entry(movement).Property(entry => entry.OccurredAtUtc).CurrentValue = boundaryInstant;
            await db.SaveChangesAsync();
        }

        var client = await CreateAuthenticatedClientAsync();
        const string range = "from=2026-08-31&to=2026-08-31";
        var revenue = await client.GetFromJsonAsync<List<PeriodAmountDto>>($"/api/reports/revenue?{range}");
        var expenses = await client.GetFromJsonAsync<List<PeriodAmountDto>>($"/api/reports/expenses?{range}");
        var dailyCash = await client.GetFromJsonAsync<List<CashFlowPeriodDto>>($"/api/reports/cash-flow?{range}&groupByDay=true");
        var monthlyCash = await client.GetFromJsonAsync<List<CashFlowPeriodDto>>($"/api/reports/cash-flow?{range}");

        Assert.True(revenue!.Single(row => row.Period == "2026-08").Amount >= revenueAmount);
        Assert.True(expenses!.Single(row => row.Period == "2026-08").Amount >= expenseAmount);
        Assert.True(dailyCash!.Single(row => row.Period == "2026-08-31").Inflow >= cashAmount);
        Assert.True(monthlyCash!.Single(row => row.Period == "2026-08").Inflow >= cashAmount);
    }

    [Fact]
    public async Task ExpensesByCategory_PreservesCategoryCapturedWhenLineWasAdded()
    {
        var client = await CreateAuthenticatedClientAsync();
        var categoryA = $"Categoria-A-{Guid.NewGuid():N}";
        var categoryB = $"Categoria-B-{Guid.NewGuid():N}";

        var supplierResponse = await client.PostAsJsonAsync("/api/suppliers", new CreateSupplierRequest(
            "Fornecedor Categoria", TestData.UniqueDocument(), null, null));
        var supplier = await supplierResponse.Content.ReadFromJsonAsync<SupplierDto>();
        var productResponse = await client.PostAsJsonAsync("/api/products", new CreateProductRequest(
            $"CAT-{Guid.NewGuid():N}"[..12], "Produto Categoria", 80m, categoryA));
        var product = await productResponse.Content.ReadFromJsonAsync<ProductDto>();
        var orderResponse = await client.PostAsJsonAsync("/api/purchase-orders", new CreatePurchaseOrderRequest(supplier!.Id));
        var order = await orderResponse.Content.ReadFromJsonAsync<PurchaseOrderDto>();

        await client.PostAsJsonAsync($"/api/purchase-orders/{order!.Id}/lines",
            new AddPurchaseOrderLineRequest(product!.Id, 2, 40m));
        var confirm = await client.PostAsJsonAsync($"/api/purchase-orders/{order.Id}/confirm",
            new ConfirmPurchaseOrderRequest(1, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30))));
        confirm.EnsureSuccessStatusCode();
        var update = await client.PutAsJsonAsync($"/api/products/{product.Id}",
            new UpdateProductRequest(product.Name, product.Price, categoryB));
        update.EnsureSuccessStatusCode();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var categories = await client.GetFromJsonAsync<List<CategoryExpenseDto>>(
            $"/api/reports/expenses-by-category?from={today:yyyy-MM-dd}&to={today:yyyy-MM-dd}");

        Assert.Equal(80m, categories!.Single(row => row.Category == categoryA).Amount);
        Assert.DoesNotContain(categories!, row => row.Category == categoryB);
    }

    [Fact]
    public async Task RangeDefaults_UseCurrentBusinessMonthWhileOneSidedRangesRemainOpen()
    {
        var client = await CreateAuthenticatedClientAsync();
        var currentCustomer = Customer.Create("Cliente Mes Atual", TestData.UniqueDocument(), null, null);
        var priorCustomer = Customer.Create("Cliente Mes Anterior", TestData.UniqueDocument(), null, null);
        var product = Product.Create($"RNG-{Guid.NewGuid():N}"[..12], "Produto Periodo", 1m, "Periodo");
        var businessClock = factory.Services.GetRequiredService<IBusinessClock>();
        var today = businessClock.Today;
        var previousMonth = today.AddMonths(-1);
        var priorDate = new DateOnly(previousMonth.Year, previousMonth.Month, 15);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var currentOrder = SalesOrder.CreateDraft(currentCustomer.Id, Guid.Empty);
            currentOrder.AddLine(product.Id, product.Name, 1, 901.01m);
            currentOrder.Approve();
            var priorOrder = SalesOrder.CreateDraft(priorCustomer.Id, Guid.Empty);
            priorOrder.AddLine(product.Id, product.Name, 1, 815.01m);
            priorOrder.Approve();

            db.AddRange(product, currentCustomer, priorCustomer, currentOrder, priorOrder);
            db.Entry(currentOrder).Property(order => order.ApprovedAtUtc).CurrentValue =
                businessClock.StartOfDayUtc(today).AddHours(12);
            db.Entry(priorOrder).Property(order => order.ApprovedAtUtc).CurrentValue =
                businessClock.StartOfDayUtc(priorDate).AddHours(12);
            await db.SaveChangesAsync();
        }

        var defaults = await client.GetFromJsonAsync<List<TopCustomerDto>>("/api/reports/top-customers?limit=100");
        var fromOnly = await client.GetFromJsonAsync<List<TopCustomerDto>>(
            $"/api/reports/top-customers?from={priorDate:yyyy-MM-dd}&limit=100");
        var toOnly = await client.GetFromJsonAsync<List<TopCustomerDto>>(
            $"/api/reports/top-customers?to={priorDate:yyyy-MM-dd}&limit=100");
        var invalid = await client.GetAsync(
            $"/api/reports/top-customers?from={today:yyyy-MM-dd}&to={priorDate:yyyy-MM-dd}");

        Assert.Contains(defaults!, row => row.CustomerId == currentCustomer.Id);
        Assert.DoesNotContain(defaults!, row => row.CustomerId == priorCustomer.Id);
        Assert.Contains(fromOnly!, row => row.CustomerId == currentCustomer.Id);
        Assert.Contains(fromOnly!, row => row.CustomerId == priorCustomer.Id);
        Assert.DoesNotContain(toOnly!, row => row.CustomerId == currentCustomer.Id);
        Assert.Contains(toOnly!, row => row.CustomerId == priorCustomer.Id);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, invalid.StatusCode);
    }
}
