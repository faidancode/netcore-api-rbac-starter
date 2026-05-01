using System.Text.Json;
using netcore_api_rbac_starter.Common.Exceptions;
using netcore_api_rbac_starter.Common.Models;
using Serilog.Context;
using System.Security.Claims;

namespace netcore_api_rbac_starter.Common.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            LogException(context, ex);
            await HandleExceptionAsync(context, ex);
        }
    }

    private void LogException(HttpContext context, Exception ex)
    {
        var requestId = ResolveRequestId(context);
        var userId = ResolveUserId(context);

        using (LogContext.PushProperty("RequestId", requestId))
        using (LogContext.PushProperty("UserId", userId))
        {
            _logger.LogError(ex,
                "Unhandled exception. {Method} {Path} responded 500. TraceId: {TraceId} RequestId: {RequestId} UserId: {UserId}",
                context.Request.Method,
                context.Request.Path,
                context.TraceIdentifier,
                requestId,
                userId
            );
        }
    }

    private static string ResolveRequestId(HttpContext context)
    {
        return context.Items.TryGetValue("X-Request-ID", out var requestIdObj) && requestIdObj is string requestId && !string.IsNullOrWhiteSpace(requestId)
            ? requestId
            : context.Request.Headers["X-Request-ID"].FirstOrDefault()
              ?? context.TraceIdentifier;
    }

    private static string ResolveUserId(HttpContext context)
    {
        return context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
               ?? context.User?.FindFirst("sub")?.Value
               ?? "anonymous";
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, message, code) = exception switch
        {
            AppException appEx => (appEx.StatusCode, appEx.Message, appEx.Code),
            _ => (500, "An unexpected error occurred.", "INTERNAL_ERROR")
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        var response = Response<object>.Fail(
            message: message,
            code: code
        );

        await context.Response.WriteAsJsonAsync(response);
    }
}
