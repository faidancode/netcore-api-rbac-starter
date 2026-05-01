using System.Text.Json;
using netcore_api_rbac_starter.Common.Exceptions;
using netcore_api_rbac_starter.Common.Models;

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
            _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
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