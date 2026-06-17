using System.Text.Json.Serialization;
using JiraIntegrationService.Api.Application.Admin;
using JiraIntegrationService.Api.Application.Configuration;
using JiraIntegrationService.Api.Application.Issues;
using JiraIntegrationService.Api.Application.Issues.Mapping;
using JiraIntegrationService.Api.Application.Jira;
using JiraIntegrationService.Api.Common;
using JiraIntegrationService.Api.Infrastructure.Jira;
using JiraIntegrationService.Api.Infrastructure.Logging;
using JiraIntegrationService.Api.Infrastructure.Persistence;
using JiraIntegrationService.Api.Infrastructure.Security;
using JiraIntegrationService.Api.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext();
});

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var response = new ApiErrorResponse(
            ErrorCodes.ValidationError,
            "Invalid request.",
            TraceId.From(context.HttpContext));

        return new BadRequestObjectResult(response);
    };
});

builder.Services.Configure<InternalAuthOptions>(
    builder.Configuration.GetSection(InternalAuthOptions.SectionName));
builder.Services.Configure<JiraOptions>(
    builder.Configuration.GetSection(JiraOptions.SectionName));
builder.Services.Configure<RetryOptions>(
    builder.Configuration.GetSection(RetryOptions.SectionName));

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Connection string 'Default' is not configured.");

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlite(connectionString);
});

builder.Services.AddScoped<IProductConfigService, ProductConfigService>();
builder.Services.AddScoped<IAdminConfigurationService, AdminConfigurationService>();
builder.Services.AddScoped<ISourcePathResolver, SourcePathResolver>();
builder.Services.AddScoped<IJiraFieldValueBuilder, JiraFieldValueBuilder>();
builder.Services.AddScoped<IJiraIssuePayloadBuilder, JiraIssuePayloadBuilder>();
builder.Services.AddScoped<IIssueService, IssueService>();
builder.Services.AddScoped<IJiraClientResolver, JiraClientResolver>();
builder.Services.AddHttpClient<IJiraClient, JiraClient>();

builder.Services.AddOpenApi();

var app = builder.Build();

await app.ApplyDatabaseMigrationsAsync();

app.UseMiddleware<RequestLogContextMiddleware>();
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("TraceId", TraceId.From(httpContext));
        diagnosticContext.Set("RequestMethod", httpContext.Request.Method);
        diagnosticContext.Set("RequestPath", httpContext.Request.Path.Value);
    };
});

var frontendFileProvider = CreateFrontendFileProvider(app.Configuration, app.Environment.ContentRootPath);
if (frontendFileProvider is not null)
{
    app.UseDefaultFiles(new DefaultFilesOptions
    {
        FileProvider = frontendFileProvider
    });
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = frontendFileProvider
    });
}

app.UseMiddleware<InternalAuthMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthorization();

app.MapControllers();

if (frontendFileProvider is not null)
{
    app.MapFallback(async context =>
    {
        if (context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        var indexFile = frontendFileProvider.GetFileInfo("index.html");
        if (!indexFile.Exists)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        context.Response.ContentType = "text/html; charset=utf-8";
        await context.Response.SendFileAsync(indexFile);
    });
}

app.Run();

static IFileProvider? CreateFrontendFileProvider(IConfiguration configuration, string contentRootPath)
{
    var frontendDistPath = ResolveFrontendDistPath(configuration, contentRootPath);
    if (!Directory.Exists(frontendDistPath))
    {
        return null;
    }

    return new PhysicalFileProvider(frontendDistPath);
}

static string ResolveFrontendDistPath(IConfiguration configuration, string contentRootPath)
{
    var configuredPath = configuration["Frontend:DistPath"];
    if (!string.IsNullOrWhiteSpace(configuredPath))
    {
        return Path.GetFullPath(
            Path.IsPathRooted(configuredPath)
                ? configuredPath
                : Path.Combine(contentRootPath, configuredPath));
    }

    var publishedWwwRoot = Path.GetFullPath(Path.Combine(contentRootPath, "wwwroot"));
    if (File.Exists(Path.Combine(publishedWwwRoot, "index.html")))
    {
        return publishedWwwRoot;
    }

    return Path.GetFullPath(Path.Combine(contentRootPath, "..", "JiraIntegrationService.Web", "dist"));
}

public partial class Program;
