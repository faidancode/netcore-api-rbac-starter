using netcore_api_rbac_starter.Common.Models;

namespace netcore_api_rbac_starter.Common.Middleware;

public class RequestTimeoutMiddleware
{
    private readonly RequestDelegate _next;
    private readonly TimeSpan _timeout;

    public RequestTimeoutMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;

        var seconds = configuration.GetValue<int?>("RequestTimeoutSeconds") ?? 30;
        _timeout = TimeSpan.FromSeconds(Math.Max(1, seconds));
    }

    public async Task Invoke(HttpContext context)
    {
        var originalRequestAborted = context.RequestAborted;
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(originalRequestAborted);
        context.RequestAborted = timeoutCts.Token;

        var nextTask = _next(context);
        var timeoutTask = Task.Delay(_timeout);

        var completed = await Task.WhenAny(nextTask, timeoutTask);

        if (completed == timeoutTask)
        {
            timeoutCts.Cancel();

            if (!context.Response.HasStarted)
            {
                context.Response.Clear();
                context.Response.StatusCode = StatusCodes.Status408RequestTimeout;
                await context.Response.WriteAsJsonAsync(
                    Response<object>.Fail("Request timed out.", code: "REQUEST_TIMEOUT"));
            }

            return;
        }

        await nextTask;
    }
}
