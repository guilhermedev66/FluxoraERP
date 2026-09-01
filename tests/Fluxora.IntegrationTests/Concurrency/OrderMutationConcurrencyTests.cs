using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Fluxora.Api.Controllers;
using Fluxora.Application.Catalog;
using Fluxora.Application.Customers;
using Fluxora.Application.Finance;
using Fluxora.Application.Purchasing;
using Fluxora.Application.Sales;
using Fluxora.Application.Suppliers;
using Fluxora.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Fluxora.IntegrationTests.Concurrency;

public class OrderMutationConcurrencyTests(FluxoraApiFactory factory) : IClassFixture<FluxoraApiFactory>
{
    [Fact]
    public async Task ConcurrentSalesAddLine_OneLosesAndPersistedTotalMatchesLines()
    {
        var client = await CreateAuthenticatedClientAsync();
        var (order, product) = await CreateSalesOrderAsync(client, withInitialLine: false);
        await using var delay = await InstallUpdateDelayAsync("SalesOrders", order.Id);

        var responses = await SendConcurrentlyAsync(
            () => client.PostAsJsonAsync($"/api/sales-orders/{order.Id}/lines", new AddSalesOrderLineRequest(product.Id, 1)),
            () => client.PostAsJsonAsync($"/api/sales-orders/{order.Id}/lines", new AddSalesOrderLineRequest(product.Id, 2)));

        AssertSingleWinner(responses);
        var persisted = await client.GetFromJsonAsync<SalesOrderDto>($"/api/sales-orders/{order.Id}");
        Assert.Equal(persisted!.Lines.Sum(line => line.LineTotal), persisted.Total);
        Assert.Single(persisted.Lines);
    }

    [Fact]
    public async Task ConcurrentPurchaseAddLine_OneLosesAndPersistedTotalMatchesLines()
    {
        var client = await CreateAuthenticatedClientAsync();
        var (order, product) = await CreatePurchaseOrderAsync(client, withInitialLine: false);
        await using var delay = await InstallUpdateDelayAsync("PurchaseOrders", order.Id);

        var responses = await SendConcurrentlyAsync(
            () => client.PostAsJsonAsync($"/api/purchase-orders/{order.Id}/lines", new AddPurchaseOrderLineRequest(product.Id, 1, 25m)),
            () => client.PostAsJsonAsync($"/api/purchase-orders/{order.Id}/lines", new AddPurchaseOrderLineRequest(product.Id, 2, 25m)));

        AssertSingleWinner(responses);
        var persisted = await client.GetFromJsonAsync<PurchaseOrderDto>($"/api/purchase-orders/{order.Id}");
        Assert.Equal(persisted!.Lines.Sum(line => line.LineTotal), persisted.Total);
        Assert.Single(persisted.Lines);
    }

