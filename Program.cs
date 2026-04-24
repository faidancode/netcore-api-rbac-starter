using netcore_api_rbac_starter;

var builder = AppBootstrap.CreateBuilder(args).AddAppServices();
var app = builder.Build();

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

app.UseAppPipeline();
app.Run();
