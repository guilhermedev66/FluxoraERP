using System.Net;
using System.Text;
using System.Threading.RateLimiting;
using Fluxora.Api.Auth;
using Fluxora.Application.Common;
using Fluxora.Infrastructure;
using Fluxora.Infrastructure.Identity;
using Fluxora.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(kestrelOptions => kestrelOptions.AddServerHeader = false);

builder.Services.AddInfrastructure(builder.Configuration);

IPAddress? knownProxyAddress = null;
var configuredKnownProxy = builder.Configuration["ReverseProxy:KnownProxy"];
if (!string.IsNullOrWhiteSpace(configuredKnownProxy))
{
    if (!IPAddress.TryParse(configuredKnownProxy, out knownProxyAddress))
    {
        throw new InvalidOperationException("ReverseProxy:KnownProxy must be a valid IP address.");
    }

    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.ForwardLimit = 1;
        options.KnownProxies.Add(knownProxyAddress);
    });
}

builder.Services.AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
    .Validate(options => { options.Validate(); return true; })
    .ValidateOnStart();

var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["Key"] ?? string.Empty)),
            ClockSkew = TimeSpan.FromSeconds(30),
        };
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy(AppPolicies.SalesAccess, policy =>
        policy.RequireRole(AppRoles.Admin, AppRoles.Manager, AppRoles.Sales))
    .AddPolicy(AppPolicies.PurchasingAccess, policy =>
        policy.RequireRole(AppRoles.Admin, AppRoles.Manager))
    .AddPolicy(AppPolicies.FinanceAccess, policy =>
        policy.RequireRole(AppRoles.Admin, AppRoles.Manager, AppRoles.Finance))
    .AddPolicy(AppPolicies.ReportingAccess, policy =>
        policy.RequireRole(AppRoles.Admin, AppRoles.Manager, AppRoles.Finance))
    .AddPolicy(AppPolicies.DataExchangeManage, policy =>
        policy.RequireRole(AppRoles.Admin, AppRoles.Manager))
    .AddPolicy(AppPolicies.AutomationManage, policy =>
        policy.RequireRole(AppRoles.Admin, AppRoles.Manager));

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();
builder.Services.AddScoped<JwtTokenService>();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
if (allowedOrigins.Length > 0)
{
    builder.Services.AddCors(options => options.AddPolicy("Frontend", policy =>
        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));
}

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ILoginAttemptGuard, LoginAttemptGuard>();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("login", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 20,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            AutoReplenishment = true,
        }));
});
builder.Services.AddHealthChecks()
    .AddCheck<Fluxora.Api.Health.DatabaseHealthCheck>("database", tags: ["ready"]);
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Info.Title = "Fluxora API";
        document.Info.Version = "v1";
        document.Info.Description = "Mini ERP API for commercial, purchasing, finance, reporting, and automation workflows.";
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes[JwtBearerDefaults.AuthenticationScheme] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "JWT access token returned by POST /api/auth/login.",
        };
        return Task.CompletedTask;
    });
    options.AddOperationTransformer((operation, context, _) =>
    {
        var requiresAuthorization = context.Description.ActionDescriptor.EndpointMetadata
            .OfType<Microsoft.AspNetCore.Authorization.IAuthorizeData>()
            .Any();
        if (requiresAuthorization)
        {
            operation.Security ??= [];
            operation.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(JwtBearerDefaults.AuthenticationScheme, context.Document)] = [],
            });
        }

        return Task.CompletedTask;
    });
});

var app = builder.Build();

app.UseExceptionHandler();

app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    context.Response.Headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";
    await next();
});

if (knownProxyAddress is not null)
{
    app.UseForwardedHeaders();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseRouting();

if (allowedOrigins.Length > 0)
{
    app.UseCors("Frontend");
}

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
});
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready"),
});
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready"),
});
app.MapControllers();

if (builder.Configuration.GetValue("Database:ApplyMigrations", defaultValue: false))
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.MigrateAsync();
    await IdentitySeeder.SeedAsync(scope.ServiceProvider);
}

app.Run();

public partial class Program;
