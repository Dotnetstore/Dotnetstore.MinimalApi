using Asp.Versioning;
using Dotnetstore.MinimalApi.Api.WebApi.Handlers;

namespace Dotnetstore.MinimalApi.Api.WebApi.Extensions;

internal static class WebApplicationExtensions
{
    private const string AllowDotnetstoreSpecificOrigins = "AllowDotnetstoreSpecificOrigins";
    
    internal static WebApplicationBuilder RegisterWebApi(this WebApplicationBuilder builder)
    {
        builder.Services
            .AddOpenApi()
            .AddCors(options =>
            {
                options.AddPolicy(name: AllowDotnetstoreSpecificOrigins,
                    policy =>
                    {
                        policy
                            .WithOrigins("http://localhost:7000")
                            .WithMethods("GET", "POST", "PUT")
                            .AllowAnyHeader();
                    });
            });

        builder.Services
            .AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.ReportApiVersions = true;
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ApiVersionReader = new HeaderApiVersionReader("api-version");
            });

        builder.Services
            .AddScoped<IWebApplicationHandlers, WebApplicationHandlers>();
        
        return builder;
    }
    
    extension(WebApplication app)
    {
        internal WebApplication RegisterMiddlewares()
        {
            if (app.Environment.IsDevelopment())
            {
                app
                    .MapOpenApi();
            }

            app
                .UseHttpsRedirection()
                .UseCors(AllowDotnetstoreSpecificOrigins);
        
            return app;
        }

        internal async ValueTask RunWebApiAsync(CancellationToken cancellationToken = default)
        {
            await app.RunAsync(cancellationToken);
        }
    }
}