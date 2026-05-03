using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using netcore_api_rbac_starter.Entities;
using netcore_api_rbac_starter.Data;
using netcore_api_rbac_starter.Security;
using netcore_api_rbac_starter.Tests.Helpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace netcore_api_rbac_starter.Tests.Integration;

/// <summary>
/// Custom WebApplicationFactory that swaps in a per-test in-memory database,
/// overrides JWT settings to a known secret, and seeds baseline data.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{

    private readonly string _dbName = Guid.NewGuid().ToString();

    public const string TestJwtSecret = "integration-test-secret-key-that-is-long-enough-32!";
    public const string TestJwtIssuer = "TestIssuer";
    public const string TestJwtAudience = "TestAudience";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureLogging(logging => logging.ClearProviders());
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = TestJwtSecret,
                ["Jwt:Issuer"] = TestJwtIssuer,
                ["Jwt:Audience"] = TestJwtAudience,
                ["Jwt:AccessTokenExpiryMinutes"] = "60",
                ["Jwt:RefreshTokenExpiryDays"] = "7"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Now AppServiceConfiguration skips Npgsql, but we still remove just in case
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<AppDbContext>();

            services.AddDbContext<AppDbContext>(opts =>
                opts.UseInMemoryDatabase(_dbName)
                    .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning)));
        });

        builder.UseEnvironment("Testing");
    }

    /// <summary>Seed baseline data into the test database.</summary>
    public async Task InitializeAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await EntityBuilder.SeedDefaultDataAsync(db);
    }

    public new Task DisposeAsync() => Task.CompletedTask;

    // ── Token helpers ─────────────────────────────────────────────────────────

    /// <summary>
    /// Generates a real JWT using the test secret for the given user.
    /// Permissions can be passed directly for fine-grained testing.
    /// </summary>
    public string GenerateToken(Guid userId, string email, IEnumerable<string>? permissions = null)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = TestJwtSecret,
                ["Jwt:Issuer"] = TestJwtIssuer,
                ["Jwt:Audience"] = TestJwtAudience,
                ["Jwt:AccessTokenExpiryMinutes"] = "60"
            })
            .Build();

        var jwtService = new JwtService(config);

        var user = new User { Id = userId, Name = "Test", Email = email };
        var perms = (permissions ?? ["manage:all"])
            .Select(p =>
            {
                var parts = p.Split(':');
                return new Permission { Action = parts[0], Subject = parts[1] };
            });

        return jwtService.GenerateAccessToken(user, perms);
    }

    /// <summary>Returns an HttpClient with Authorization header set for admin user.</summary>
    public HttpClient CreateAdminClient()
    {
        var token = GenerateToken(EntityBuilder.AdminUserId, "admin@example.com", ["manage:all"]);
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public HttpClient CreateManagerClient()
    {
        var token = GenerateToken(EntityBuilder.ManagerUserId, "manager@example.com", ["manage:all"]);
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    /// <summary>Returns an HttpClient with Authorization header for a viewer (read-only).</summary>
    public HttpClient CreateViewerClient()
    {
        var token = GenerateToken(EntityBuilder.RegularUserId, "user@example.com",
            ["read:User", "read:Employee", "read:Department", "read:Position", "read:Role"]);
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    /// <summary>Returns an unauthenticated HttpClient.</summary>
    public HttpClient CreateAnonClient() => CreateClient();
}
