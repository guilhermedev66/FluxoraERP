using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Fluxora.Api.Controllers;
using Fluxora.Application.Customers;
using Fluxora.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Fluxora.IntegrationTests.Customers;

public class CustomerCsvTests(FluxoraApiFactory factory) : IClassFixture<FluxoraApiFactory>
{
    [Fact]
    public async Task Import_ValidCsv_ImportsEveryRowAndCreatesCorrelatedAudit()
    {
        var client = await CreateAuthenticatedClientAsync();
        var document1 = TestData.UniqueDocument();
        var document2 = TestData.UniqueDocument();
        var csv = $"name,document,email,phone\nCliente Um,{document1},um@example.com,11999990001\nCliente Dois,{document2},,\n";

        var response = await ImportAsync(client, csv);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<CustomerCsvImportResult>();
        Assert.Equal(2, result!.Total);
        Assert.Equal(2, result.Imported);
        Assert.Equal(0, result.Rejected);
        Assert.Empty(result.Errors);

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var importedCustomers = await dbContext.Customers.AsNoTracking()
            .Where(customer => customer.Document == document1 || customer.Document == document2)
            .ToListAsync();
        Assert.Equal(2, importedCustomers.Count);

        var importAudits = await dbContext.AuditEntries.AsNoTracking()
            .Where(entry => importedCustomers.Select(customer => customer.Id).Contains(entry.EntityId) &&
                entry.Action == "CustomerImported")
            .ToListAsync();
        Assert.Equal(2, importAudits.Count);
        var correlationId = Assert.Single(importAudits.Select(entry => entry.CorrelationId).Distinct());
        Assert.NotNull(correlationId);
        Assert.Equal(1, await dbContext.AuditEntries.CountAsync(entry =>
            entry.Action == "CustomerCsvImportCompleted" && entry.EntityId == correlationId));
    }

    [Fact]
    public async Task Import_PartiallyInvalidCsv_ImportsValidRowsAndReportsEveryRejectedLine()
    {
        var client = await CreateAuthenticatedClientAsync();
        var document = TestData.UniqueDocument();
        var csv = $"""
            name,document,email,phone
            Cliente Valido,{document},valid@example.com,11999990000
            ,DOC-MISSING-NAME,,
            Email Ruim,DOC-BAD-EMAIL,not-an-email,
            Duplicado,{document},duplicate@example.com,
            """;

        var response = await ImportAsync(client, csv);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<CustomerCsvImportResult>();
        Assert.Equal(4, result!.Total);
        Assert.Equal(1, result.Imported);
        Assert.Equal(3, result.Rejected);
        Assert.Equal([3L, 4L, 5L], result.Errors.Select(error => error.Line));
        Assert.All(result.Errors, error => Assert.False(string.IsNullOrWhiteSpace(error.Reason)));
    }

    [Fact]
    public async Task Import_AllDataRowsInvalid_ReturnsDetailedZeroImportResult()
    {
        var client = await CreateAuthenticatedClientAsync();
        const string csv = "name,document,email,phone\n,DOC-EMPTY,,\nBad Email,DOC-BAD,bad-email,\n";

        var response = await ImportAsync(client, csv);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<CustomerCsvImportResult>();
        Assert.Equal(2, result!.Total);
        Assert.Equal(0, result.Imported);
        Assert.Equal(2, result.Rejected);
        Assert.Equal(2, result.Errors.Count);
    }

    [Fact]
    public async Task Import_ConcurrentFilesWithSameDocument_PreservePartialResultsAndAudits()
    {
        var firstClient = await CreateAuthenticatedClientAsync();
        var secondClient = await CreateAuthenticatedClientAsync();
        var sharedDocument = TestData.UniqueDocument();
        var firstOnlyDocument = TestData.UniqueDocument();
        var secondOnlyDocument = TestData.UniqueDocument();
        var firstCsv = $"name,document,email,phone\nShared First,{sharedDocument},,\nFirst Only,{firstOnlyDocument},,\n";
        var secondCsv = $"name,document,email,phone\nShared Second,{sharedDocument},,\nSecond Only,{secondOnlyDocument},,\n";
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var firstTask = Task.Run(async () =>
        {
            await start.Task;
            return await ImportAsync(firstClient, firstCsv);
        });
        var secondTask = Task.Run(async () =>
        {
            await start.Task;
            return await ImportAsync(secondClient, secondCsv);
        });

        start.SetResult();
        var responses = await Task.WhenAll(firstTask, secondTask);

        Assert.All(responses, response => Assert.Equal(HttpStatusCode.OK, response.StatusCode));
        var results = await Task.WhenAll(responses.Select(response =>
            response.Content.ReadFromJsonAsync<CustomerCsvImportResult>()));
        Assert.Equal(3, results.Sum(result => result!.Imported));
        Assert.Equal(1, results.Sum(result => result!.Rejected));

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var documents = new[] { sharedDocument, firstOnlyDocument, secondOnlyDocument };
        var customers = await dbContext.Customers.AsNoTracking()
            .Where(customer => documents.Contains(customer.Document))
            .ToListAsync();
        Assert.Equal(3, customers.Count);
        var sharedCustomer = Assert.Single(customers, customer => customer.Document == sharedDocument);
        Assert.Equal(1, await dbContext.AuditEntries.CountAsync(entry =>
            entry.Action == "CustomerImported" && entry.EntityId == sharedCustomer.Id));
    }

