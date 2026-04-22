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