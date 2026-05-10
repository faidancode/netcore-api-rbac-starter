using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using netcore_api_rbac_starter.Common.Middleware;
using Serilog;

namespace netcore_api_rbac_starter;

public static class AppPipeline
{
    public static WebApplication UseAppPipeline(this WebApplication app)
    {
        // 1. Request ID paling awal
        app.UseMiddleware<RequestIdMiddleware>();

        // 2. Exception handling (wrap semua setelah ini)
        app.UseMiddleware<ExceptionMiddleware>();

        // 3. Global timeout to stop runaway requests early
        app.UseMiddleware<RequestTimeoutMiddleware>();

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

        // 4. Tambahkan RequestId/UserId ke log context sebelum request logging
        app.UseMiddleware<LoggingContextMiddleware>();

        // 5. Serilog request logging cukup sekali, setelah context lengkap
        if (!app.Environment.IsEnvironment("Testing"))
        {
            app.UseSerilogRequestLogging();
        }

        // 6. Rate limiting setelah auth supaya policy berbasis user bisa bekerja
        app.UseRateLimiter();

        // 7. Idempotency (butuh user + requestId)
        app.UseMiddleware<IdempotencyMiddleware>();

        app.MapControllers();
        app.MapHealthChecks("/health"); // ✅ lightweight

        app.MapHealthChecks("/ready", new HealthCheckOptions
        {
            Predicate = _ => true,
            ResponseWriter = HealthCheckResponseWriter.WriteResponse
        });

        return app;
    }

}
