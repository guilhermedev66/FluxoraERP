using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Fluxora.Api.Controllers;
using Fluxora.Application.Customers;

namespace Fluxora.IntegrationTests.Customers;

public class CustomersApiTests(FluxoraApiFactory factory) : IClassFixture<FluxoraApiFactory>
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
    public async Task List_WithoutAuthentication_ReturnsUnauthorized()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/customers");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_ThenGetById_ReturnsTheSameCustomer()
    {
        var client = await CreateAuthenticatedClientAsync();
        var document = TestData.UniqueDocument();

        var createResponse = await client.PostAsJsonAsync("/api/customers", new CreateCustomerRequest(
            "Acme Consultoria Ltda", document, "financeiro@acme.example", null));
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<CustomerDto>();
        Assert.NotNull(created);
        Assert.Equal("Acme Consultoria Ltda", created!.Name);

        var getResponse = await client.GetAsync($"/api/customers/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var fetched = await getResponse.Content.ReadFromJsonAsync<CustomerDto>();
        Assert.Equal(created.Id, fetched!.Id);
        Assert.True(fetched.IsActive);
    }

    [Fact]
    public async Task Create_WithDuplicateDocument_ReturnsConflict()
    {
        var client = await CreateAuthenticatedClientAsync();
        var document = TestData.UniqueDocument();

        var first = await client.PostAsJsonAsync("/api/customers", new CreateCustomerRequest(
            "Primeiro Cliente", document, null, null));
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await client.PostAsJsonAsync("/api/customers", new CreateCustomerRequest(
            "Segundo Cliente", document, null, null));
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Deactivate_SetsCustomerInactive()
    {
        var client = await CreateAuthenticatedClientAsync();
        var document = TestData.UniqueDocument();

        var createResponse = await client.PostAsJsonAsync("/api/customers", new CreateCustomerRequest(
            "Cliente Para Desativar", document, null, null));
        var created = await createResponse.Content.ReadFromJsonAsync<CustomerDto>();

        var deactivateResponse = await client.PostAsync($"/api/customers/{created!.Id}/deactivate", null);
        Assert.Equal(HttpStatusCode.NoContent, deactivateResponse.StatusCode);

        var getResponse = await client.GetAsync($"/api/customers/{created.Id}");
        var fetched = await getResponse.Content.ReadFromJsonAsync<CustomerDto>();
        Assert.False(fetched!.IsActive);
    }
}
