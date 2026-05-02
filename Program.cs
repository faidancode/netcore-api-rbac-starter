using netcore_api_rbac_starter;
using netcore_api_rbac_starter.Common;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = AppBootstrap.CreateBuilder(args).AddAppServices();

    DotEnv.Load();

    builder.Configuration.AddEnvironmentVariables();

    builder.Host.UseSerilog((context, services, loggerConfiguration) =>
    {
        loggerConfiguration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .WriteTo.Console();

        if (!context.HostingEnvironment.IsDevelopment())
        {
            loggerConfiguration.WriteTo.File(
                path: "logs/log-.txt",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14,
                shared: true);
        }
    });

    var app = builder.Build();

    // Graceful Shutdown 
    app.Lifetime.ApplicationStopping.Register(() =>
    {
        Log.Information("Application is shutting down gracefully...");
    });

    if (AppBootstrap.IsMigrateCommand(args))
    {
        await MigrateCommand.RunAsync(app);
        return;
    }

    if (AppBootstrap.IsSeedCommand(args))
    {
        await SeedCommand.RunAsync(app);
        return;
    }

    await MigrateCommand.RunAsync(app);

    app.UseAppPipeline();
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
