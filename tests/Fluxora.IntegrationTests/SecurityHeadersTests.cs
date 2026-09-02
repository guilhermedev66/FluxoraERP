using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Fluxora.IntegrationTests;

public class SecurityHeadersTests(FluxoraApiFactory factory) : IClassFixture<FluxoraApiFactory>
{
    [Fact]
    public async Task AnyResponse_IncludesBaselineSecurityHeaders()
    {
        var response = await factory.CreateClient().GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
        Assert.Equal("no-referrer", response.Headers.GetValues("Referrer-Policy").Single());
        Assert.Contains("default-src 'none'", response.Headers.GetValues("Content-Security-Policy").Single());
    }
}

/// <summary>
/// The exception-handler path clears and rewrites the response, which previously wiped the
/// baseline security headers set earlier in the pipeline. This uses a dedicated factory (an
/// IStartupFilter that maps a deliberately-throwing endpoint) rather than touching production
/// Program.cs, so the real, unmodified middleware order is what gets exercised.
/// </summary>
public class SecurityHeadersOnExceptionTests(ThrowingEndpointApiFactory factory) : IClassFixture<ThrowingEndpointApiFactory>
{
    [Fact]
    public async Task UnhandledException_StillIncludesBaselineSecurityHeaders()
    {
        var response = await factory.CreateClient().GetAsync("/__test/throw");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
        Assert.Equal("no-referrer", response.Headers.GetValues("Referrer-Policy").Single());
        Assert.Contains("default-src 'none'", response.Headers.GetValues("Content-Security-Policy").Single());
    }
}

public class ThrowingEndpointApiFactory : FluxoraApiFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureServices(services =>
            services.AddSingleton<IStartupFilter>(new ThrowingEndpointStartupFilter()));
    }

    private sealed class ThrowingEndpointStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            next(app);
            app.Map("/__test/throw", branch => branch.Run(_ => throw new InvalidOperationException(
                "Deliberate failure for SecurityHeadersOnExceptionTests.")));
        };
    }
}
