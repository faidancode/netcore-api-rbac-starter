using netcore_api_rbac_starter.Data;
using Microsoft.EntityFrameworkCore;

namespace netcore_api_rbac_starter;

public static class MigrateCommand
{
    public static async Task RunAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.Database.MigrateAsync();
    }
}
