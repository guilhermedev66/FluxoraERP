using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Fluxora.Api.Controllers;
using Fluxora.Application.Automation;
using Fluxora.Application.Catalog;
using Fluxora.Application.Customers;
using Fluxora.Application.Finance;
using Fluxora.Application.Sales;
using Fluxora.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Fluxora.IntegrationTests.Concurrency;

/// <summary>
/// Deterministic PostgreSQL-backed regression coverage for the race the overdue sweep fix
/// (commit "fix(automation): make overdue sweep resilient to concurrent payments") was written
/// against, but shipped without a real Testcontainers-verified test for: a payment landing on the
/// same installment the sweep is concurrently transitioning. The prior batch design committed every
/// installment's transition in a single SaveChanges, so a version conflict on one installment rolled
/// back every other legitimately-marked installment in the same run; the fix moved each installment
/// to its own transaction. This proves that specifically: a real lock contention on one installment
/// resolves cleanly (as a 409, not a crash or silent corruption) and does not affect an unrelated
/// installment processed in the same sweep.
/// </summary>
public class OverdueSweepConcurrencyTests(FluxoraApiFactory factory) : IClassFixture<FluxoraApiFactory>
{
    [Fact]
    public async Task Sweep_RacingPaymentOnSameInstallment_ResolvesCleanlyWithoutAffectingUnrelatedInstallment()
    {
        var client = await CreateAuthenticatedClientAsync();
        var pastDueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5));

        var raced = await CreateOverdueEligibleReceivableAsync(client, pastDueDate);
        var unrelated = await CreateOverdueEligibleReceivableAsync(client, pastDueDate);
        var racedInstallment = raced.Installments.Single();
        await using var delay = await InstallUpdateDelayAsync(racedInstallment.Id);

        // Give the sweep a head start so it is deterministically the one holding the row lock
        // (sleeping inside its UPDATE) once the payment's own UPDATE reaches the same row.
        var sweepTask = RunSweepAsync();
        await Task.Delay(TimeSpan.FromMilliseconds(300));
        var paymentResponse = await ApplyReceiptAsync(
            client, raced.Id, racedInstallment.Id, racedInstallment.Amount, racedInstallment.Version);
        var sweepResult = await sweepTask;

        // The sweep wins the row: the payment's optimistic-concurrency check must fail cleanly
        // (409), never silently overwrite the sweep's transition or crash the request.
        Assert.Equal(HttpStatusCode.Conflict, paymentResponse.StatusCode);

        var racedAfterSweep = await client.GetFromJsonAsync<ReceivableDto>($"/api/receivables/{raced.Id}");
        var racedInstallmentAfterSweep = racedAfterSweep!.Installments.Single();
        Assert.Equal("Overdue", racedInstallmentAfterSweep.Status);
        Assert.Equal(racedInstallment.Version + 1, racedInstallmentAfterSweep.Version);

        // The core regression: contention on one installment must not roll back or block the
        // unrelated installment processed in the same sweep run.
        var unrelatedAfter = await client.GetFromJsonAsync<ReceivableDto>($"/api/receivables/{unrelated.Id}");
        Assert.Equal("Overdue", unrelatedAfter!.Installments.Single().Status);
        Assert.True(sweepResult.ReceivablesMarked >= 1);

        // The client can recover: retrying with the current version now succeeds and correctly
        // transitions Overdue -> Paid (Overdue does not block receipt application).
        var retryResponse = await ApplyReceiptAsync(
            client, raced.Id, racedInstallment.Id, racedInstallmentAfterSweep.Amount, racedInstallmentAfterSweep.Version);
        Assert.Equal(HttpStatusCode.Created, retryResponse.StatusCode);
        var retryReceipt = await retryResponse.Content.ReadFromJsonAsync<ReceiptDto>();
        Assert.Equal("Paid", retryReceipt!.InstallmentStatus);
    }

    private async Task<OverdueProcessingResult> RunSweepAsync()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<OverdueProcessingService>();
        return await service.ProcessAsync();
    }

    private static async Task<HttpResponseMessage> ApplyReceiptAsync(
        HttpClient client, Guid receivableId, Guid installmentId, decimal amount, int expectedVersion)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Post, $"/api/receivables/{receivableId}/installments/{installmentId}/receipts")
        {
            Content = JsonContent.Create(new ApplyReceiptRequest(amount, expectedVersion)),
        };
        request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString("N"));
        return await client.SendAsync(request);
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

    private static async Task<ReceivableDto> CreateOverdueEligibleReceivableAsync(HttpClient client, DateOnly dueDate)
    {
        var customerResponse = await client.PostAsJsonAsync("/api/customers", new CreateCustomerRequest(
            "Cliente Sweep", TestData.UniqueDocument(), null, null));
        customerResponse.EnsureSuccessStatusCode();
        var customer = await customerResponse.Content.ReadFromJsonAsync<CustomerDto>();

        var productResponse = await client.PostAsJsonAsync("/api/products", new CreateProductRequest(
            $"SKU-{Guid.NewGuid():N}"[..12], "Produto Sweep", 100m, "Testes"));
        productResponse.EnsureSuccessStatusCode();
        var product = await productResponse.Content.ReadFromJsonAsync<ProductDto>();

        var orderResponse = await client.PostAsJsonAsync("/api/sales-orders", new CreateSalesOrderRequest(customer!.Id));
        orderResponse.EnsureSuccessStatusCode();
        var order = await orderResponse.Content.ReadFromJsonAsync<SalesOrderDto>();

        var lineResponse = await client.PostAsJsonAsync(
            $"/api/sales-orders/{order!.Id}/lines", new AddSalesOrderLineRequest(product!.Id, 1));
        lineResponse.EnsureSuccessStatusCode();

        var approveResponse = await client.PostAsJsonAsync(
            $"/api/sales-orders/{order.Id}/approve", new ApproveSalesOrderRequest(1, dueDate));
        approveResponse.EnsureSuccessStatusCode();

        var receivables = await client.GetFromJsonAsync<List<ReceivableDto>>("/api/receivables");
        return receivables!.Single(item => item.SalesOrderId == order.Id);
    }

    private async Task<IAsyncDisposable> InstallUpdateDelayAsync(Guid installmentId)
    {
        var suffix = Guid.NewGuid().ToString("N");
        var functionName = $"test_delay_{suffix}";
        var triggerName = $"test_delay_{suffix}";

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
#pragma warning disable EF1002 // Identifiers are generated GUIDs; table is fixed, not user input.
            await db.Database.ExecuteSqlRawAsync(
                $"""
                CREATE FUNCTION "{functionName}"() RETURNS trigger
                LANGUAGE plpgsql AS $function$
                BEGIN
                    IF NEW."Id" = '{installmentId:D}'::uuid THEN
                        PERFORM pg_sleep(1);
                    END IF;
                    RETURN NEW;
                END;
                $function$;
                CREATE TRIGGER "{triggerName}"
                BEFORE UPDATE ON "ReceivableInstallments"
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
                DROP TRIGGER IF EXISTS "{triggerName}" ON "ReceivableInstallments";
                DROP FUNCTION IF EXISTS "{functionName}"();
                """);
#pragma warning restore EF1002
        });
    }

    private sealed class AsyncCleanup(Func<Task> cleanup) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync() => await cleanup();
    }
}
