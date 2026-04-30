using netcore_api_rbac_starter.Common.Middleware;
using Serilog;

namespace netcore_api_rbac_starter;

public static class AppPipeline
{
    public static WebApplication UseAppPipeline(this WebApplication app)
    {
        app.UseMiddleware<ExceptionMiddleware>();
        app.UseSerilogRequestLogging();
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

        app.UseMiddleware<RequestIdMiddleware>();

        app.UseCors("AngularApp");

        app.UseRateLimiter();

        app.UseAuthentication();

        app.UseAuthorization();

        app.UseMiddleware<IdempotencyMiddleware>();

        app.MapControllers();

        return app;
    }

}
