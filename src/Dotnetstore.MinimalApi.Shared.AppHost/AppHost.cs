var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.Dotnetstore_MinimalApi_Api_WebApi>("webApi")
	.WithExternalHttpEndpoints();

builder.Build().Run();