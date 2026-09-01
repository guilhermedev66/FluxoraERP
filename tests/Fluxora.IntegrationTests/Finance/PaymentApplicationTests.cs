using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Fluxora.Api.Controllers;
using Fluxora.Application.Customers;
using Fluxora.Application.Finance;
using Fluxora.Application.Purchasing;
using Fluxora.Application.Suppliers;

namespace Fluxora.IntegrationTests.Finance;

/// <summary>
/// Adversarial coverage for the payment application endpoint: idempotent retries, idempotency
/// key reuse with a different payload, stale-version rejection, and a genuine parallel-request
/// race proving two simultaneous payments cannot both apply against the same installment.
/// </summary>
public class PaymentApplicationTests(FluxoraApiFactory factory) : IClassFixture<FluxoraApiFactory>
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

    private async Task<PayableDto> CreatePayableWithOneInstallmentAsync(HttpClient client, decimal total = 100m)
    {
        var supplierResponse = await client.PostAsJsonAsync("/api/suppliers", new CreateSupplierRequest(
            "Fornecedor Pagamentos", TestData.UniqueDocument(), null, null));
        supplierResponse.EnsureSuccessStatusCode();
        var supplier = await supplierResponse.Content.ReadFromJsonAsync<SupplierDto>();

        var productResponse = await client.PostAsJsonAsync("/api/products", new Fluxora.Application.Catalog.CreateProductRequest(
            $"SKU-{Guid.NewGuid():N}"[..12], "Servico", 10m));
        var product = await productResponse.Content.ReadFromJsonAsync<Fluxora.Application.Catalog.ProductDto>();

        var orderResponse = await client.PostAsJsonAsync("/api/purchase-orders", new CreatePurchaseOrderRequest(supplier!.Id));
        var order = await orderResponse.Content.ReadFromJsonAsync<PurchaseOrderDto>();

        await client.PostAsJsonAsync($"/api/purchase-orders/{order!.Id}/lines",
            new AddPurchaseOrderLineRequest(product!.Id, Quantity: 1, UnitPrice: total));

        var confirmResponse = await client.PostAsJsonAsync($"/api/purchase-orders/{order.Id}/confirm",
            new ConfirmPurchaseOrderRequest(InstallmentCount: 1, FirstDueDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30))));
        confirmResponse.EnsureSuccessStatusCode();

        var payablesResponse = await client.GetAsync("/api/payables");
        var payables = await payablesResponse.Content.ReadFromJsonAsync<List<PayableDto>>();
        return payables!.Single(p => p.PurchaseOrderId == order.Id);
    }

    private static HttpRequestMessage BuildPaymentRequest(Guid payableId, Guid installmentId, decimal amount, int expectedVersion, string idempotencyKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/payables/{payableId}/installments/{installmentId}/payments")
        {
            Content = JsonContent.Create(new ApplyPaymentRequest(amount, expectedVersion)),
        };
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        return request;
    }

    [Fact]
    public async Task ApplyPayment_Once_MarksInstallmentPaid()
    {
        var client = await CreateAuthenticatedClientAsync();
        var payable = await CreatePayableWithOneInstallmentAsync(client);
        var installment = payable.Installments[0];

        var response = await client.SendAsync(BuildPaymentRequest(
            payable.Id, installment.Id, payable.TotalAmount, installment.Version, $"key-{Guid.NewGuid():N}"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payment = await response.Content.ReadFromJsonAsync<PaymentDto>();
        Assert.Equal("Paid", payment!.InstallmentStatus);
        Assert.Equal(0m, payment.InstallmentRemainingAmount);
    }

    [Fact]
    public async Task ApplyPayment_SameIdempotencyKeyTwice_DoesNotDoublePay()
    {
        var client = await CreateAuthenticatedClientAsync();
        var payable = await CreatePayableWithOneInstallmentAsync(client, total: 200m);
        var installment = payable.Installments[0];
        var idempotencyKey = $"key-{Guid.NewGuid():N}";

        var first = await client.SendAsync(BuildPaymentRequest(payable.Id, installment.Id, 80m, installment.Version, idempotencyKey));
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        var firstPayment = await first.Content.ReadFromJsonAsync<PaymentDto>();

        // Exact same request, same key: must replay the original response, not apply a second payment.
        var second = await client.SendAsync(BuildPaymentRequest(payable.Id, installment.Id, 80m, installment.Version, idempotencyKey));
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
        var secondPayment = await second.Content.ReadFromJsonAsync<PaymentDto>();

        Assert.Equal(firstPayment!.Id, secondPayment!.Id);

        var getResponse = await client.GetAsync($"/api/payables/{payable.Id}");
        var refreshed = await getResponse.Content.ReadFromJsonAsync<PayableDto>();
        var refreshedInstallment = refreshed!.Installments.Single(i => i.Id == installment.Id);

        Assert.Equal(80m, refreshedInstallment.AmountPaid);
        Assert.Equal(installment.Version + 1, refreshedInstallment.Version);
    }

    [Fact]
    public async Task ApplyPayment_TwoConcurrentRequestsWithSameKey_ReplayExactResponse()
    {
        var client = await CreateAuthenticatedClientAsync();
        var payable = await CreatePayableWithOneInstallmentAsync(client, total: 200m);
        var installment = payable.Installments[0];
        var idempotencyKey = $"same-race-{Guid.NewGuid():N}";

        var task1 = client.SendAsync(BuildPaymentRequest(
            payable.Id, installment.Id, 80m, installment.Version, idempotencyKey));
        var task2 = client.SendAsync(BuildPaymentRequest(
            payable.Id, installment.Id, 80m, installment.Version, idempotencyKey));

        var responses = await Task.WhenAll(task1, task2);
        Assert.All(responses, response => Assert.Equal(HttpStatusCode.Created, response.StatusCode));

        var payments = await Task.WhenAll(responses.Select(response =>
            response.Content.ReadFromJsonAsync<PaymentDto>()));
        Assert.Equal(payments[0]!.Id, payments[1]!.Id);
        Assert.Equal(payments[0], payments[1]);

        var refreshed = await (await client.GetAsync($"/api/payables/{payable.Id}"))
            .Content.ReadFromJsonAsync<PayableDto>();
        var refreshedInstallment = refreshed!.Installments.Single(i => i.Id == installment.Id);
        Assert.Equal(80m, refreshedInstallment.AmountPaid);
        Assert.Equal(installment.Version + 1, refreshedInstallment.Version);
    }

    [Fact]
    public async Task ApplyPayment_SameIdempotencyKeyDifferentPayload_ReturnsConflict()
    {
        var client = await CreateAuthenticatedClientAsync();
        var payable = await CreatePayableWithOneInstallmentAsync(client, total: 200m);
        var installment = payable.Installments[0];
        var idempotencyKey = $"key-{Guid.NewGuid():N}";

        var first = await client.SendAsync(BuildPaymentRequest(payable.Id, installment.Id, 50m, installment.Version, idempotencyKey));
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await client.SendAsync(BuildPaymentRequest(payable.Id, installment.Id, 60m, installment.Version, idempotencyKey));
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task ApplyPayment_WithStaleVersion_ReturnsConflict()
    {
        var client = await CreateAuthenticatedClientAsync();
        var payable = await CreatePayableWithOneInstallmentAsync(client, total: 200m);
        var installment = payable.Installments[0];

        var first = await client.SendAsync(BuildPaymentRequest(
            payable.Id, installment.Id, 50m, installment.Version, $"key-{Guid.NewGuid():N}"));
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        // Second attempt still claims the ORIGINAL (now stale) version.
        var second = await client.SendAsync(BuildPaymentRequest(
            payable.Id, installment.Id, 50m, installment.Version, $"key-{Guid.NewGuid():N}"));

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task ApplyPayment_InstallmentFromDifferentPayable_ReturnsNotFoundWithoutMutation()
    {
        var client = await CreateAuthenticatedClientAsync();
        var routePayable = await CreatePayableWithOneInstallmentAsync(client, total: 100m);
        var otherPayable = await CreatePayableWithOneInstallmentAsync(client, total: 200m);
        var otherInstallment = otherPayable.Installments[0];

        var response = await client.SendAsync(BuildPaymentRequest(
            routePayable.Id,
            otherInstallment.Id,
            50m,
            otherInstallment.Version,
            $"wrong-parent-{Guid.NewGuid():N}"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var refreshed = await (await client.GetAsync($"/api/payables/{otherPayable.Id}"))
            .Content.ReadFromJsonAsync<PayableDto>();
        Assert.Equal(0m, refreshed!.Installments.Single(i => i.Id == otherInstallment.Id).AmountPaid);
    }

    [Fact]
    public async Task ApplyPayment_ExceedingRemainingBalance_ReturnsConflict()
    {
        var client = await CreateAuthenticatedClientAsync();
        var payable = await CreatePayableWithOneInstallmentAsync(client, total: 100m);
        var installment = payable.Installments[0];

        var response = await client.SendAsync(BuildPaymentRequest(
            payable.Id, installment.Id, 150m, installment.Version, $"key-{Guid.NewGuid():N}"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Theory]
    [InlineData(0.004)]
    [InlineData(9.999)]
    public async Task ApplyPayment_WithFractionalCent_ReturnsBadRequestWithoutChangingInstallment(decimal amount)
    {
        var client = await CreateAuthenticatedClientAsync();
        var payable = await CreatePayableWithOneInstallmentAsync(client, total: 100m);
        var installment = payable.Installments[0];

        var response = await client.SendAsync(BuildPaymentRequest(
            payable.Id, installment.Id, amount, installment.Version, $"key-{Guid.NewGuid():N}"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var refreshed = await (await client.GetAsync($"/api/payables/{payable.Id}"))
            .Content.ReadFromJsonAsync<PayableDto>();
        var refreshedInstallment = refreshed!.Installments.Single(i => i.Id == installment.Id);
        Assert.Equal(0m, refreshedInstallment.AmountPaid);
        Assert.Equal(installment.Version, refreshedInstallment.Version);
    }

    [Fact]
    public async Task ApplyPayment_TwoTrulyConcurrentRequests_OnlyOneSucceeds()
    {
        var client = await CreateAuthenticatedClientAsync();
        var payable = await CreatePayableWithOneInstallmentAsync(client, total: 100m);
        var installment = payable.Installments[0];

        // Both requests read the SAME starting Version and race to apply 60 each - if both
        // succeeded the installment would be overpaid (120 against a 100 balance). Distinct
        // idempotency keys so the idempotency layer can't be the thing that blocks the second one -
        // only the Version-based concurrency guard is allowed to decide the outcome here.
        var task1 = client.SendAsync(BuildPaymentRequest(payable.Id, installment.Id, 60m, installment.Version, $"race-a-{Guid.NewGuid():N}"));
        var task2 = client.SendAsync(BuildPaymentRequest(payable.Id, installment.Id, 60m, installment.Version, $"race-b-{Guid.NewGuid():N}"));

        var responses = await Task.WhenAll(task1, task2);

        var successCount = responses.Count(r => r.StatusCode == HttpStatusCode.Created);
        var conflictCount = responses.Count(r => r.StatusCode == HttpStatusCode.Conflict);

        Assert.Equal(1, successCount);
        Assert.Equal(1, conflictCount);

        var getResponse = await client.GetAsync($"/api/payables/{payable.Id}");
        var refreshed = await getResponse.Content.ReadFromJsonAsync<PayableDto>();
        var refreshedInstallment = refreshed!.Installments.Single(i => i.Id == installment.Id);

        // The critical assertion: exactly one payment landed, never both.
        Assert.Equal(60m, refreshedInstallment.AmountPaid);
        Assert.Equal(installment.Version + 1, refreshedInstallment.Version);
    }
}
