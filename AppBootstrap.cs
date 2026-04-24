using netcore_api_rbac_starter.Common;

namespace netcore_api_rbac_starter;

public static class AppBootstrap
{
    public static WebApplicationBuilder CreateBuilder(string[] args)
    {
        DotEnv.Load(Path.Combine(Directory.GetCurrentDirectory(), ".env"));
        return WebApplication.CreateBuilder(args);
    }

    public static bool IsSeedCommand(string[] args)
        => args.Any(a => string.Equals(a, "--seed", StringComparison.OrdinalIgnoreCase));

    public static bool IsMigrateCommand(string[] args)
        => args.Any(a => string.Equals(a, "--migrate", StringComparison.OrdinalIgnoreCase));
}
