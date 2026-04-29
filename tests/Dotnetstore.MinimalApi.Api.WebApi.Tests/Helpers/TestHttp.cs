using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Dotnetstore.MinimalApi.Api.WebApi.Configuration;

namespace Dotnetstore.MinimalApi.Api.WebApi.Tests.Helpers;

internal static class TestHttp
{
    internal static string ApiVersionHeaderName => WebApiConfiguration.ApiVersionHeaderName;
    internal const string ApiKeyHeaderName = "X-API-KEY";
    internal const string ApiKeyValue = "5CC5F891B1A44E45BCFAB72B598515CA";
    internal const string HttpLocalhost = "http://localhost";
    internal const string HttpsLocalhost = "https://localhost";
    internal const string OpenApiDocumentPath = "/openapi/v1.json";

    internal static HttpClient CreateClient(WebApplication app, string baseAddress)
    {
        var client = app.GetTestClient();
        client.BaseAddress = new Uri(baseAddress);

        return client;
    }

    internal static HttpClient CreateClient<TEntryPoint>(WebApplicationFactory<TEntryPoint> factory, string baseAddress)
        where TEntryPoint : class =>
        factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri(baseAddress),
            AllowAutoRedirect = false
        });

    internal static HttpRequestMessage CreateOriginRequest(HttpMethod method, string requestUri, string origin)
    {
        var request = new HttpRequestMessage(method, requestUri);
        request.Headers.Add("Origin", origin);

        return request;
    }

    internal static HttpRequestMessage CreateVersionedRequest(HttpMethod method, string requestUri, string apiVersion)
    {
        var request = new HttpRequestMessage(method, requestUri);
        request.Headers.Add(ApiVersionHeaderName, apiVersion);

        return request;
    }

    internal static HttpRequestMessage CreateAuthorizedVersionedRequest(HttpMethod method, string requestUri, string apiVersion)
    {
        var request = CreateVersionedRequest(method, requestUri, apiVersion);
        AddApiKeyHeader(request);

        return request;
    }

    internal static void AddApiKeyHeader(HttpRequestMessage request)
    {
        request.Headers.Add(ApiKeyHeaderName, ApiKeyValue);
    }

    internal static HttpRequestMessage CreateCorsPreflightRequest(
        string requestUri,
        string origin,
        string requestedMethod,
        string requestedHeaders)
    {
        var request = CreateOriginRequest(HttpMethod.Options, requestUri, origin);
        request.Headers.Add("Access-Control-Request-Method", requestedMethod);
        request.Headers.Add("Access-Control-Request-Headers", requestedHeaders);

        return request;
    }
}


