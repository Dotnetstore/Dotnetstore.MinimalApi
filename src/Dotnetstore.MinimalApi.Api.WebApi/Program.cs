using Dotnetstore.MinimalApi.Api.WebApi.Endpoints;
using Dotnetstore.MinimalApi.Api.WebApi.Extensions;

var builder = WebApplication.CreateBuilder(args);
var cancellationToken = new CancellationTokenSource().Token;

builder
    .RegisterWebApi();

var app = builder.Build();
app.RegisterMiddlewares();

using (var scope = app.Services.CreateScope())
{
    var testEndpoints = scope.ServiceProvider.GetRequiredService<ITestEndpoints>();
    testEndpoints.MapEndpoints(app);
}

await app
    .RunWebApiAsync(cancellationToken);

public partial class Program;
