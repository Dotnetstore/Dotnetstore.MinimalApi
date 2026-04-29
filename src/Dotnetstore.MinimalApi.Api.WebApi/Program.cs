using Dotnetstore.MinimalApi.Api.WebApi.Endpoints;
using Dotnetstore.MinimalApi.Api.WebApi.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder
    .RegisterWebApi();

var app = builder.Build();
app
    .RegisterMiddlewares();

var testEndpoints = app.Services.GetRequiredService<ITestEndpoints>();
testEndpoints.MapEndpoints(app);

await app
    .RunWebApiAsync();

public partial class Program;
