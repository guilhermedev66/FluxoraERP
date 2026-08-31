using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Fluxora.Api.Controllers;
using Fluxora.Application.Catalog;
using Fluxora.Application.Customers;
using Fluxora.Application.Finance;
using Fluxora.Application.Sales;

namespace Fluxora.IntegrationTests.Sales;

/// <summary>
/// Exercises the core Fluxora story end-to-end: an approved sale must generate a receivable
/// whose installments sum exactly to the order total.
/// </summary>
public class SalesOrderApprovalGeneratesReceivableTests(FluxoraApiFactory factory) : IClassFixture<FluxoraApiFactory>
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
    public async Task ApprovingASale_GeneratesAReceivableWithMatchingInstallments()
    {
        var client = await CreateAuthenticatedClientAsync();

        var customerResponse = await client.PostAsJsonAsync("/api/customers", new CreateCustomerRequest(
            "Cliente Fluxo de Vendas", $"CPF-{Guid.NewGuid():N}", null, null));
        customerResponse.EnsureSuccessStatusCode();
        var customer = await customerResponse.Content.ReadFromJsonAsync<CustomerDto>();

        var productResponse = await client.PostAsJsonAsync("/api/products", new CreateProductRequest(
            $"SKU-{Guid.NewGuid():N}"[..12], "Consultoria Mensal", 100.00m));
        productResponse.EnsureSuccessStatusCode();
        var product = await productResponse.Content.ReadFromJsonAsync<ProductDto>();

        var orderResponse = await client.PostAsJsonAsync("/api/sales-orders", new CreateSalesOrderRequest(customer!.Id));
        orderResponse.EnsureSuccessStatusCode();
        var order = await orderResponse.Content.ReadFromJsonAsync<SalesOrderDto>();

        var addLineResponse = await client.PostAsJsonAsync($"/api/sales-orders/{order!.Id}/lines",
            new AddSalesOrderLineRequest(product!.Id, Quantity: 1));
        addLineResponse.EnsureSuccessStatusCode();

        var approveResponse = await client.PostAsJsonAsync($"/api/sales-orders/{order.Id}/approve",
            new ApproveSalesOrderRequest(InstallmentCount: 3, FirstDueDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30))));
        Assert.Equal(HttpStatusCode.OK, approveResponse.StatusCode);

        var approved = await approveResponse.Content.ReadFromJsonAsync<SalesOrderDto>();
        Assert.Equal("Approved", approved!.Status);
        Assert.Equal(100.00m, approved.Total);

        var receivablesResponse = await client.GetAsync("/api/receivables");
        receivablesResponse.EnsureSuccessStatusCode();
        var receivables = await receivablesResponse.Content.ReadFromJsonAsync<List<ReceivableDto>>();

        var receivable = receivables!.Single(r => r.SalesOrderId == order.Id);
        Assert.Equal(100.00m, receivable.TotalAmount);
        Assert.Equal(3, receivable.Installments.Count);
        Assert.Equal(100.00m, receivable.Installments.Sum(i => i.Amount));
        Assert.All(receivable.Installments, i => Assert.Equal("Pending", i.Status));
    }

    [Fact]
    public async Task Approving_WithoutLines_ReturnsConflict()
    {
        var client = await CreateAuthenticatedClientAsync();

        var customerResponse = await client.PostAsJsonAsync("/api/customers", new CreateCustomerRequest(
            "Cliente Sem Linhas", $"CPF-{Guid.NewGuid():N}", null, null));
        var customer = await customerResponse.Content.ReadFromJsonAsync<CustomerDto>();

        var orderResponse = await client.PostAsJsonAsync("/api/sales-orders", new CreateSalesOrderRequest(customer!.Id));
        var order = await orderResponse.Content.ReadFromJsonAsync<SalesOrderDto>();

        var approveResponse = await client.PostAsJsonAsync($"/api/sales-orders/{order!.Id}/approve",
            new ApproveSalesOrderRequest(InstallmentCount: 1, FirstDueDate: DateOnly.FromDateTime(DateTime.UtcNow)));

        Assert.Equal(HttpStatusCode.Conflict, approveResponse.StatusCode);
    }
}