    [Fact]
    public async Task ConcurrentSalesAddLineAndApprove_OneLosesAndFinancialStateRemainsConsistent()
    {
        var client = await CreateAuthenticatedClientAsync();
        var (order, product) = await CreateSalesOrderAsync(client, withInitialLine: true);
        await using var delay = await InstallUpdateDelayAsync("SalesOrders", order.Id);

        var responses = await SendConcurrentlyAsync(
            () => client.PostAsJsonAsync($"/api/sales-orders/{order.Id}/lines", new AddSalesOrderLineRequest(product.Id, 2)),
            () => client.PostAsJsonAsync($"/api/sales-orders/{order.Id}/approve",
                new ApproveSalesOrderRequest(1, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)))));

        AssertSingleWinner(responses);
        var persisted = await client.GetFromJsonAsync<SalesOrderDto>($"/api/sales-orders/{order.Id}");
        Assert.Equal(persisted!.Lines.Sum(line => line.LineTotal), persisted.Total);

        var receivables = await client.GetFromJsonAsync<List<ReceivableDto>>("/api/receivables");
        var receivable = receivables!.SingleOrDefault(item => item.SalesOrderId == order.Id);
        if (persisted.Status == "Approved")
        {
            Assert.NotNull(receivable);
            Assert.Equal(persisted.Total, receivable.TotalAmount);
        }
        else
        {
            Assert.Null(receivable);
        }
    }

    [Fact]
    public async Task ConcurrentPurchaseAddLineAndConfirm_OneLosesAndFinancialStateRemainsConsistent()
    {
        var client = await CreateAuthenticatedClientAsync();
        var (order, product) = await CreatePurchaseOrderAsync(client, withInitialLine: true);
        await using var delay = await InstallUpdateDelayAsync("PurchaseOrders", order.Id);

        var responses = await SendConcurrentlyAsync(
            () => client.PostAsJsonAsync($"/api/purchase-orders/{order.Id}/lines", new AddPurchaseOrderLineRequest(product.Id, 2, 25m)),
            () => client.PostAsJsonAsync($"/api/purchase-orders/{order.Id}/confirm",
                new ConfirmPurchaseOrderRequest(1, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)))));

        AssertSingleWinner(responses);
        var persisted = await client.GetFromJsonAsync<PurchaseOrderDto>($"/api/purchase-orders/{order.Id}");
        Assert.Equal(persisted!.Lines.Sum(line => line.LineTotal), persisted.Total);

        var payables = await client.GetFromJsonAsync<List<PayableDto>>("/api/payables");
        var payable = payables!.SingleOrDefault(item => item.PurchaseOrderId == order.Id);
        if (persisted.Status == "Confirmed")
        {
            Assert.NotNull(payable);
            Assert.Equal(persisted.Total, payable.TotalAmount);
        }
        else
        {
            Assert.Null(payable);
        }
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var client = factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(
            FluxoraApiFactory.AdminEmail, FluxoraApiFactory.AdminPassword));
        login.EnsureSuccessStatusCode();
        var payload = await login.Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", payload!.AccessToken);
        return client;
    }

    private static async Task<(SalesOrderDto Order, ProductDto Product)> CreateSalesOrderAsync(
        HttpClient client, bool withInitialLine)
    {
        var customerResponse = await client.PostAsJsonAsync("/api/customers", new CreateCustomerRequest(
            "Cliente Concorrencia", TestData.UniqueDocument(), null, null));
        customerResponse.EnsureSuccessStatusCode();
        var customer = await customerResponse.Content.ReadFromJsonAsync<CustomerDto>();

        var productResponse = await client.PostAsJsonAsync("/api/products", new CreateProductRequest(
            $"SKU-{Guid.NewGuid():N}"[..12], "Produto Concorrencia", 25m, "Testes"));
        productResponse.EnsureSuccessStatusCode();
        var product = await productResponse.Content.ReadFromJsonAsync<ProductDto>();

        var orderResponse = await client.PostAsJsonAsync("/api/sales-orders", new CreateSalesOrderRequest(customer!.Id));
        orderResponse.EnsureSuccessStatusCode();
        var order = await orderResponse.Content.ReadFromJsonAsync<SalesOrderDto>();

        if (withInitialLine)
        {
            var lineResponse = await client.PostAsJsonAsync(
                $"/api/sales-orders/{order!.Id}/lines", new AddSalesOrderLineRequest(product!.Id, 1));
            lineResponse.EnsureSuccessStatusCode();
            order = await lineResponse.Content.ReadFromJsonAsync<SalesOrderDto>();
        }

        return (order!, product!);
    }

    private static async Task<(PurchaseOrderDto Order, ProductDto Product)> CreatePurchaseOrderAsync(
        HttpClient client, bool withInitialLine)
    {
        var supplierResponse = await client.PostAsJsonAsync("/api/suppliers", new CreateSupplierRequest(
            "Fornecedor Concorrencia", TestData.UniqueDocument(), null, null));
        supplierResponse.EnsureSuccessStatusCode();
        var supplier = await supplierResponse.Content.ReadFromJsonAsync<SupplierDto>();

        var productResponse = await client.PostAsJsonAsync("/api/products", new CreateProductRequest(
            $"SKU-{Guid.NewGuid():N}"[..12], "Insumo Concorrencia", 25m, "Testes"));
        productResponse.EnsureSuccessStatusCode();
        var product = await productResponse.Content.ReadFromJsonAsync<ProductDto>();

        var orderResponse = await client.PostAsJsonAsync("/api/purchase-orders", new CreatePurchaseOrderRequest(supplier!.Id));
        orderResponse.EnsureSuccessStatusCode();
        var order = await orderResponse.Content.ReadFromJsonAsync<PurchaseOrderDto>();

        if (withInitialLine)
        {
            var lineResponse = await client.PostAsJsonAsync(
                $"/api/purchase-orders/{order!.Id}/lines", new AddPurchaseOrderLineRequest(product!.Id, 1, 25m));
            lineResponse.EnsureSuccessStatusCode();
            order = await lineResponse.Content.ReadFromJsonAsync<PurchaseOrderDto>();
        }

        return (order!, product!);
    }

    private async Task<IAsyncDisposable> InstallUpdateDelayAsync(string table, Guid orderId)
    {
        var allowedTable = table switch
        {
            "SalesOrders" => "SalesOrders",
            "PurchaseOrders" => "PurchaseOrders",
            _ => throw new ArgumentOutOfRangeException(nameof(table)),
        };
        var suffix = Guid.NewGuid().ToString("N");
        var functionName = $"test_delay_{suffix}";
        var triggerName = $"test_delay_{suffix}";

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
#pragma warning disable EF1002 // Identifiers are generated GUIDs; table is selected from a fixed allowlist above.
            await db.Database.ExecuteSqlRawAsync(
                $"""
                CREATE FUNCTION "{functionName}"() RETURNS trigger
                LANGUAGE plpgsql AS $function$
                BEGIN
                    IF NEW."Id" = '{orderId:D}'::uuid THEN
                        PERFORM pg_sleep(1);
                    END IF;
                    RETURN NEW;
                END;
                $function$;
                CREATE TRIGGER "{triggerName}"
                BEFORE UPDATE ON "{allowedTable}"
                FOR EACH ROW EXECUTE FUNCTION "{functionName}"();
                """);
#pragma warning restore EF1002
        }

        return new AsyncCleanup(async () =>
        {
            await using var scope = factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
#pragma warning disable EF1002 // Identifiers are the same generated/allowlisted values used during setup.
            await db.Database.ExecuteSqlRawAsync(
                $"""
                DROP TRIGGER IF EXISTS "{triggerName}" ON "{allowedTable}";
                DROP FUNCTION IF EXISTS "{functionName}"();
                """);
#pragma warning restore EF1002
        });
    }

    private static async Task<HttpResponseMessage[]> SendConcurrentlyAsync(
        Func<Task<HttpResponseMessage>> firstRequest,
        Func<Task<HttpResponseMessage>> secondRequest)
    {
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task<HttpResponseMessage> SendAsync(Func<Task<HttpResponseMessage>> request)
        {
            await start.Task;
            return await request();
        }

        var first = SendAsync(firstRequest);
        var second = SendAsync(secondRequest);
        start.SetResult();
        return await Task.WhenAll(first, second);
    }

    private static void AssertSingleWinner(IReadOnlyCollection<HttpResponseMessage> responses)
    {
        Assert.Equal(1, responses.Count(response => response.IsSuccessStatusCode));
        Assert.Equal(1, responses.Count(response => response.StatusCode == HttpStatusCode.Conflict));
    }

    private sealed class AsyncCleanup(Func<Task> cleanup) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync() => await cleanup();
    }
}
