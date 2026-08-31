using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Fluxora.Api.Controllers;
using Fluxora.Application.Customers;
using Fluxora.Application.Finance;
using Fluxora.Application.Sales;

namespace Fluxora.IntegrationTests.Finance;

/// <summary>
/// Mirrors PaymentApplicationTests for the receivable side: idempotent retries and a genuine
/// parallel-request race proving two simultaneous receipts cannot both apply against the same
/// installment.
/// </summary>
public class ReceiptApplicationTests(FluxoraApiFactory factory) : IClassFixture<FluxoraApiFactory>
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

    private async Task<ReceivableDto> CreateReceivableWithOneInstallmentAsync(HttpClient client, decimal total = 100m)
    {
        var customerResponse = await client.PostAsJsonAsync("/api/customers", new CreateCustomerRequest(
            "Cliente Recebimentos", TestData.UniqueDocument(), null, null));
        var customer = await customerResponse.Content.ReadFromJsonAsync<CustomerDto>();

        var productResponse = await client.PostAsJsonAsync("/api/products", new Fluxora.Application.Catalog.CreateProductRequest(
            $"SKU-{Guid.NewGuid():N}"[..12], "Produto", total));
        var product = await productResponse.Content.ReadFromJsonAsync<Fluxora.Application.Catalog.ProductDto>();

        var orderResponse = await client.PostAsJsonAsync("/api/sales-orders", new CreateSalesOrderRequest(customer!.Id));
        var order = await orderResponse.Content.ReadFromJsonAsync<SalesOrderDto>();

        await client.PostAsJsonAsync($"/api/sales-orders/{order!.Id}/lines", new AddSalesOrderLineRequest(product!.Id, Quantity: 1));

        var approveResponse = await client.PostAsJsonAsync($"/api/sales-orders/{order.Id}/approve",
            new ApproveSalesOrderRequest(InstallmentCount: 1, FirstDueDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30))));
        approveResponse.EnsureSuccessStatusCode();

        var receivablesResponse = await client.GetAsync("/api/receivables");
        var receivables = await receivablesResponse.Content.ReadFromJsonAsync<List<ReceivableDto>>();
        return receivables!.Single(r => r.SalesOrderId == order.Id);
    }

    private static HttpRequestMessage BuildReceiptRequest(Guid receivableId, Guid installmentId, decimal amount, int expectedVersion, string idempotencyKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/receivables/{receivableId}/installments/{installmentId}/receipts")
        {
            Content = JsonContent.Create(new ApplyReceiptRequest(amount, expectedVersion)),
        };
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        return request;
    }

    [Fact]
    public async Task ApplyReceipt_SameIdempotencyKeyTwice_DoesNotDoubleReceive()
    {
        var client = await CreateAuthenticatedClientAsync();
        var receivable = await CreateReceivableWithOneInstallmentAsync(client, total: 200m);
        var installment = receivable.Installments[0];
        var idempotencyKey = $"key-{Guid.NewGuid():N}";

        var first = await client.SendAsync(BuildReceiptRequest(receivable.Id, installment.Id, 80m, installment.Version, idempotencyKey));
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        var firstReceipt = await first.Content.ReadFromJsonAsync<ReceiptDto>();

        var second = await client.SendAsync(BuildReceiptRequest(receivable.Id, installment.Id, 80m, installment.Version, idempotencyKey));
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
        var secondReceipt = await second.Content.ReadFromJsonAsync<ReceiptDto>();

        Assert.Equal(firstReceipt!.Id, secondReceipt!.Id);

        var getResponse = await client.GetAsync($"/api/receivables/{receivable.Id}");
        var refreshed = await getResponse.Content.ReadFromJsonAsync<ReceivableDto>();
        Assert.Equal(80m, refreshed!.Installments.Single(i => i.Id == installment.Id).AmountPaid);
    }

    [Fact]
    public async Task ApplyReceipt_TwoTrulyConcurrentReceipts_OnlyOneSucceeds()
    {
        var client = await CreateAuthenticatedClientAsync();
        var receivable = await CreateReceivableWithOneInstallmentAsync(client, total: 100m);
        var installment = receivable.Installments[0];

        var task1 = client.SendAsync(BuildReceiptRequest(receivable.Id, installment.Id, 70m, installment.Version, $"race-a-{Guid.NewGuid():N}"));
        var task2 = client.SendAsync(BuildReceiptRequest(receivable.Id, installment.Id, 70m, installment.Version, $"race-b-{Guid.NewGuid():N}"));

        var responses = await Task.WhenAll(task1, task2);

        Assert.Equal(1, responses.Count(r => r.StatusCode == HttpStatusCode.Created));
        Assert.Equal(1, responses.Count(r => r.StatusCode == HttpStatusCode.Conflict));

        var getResponse = await client.GetAsync($"/api/receivables/{receivable.Id}");
        var refreshed = await getResponse.Content.ReadFromJsonAsync<ReceivableDto>();
        var refreshedInstallment = refreshed!.Installments.Single(i => i.Id == installment.Id);

        Assert.Equal(70m, refreshedInstallment.AmountPaid);
        Assert.Equal(installment.Version + 1, refreshedInstallment.Version);
    }
}
