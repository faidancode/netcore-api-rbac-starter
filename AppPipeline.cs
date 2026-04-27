using netcore_api_rbac_starter.Common.Middleware;

namespace netcore_api_rbac_starter;

public static class AppPipeline
{
    public static WebApplication UseAppPipeline(this WebApplication app)
    {
        app.UseMiddleware<ExceptionMiddleware>();
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/openapi/v1.json", "API v1");
                options.RoutePrefix = "swagger";
            });
        }

        if (app.Configuration.GetValue<int?>("ASPNETCORE_HTTPS_PORT").HasValue)
        {
            app.UseHttpsRedirection();
        }

        app.UseCors("AngularApp");

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapGet("/weatherforecast", () =>
        {
            var summaries = new[]
            {
                "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
            };

            var forecast = Enumerable.Range(1, 5).Select(index =>
                new WeatherForecast
                (
                    DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                    Random.Shared.Next(-20, 55),
                    summaries[Random.Shared.Next(summaries.Length)]
                ))
                .ToArray();
            return forecast;
        })
        .WithName("GetWeatherForecast");

        app.MapControllers();

        return app;
    }

    private record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
    {
        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
    }
}
