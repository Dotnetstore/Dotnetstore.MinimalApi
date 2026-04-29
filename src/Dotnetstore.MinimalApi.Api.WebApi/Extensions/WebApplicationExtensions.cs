using System.Threading.RateLimiting;
using Asp.Versioning;
using Dotnetstore.MinimalApi.Api.WebApi.Configuration;
using Dotnetstore.MinimalApi.Api.WebApi.Endpoints;
using Dotnetstore.MinimalApi.Api.WebApi.Exceptions;
using Dotnetstore.MinimalApi.Api.WebApi.Filters;
using Dotnetstore.MinimalApi.Api.WebApi.Handlers;
using Microsoft.Extensions.Options;

namespace Dotnetstore.MinimalApi.Api.WebApi.Extensions;

internal static class WebApplicationExtensions
{
    extension(WebApplicationBuilder builder)
    {
        internal WebApplicationBuilder RegisterWebApi()
        {
            builder
                .AddServiceDefaults();
            
            builder.Services
                .AddSingleton<IValidateOptions<WebApiOptions>, WebApiOptionsValidator>()
                .AddOptions<WebApiOptions>()
                .Bind(builder.Configuration.GetSection(WebApiConfiguration.OptionsSectionName))
                .PostConfigure(options => options.ApplyDefaults())
                .ValidateOnStart();

            var webApiOptions = builder.ResolveWebApiOptions();
            
            builder.Services.Configure<ApiKeyOptions>(builder.Configuration.GetSection(ApiKeyOptions.SectionName));

            builder.SetupHsts(webApiOptions);
            builder.SetupCors(webApiOptions);
            builder.SetupVersioning();
            builder.SetupRateLimiter(webApiOptions);

            builder.Services
                .AddOpenApi()
                .AddProblemDetails()
                .AddSingleton<IWebApplicationHandlers, WebApplicationHandlers>()
                .AddSingleton<ITestEndpoints, TestEndpoints>()
                .AddExceptionHandler<DefaultExceptionHandler>()
                .AddScoped<ApiKeyFilter>();
        
            return builder;
        }

        private void SetupHsts(WebApiOptions webApiOptions)
        {
            builder.Services
                .AddHttpsRedirection(options =>
                {
                    options.RedirectStatusCode = webApiOptions.HttpsRedirection.RedirectStatusCode;
                    options.HttpsPort = webApiOptions.HttpsRedirection.HttpsPort;
                });
        
            builder.Services.AddHsts(options =>
            {
                options.Preload = webApiOptions.Hsts.Preload;
                options.IncludeSubDomains = webApiOptions.Hsts.IncludeSubDomains;
                options.MaxAge = TimeSpan.FromDays(webApiOptions.Hsts.MaxAgeDays);
            });
        }

        private void SetupCors(WebApiOptions webApiOptions)
        {
            builder.Services
                .AddCors(options =>
                {
                    options.AddPolicy(WebApiConfiguration.CorsPolicyName,
                        policy =>
                        {
                            policy
                                .WithOrigins(webApiOptions.Cors.AllowedOrigins!)
                                .WithMethods(webApiOptions.Cors.AllowedMethods!)
                                .AllowAnyHeader();
                        });
                });
        }
        
        private void SetupVersioning()
        {
            builder.Services
                .AddApiVersioning(options =>
                {
                    options.DefaultApiVersion = new ApiVersion(1, 0);
                    options.ReportApiVersions = true;
                    options.AssumeDefaultVersionWhenUnspecified = true;
                    options.ApiVersionReader = new HeaderApiVersionReader(WebApiConfiguration.ApiVersionHeaderName);
                });
        }

        private void SetupRateLimiter(WebApiOptions webApiOptions)
        {            
            var rateLimitingOptions = webApiOptions.RateLimiting;

            builder.Services
                .AddRateLimiter(options =>
                {
                    options.RejectionStatusCode = rateLimitingOptions.RejectionStatusCode;
                    options.OnRejected = async (context, token) =>
                    {
                        await context.HttpContext.Response.WriteAsync(rateLimitingOptions.RejectionMessage, token);
                    };
                    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                        RateLimitPartition.GetFixedWindowLimiter(
                            partitionKey: GetRateLimitPartitionKey(httpContext, rateLimitingOptions.PartitionKeyFallback),
                            factory: _ => new FixedWindowRateLimiterOptions
                            {
                                QueueLimit = rateLimitingOptions.GlobalQueueLimit,
                                PermitLimit = rateLimitingOptions.GlobalPermitLimit,
                                Window = TimeSpan.FromSeconds(rateLimitingOptions.GlobalWindowSeconds)
                            }));
                    options.AddPolicy(WebApiConfiguration.ShortRateLimitPolicyName, context =>
                        RateLimitPartition.GetFixedWindowLimiter(
                            partitionKey: GetRateLimitPartitionKey(context, rateLimitingOptions.PartitionKeyFallback),
                            factory: _ => new FixedWindowRateLimiterOptions
                            {
                                QueueLimit = rateLimitingOptions.ShortQueueLimit,
                                PermitLimit = rateLimitingOptions.ShortPermitLimit,
                                Window = TimeSpan.FromSeconds(rateLimitingOptions.ShortWindowSeconds)
                            }));
                });
        }

        private WebApiOptions ResolveWebApiOptions() =>
            (builder.Configuration
                .GetSection(WebApiConfiguration.OptionsSectionName)
                .Get<WebApiOptions>()
            ?? new WebApiOptions())
            .ApplyDefaults();

        private static string GetRateLimitPartitionKey(HttpContext httpContext, string partitionKeyFallback) =>
            httpContext.Connection.RemoteIpAddress?.ToString() ?? partitionKeyFallback;
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
                .UseCors(WebApiConfiguration.CorsPolicyName)
                .UseRateLimiter()
                .UseExceptionHandler();
        
            return app;
        }

        internal async ValueTask RunWebApiAsync(CancellationToken cancellationToken = default)
        {
            await app.RunAsync(cancellationToken);
        }
    }
}