    [Fact]
    public async Task Import_RacingCustomerCreate_ProducesOneCustomerWithoutServerError()
    {
        var importClient = await CreateAuthenticatedClientAsync();
        var createClient = await CreateAuthenticatedClientAsync();
        var document = TestData.UniqueDocument();
        var csv = $"name,document,email,phone\nImported Customer,{document},,\n";
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var importTask = Task.Run(async () =>
        {
            await start.Task;
            return await ImportAsync(importClient, csv);
        });
        var createTask = Task.Run(async () =>
        {
            await start.Task;
            return await createClient.PostAsJsonAsync("/api/customers", new CreateCustomerRequest(
                "Created Customer", document, null, null));
        });

        start.SetResult();
        await Task.WhenAll(importTask, createTask);
        var importResponse = await importTask;
        var createResponse = await createTask;
        var importResult = await importResponse.Content.ReadFromJsonAsync<CustomerCsvImportResult>();

        Assert.Equal(HttpStatusCode.OK, importResponse.StatusCode);
        Assert.True(
            (createResponse.StatusCode == HttpStatusCode.Created && importResult!.Imported == 0 && importResult.Rejected == 1) ||
            (createResponse.StatusCode == HttpStatusCode.Conflict && importResult!.Imported == 1 && importResult.Rejected == 0));

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(1, await dbContext.Customers.CountAsync(customer => customer.Document == document));
    }

    [Theory]
    [InlineData("full_name,document,email,phone\nTest,DOC-1,,")]
    [InlineData("")]
    public async Task Import_InvalidStructure_ReturnsBadRequest(string csv)
    {
        var client = await CreateAuthenticatedClientAsync();

        var response = await ImportAsync(client, csv);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Import_InvalidUtf8File_ReturnsBadRequest()
    {
        var client = await CreateAuthenticatedClientAsync();
        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent([0xFF, 0xFE, 0x00, 0x80]), "file", "customers.csv");

        var response = await client.PostAsync("/api/customers/import", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Export_ReturnsRealFilteredCustomersAsEscapedUtf8Csv()
    {
        var client = await CreateAuthenticatedClientAsync();
        var document = TestData.UniqueDocument();
        var createResponse = await client.PostAsJsonAsync("/api/customers", new CreateCustomerRequest(
            "ACME, \"Brasil\"", document, "finance@example.com", "+55 11 99999-0000"));
        createResponse.EnsureSuccessStatusCode();

        var response = await client.GetAsync($"/api/customers/export?search={document}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/csv", response.Content.Headers.ContentType!.MediaType);
        Assert.Contains("attachment", response.Content.Headers.ContentDisposition!.DispositionType);
        var csv = await response.Content.ReadAsStringAsync();
        Assert.Contains("name,document,email,phone,is_active,created_at_utc", csv);
        Assert.Contains("\"ACME, \"\"Brasil\"\"\"", csv);
        Assert.Contains(document, csv);
    }

    [Fact]
    public async Task Export_NeutralizesSpreadsheetFormulasIncludingLeadingControlCharacters()
    {
        var client = await CreateAuthenticatedClientAsync();
        var document = TestData.UniqueDocument();
        var createResponse = await client.PostAsJsonAsync("/api/customers", new CreateCustomerRequest(
            "=HYPERLINK(\"https://example.invalid\")", document, null, "\u0001+2+2"));
        createResponse.EnsureSuccessStatusCode();

        var response = await client.GetAsync($"/api/customers/export?search={document}");

        response.EnsureSuccessStatusCode();
        var csv = await response.Content.ReadAsStringAsync();
        Assert.Contains("'=HYPERLINK", csv);
        Assert.Contains("'\u0001+2+2", csv);
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

    private static Task<HttpResponseMessage> ImportAsync(HttpClient client, string csv)
    {
        var content = new MultipartFormDataContent();
        var file = new ByteArrayContent(Encoding.UTF8.GetBytes(csv));
        file.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        content.Add(file, "file", "customers.csv");
        return client.PostAsync("/api/customers/import", content);
    }
}
