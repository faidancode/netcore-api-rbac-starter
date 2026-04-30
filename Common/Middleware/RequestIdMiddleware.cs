public class RequestIdMiddleware
{
    private const string HeaderName = "X-Request-ID";
    private readonly RequestDelegate _next;

    public RequestIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        var requestId = context.Request.Headers[HeaderName].FirstOrDefault()
                        ?? Guid.NewGuid().ToString("N");

        context.Items[HeaderName] = requestId;
        context.Response.Headers[HeaderName] = requestId;

        using (context.RequestServices
            .GetRequiredService<ILogger<RequestIdMiddleware>>()
            .BeginScope(new Dictionary<string, object>
            {
                ["RequestId"] = requestId
            }))
        {
            await _next(context);
        }
    }
}