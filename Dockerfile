FROM mcr.microsoft.com/dotnet/sdk:10.0.203 AS build
WORKDIR /src

COPY ["Directory.Build.props", "./"]
COPY ["src/Dotnetstore.MinimalApi.Api.WebApi/Dotnetstore.MinimalApi.Api.WebApi.csproj", "src/Dotnetstore.MinimalApi.Api.WebApi/"]
COPY ["src/Dotnetstore.MinimalApi.Shared.ServiceDefaults/Dotnetstore.MinimalApi.Shared.ServiceDefaults.csproj", "src/Dotnetstore.MinimalApi.Shared.ServiceDefaults/"]
RUN dotnet restore "src/Dotnetstore.MinimalApi.Api.WebApi/Dotnetstore.MinimalApi.Api.WebApi.csproj"

COPY . .
WORKDIR "/src/src/Dotnetstore.MinimalApi.Api.WebApi"
RUN dotnet publish "Dotnetstore.MinimalApi.Api.WebApi.csproj" --configuration Release --output /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Dotnetstore.MinimalApi.Api.WebApi.dll"]

