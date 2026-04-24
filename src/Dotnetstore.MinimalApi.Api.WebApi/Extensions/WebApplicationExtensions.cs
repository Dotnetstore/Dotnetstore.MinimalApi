using Asp.Versioning;
using Dotnetstore.MinimalApi.Api.WebApi.Endpoints;
using Dotnetstore.MinimalApi.Api.WebApi.Handlers;

namespace Dotnetstore.MinimalApi.Api.WebApi.Extensions;

internal static class WebApplicationExtensions
{
    private const string AllowDotnetstoreSpecificOrigins = "AllowDotnetstoreSpecificOrigins";
    
    extension(WebApplicationBuilder builder)
    {
        internal WebApplicationBuilder RegisterWebApi()
        {
            builder
                .SetupHsts()
                .SetupCors()
                .SetupVersioning();

            builder.Services
                .AddOpenApi()
                .AddScoped<IWebApplicationHandlers, WebApplicationHandlers>()
                .AddScoped<ITestEndpoints, TestEndpoints>();
        
        
            return builder;
        }

        private WebApplicationBuilder SetupHsts()
        {
            if(builder.Environment.IsDevelopment())
            {
                builder.Services
                    .AddHttpsRedirection(options =>
                    {
                        options.RedirectStatusCode = StatusCodes.Status307TemporaryRedirect;
                        options.HttpsPort = 7201;
                    });
            }

            if (!builder.Environment.IsDevelopment())
            {
                builder.Services
                    .AddHttpsRedirection(options =>
                    {
                        options.RedirectStatusCode = StatusCodes.Status308PermanentRedirect;
                        options.HttpsPort = 443;
                    });
            }
        
            builder.Services.AddHsts(options =>
            {
                options.Preload = true;
                options.IncludeSubDomains = true;
                options.MaxAge = TimeSpan.FromDays(30);
            });
        
            return builder;
        }

        private WebApplicationBuilder SetupCors()
        {
            builder.Services
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
        
            return builder;
        }
        
        private WebApplicationBuilder SetupVersioning()
        {
            builder.Services
                .AddApiVersioning(options =>
                {
                    options.DefaultApiVersion = new ApiVersion(1, 0);
                    options.ReportApiVersions = true;
                    options.AssumeDefaultVersionWhenUnspecified = true;
                    options.ApiVersionReader = new HeaderApiVersionReader("api-version");
                });
        
            return builder;
        }
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

            if (!app.Environment.IsDevelopment())
            {
                app.UseHsts();
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