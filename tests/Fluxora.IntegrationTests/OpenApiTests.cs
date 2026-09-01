using System.Net;
using System.Text.Json;

namespace Fluxora.IntegrationTests;

public class OpenApiTests(FluxoraApiFactory factory) : IClassFixture<FluxoraApiFactory>
{
    [Fact]
    public async Task DevelopmentDocument_DescribesApiAndBearerAuthentication()
    {
        var response = await factory.CreateClient().GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;
        Assert.Equal("Fluxora API", root.GetProperty("info").GetProperty("title").GetString());
        Assert.Equal("v1", root.GetProperty("info").GetProperty("version").GetString());

        var bearer = root.GetProperty("components").GetProperty("securitySchemes").GetProperty("Bearer");
        Assert.Equal("http", bearer.GetProperty("type").GetString());
        Assert.Equal("bearer", bearer.GetProperty("scheme").GetString());

        var salesList = root.GetProperty("paths").GetProperty("/api/sales-orders").GetProperty("get");
        Assert.Contains(salesList.GetProperty("security").EnumerateArray(), requirement =>
            requirement.TryGetProperty("Bearer", out _));
    }
}
