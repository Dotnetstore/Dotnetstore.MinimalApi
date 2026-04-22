using Dotnetstore.MinimalApi.Api.WebApi.Extensions;

var builder = WebApplication.CreateBuilder(args);
var cancellationToken = new CancellationTokenSource().Token;

builder
    .RegisterWebApi();

var app = builder.Build();

await app
    .RegisterMiddlewares()
    .RunWebApiAsync(cancellationToken);

public partial class Program;
