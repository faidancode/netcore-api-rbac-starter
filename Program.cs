using netcore_api_rbac_starter;

var builder = AppBootstrap.CreateBuilder(args).AddAppServices();
var app = builder.Build();

if (AppBootstrap.IsSeedCommand(args))
{
    await SeedCommand.RunAsync(app);
    return;
}

app.UseAppPipeline();
app.Run();
