using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace FlowBoard.IntegrationTests;

/// <summary>
/// HTTP-level assertion that authenticated write endpoints return 429 after the per-user limit.
/// </summary>
[Collection(nameof(SqlServerCollection))]
public sealed class WriteRateLimitApiTests(SqlServerFixture fixture) : IAsyncLifetime
{
    private WebApplicationFactory<Program>? _factory;
    private HttpClient? _client;
    private string? _accessToken;

    public async Task InitializeAsync()
    {
        if (!fixture.IsDockerAvailable)
            return;

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.UseSetting("ConnectionStrings:DefaultConnection", fixture.ConnectionString);
                builder.UseSetting("Jwt:SecretKey", "integration-tests-secret-key-0123456789");
                builder.UseSetting("Jwt:Issuer", "FlowBoard.API");
                builder.UseSetting("Jwt:Audience", "FlowBoard.Client");
                builder.UseSetting("Hangfire:RegisterRecurringJobs", "false");
            });

        _client = _factory.CreateClient();

        var email = $"rate{Guid.NewGuid():N}@test.com";
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            fullName = "Rate Tester",
            password = "Password123!",
            confirmPassword = "Password123!"
        });
        registerResponse.EnsureSuccessStatusCode();

        var auth = await registerResponse.Content.ReadFromJsonAsync<AuthTokens>();
        _accessToken = auth!.AccessToken;
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
    }

    [SkippableFact]
    public async Task AuthenticatedWrite_Endpoint61stRequest_Returns429WithProblemDetails()
    {
        Skip.IfNot(fixture.IsDockerAvailable, "Docker is not running — start Docker Desktop to run integration tests.");
        Skip.If(_client is null || _accessToken is null, "API test host failed to start.");

        HttpResponseMessage? lastResponse = null;

        for (var i = 0; i < 61; i++)
        {
            lastResponse = await _client!.PostAsJsonAsync("/api/workspaces", new
            {
                name = $"WS-{i}-{Guid.NewGuid():N}"
            });
        }

        Assert.NotNull(lastResponse);
        Assert.Equal(HttpStatusCode.TooManyRequests, lastResponse.StatusCode);
        Assert.Equal("application/problem+json", lastResponse.Content.Headers.ContentType?.MediaType);

        var body = await lastResponse.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        Assert.Equal(429, doc.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("Too many requests", doc.RootElement.GetProperty("title").GetString());
    }

    public async Task DisposeAsync()
    {
        if (_factory is not null)
            await _factory.DisposeAsync();
    }

    private sealed record AuthTokens(string AccessToken, string RefreshToken);
}
