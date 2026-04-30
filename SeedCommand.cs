using netcore_api_rbac_starter.Data;
using netcore_api_rbac_starter.Data.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace netcore_api_rbac_starter;

public static class SeedCommand
{
    public static async Task RunAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger("SeedCommand");

        var pendingMigrations = await db.Database.GetPendingMigrationsAsync();
        if (pendingMigrations.Any())
        {
            try
            {
                await db.Database.MigrateAsync();
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("PendingModelChangesWarning", StringComparison.Ordinal))
            {
                logger.LogWarning(ex, "Skipping migrate during seed because EF reported pending model changes.");
            }
        }

        await DatabaseSeeder.SeedAsync(db);
    }
}
