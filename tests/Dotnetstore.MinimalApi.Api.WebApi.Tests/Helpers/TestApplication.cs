using Asp.Versioning;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Dotnetstore.MinimalApi.Api.WebApi.Tests.Helpers;

internal static class TestApplication
{
    internal static WebApplication CreateVersionedApp(Action<IServiceCollection>? configureServices = null)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.ReportApiVersions = true;
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ApiVersionReader = new HeaderApiVersionReader(TestHttp.ApiVersionHeaderName);
        });
        configureServices?.Invoke(builder.Services);
        builder.WebHost.UseTestServer();

        return builder.Build();
    }
}

