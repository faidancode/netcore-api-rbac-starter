using netcore_api_rbac_starter.Data;
using netcore_api_rbac_starter.Data.Seed;
using Microsoft.EntityFrameworkCore;

namespace netcore_api_rbac_starter;

public static class SeedCommand
{
    public static async Task RunAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var pendingMigrations = await db.Database.GetPendingMigrationsAsync();
        if (pendingMigrations.Any())
        {
            throw new InvalidOperationException(
                "Database has pending migrations. Run 'dotnet ef database update' before seeding.");
        }

        await DatabaseSeeder.SeedAsync(db);
    }
}
