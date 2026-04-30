using System.Security.Claims;
using StackExchange.Redis;

public class IdempotencyMiddleware
{
    private const string HeaderName = "Idempotency-Key";
    private readonly RequestDelegate _next;
    private readonly IDatabase _redis;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public IdempotencyMiddleware(
        RequestDelegate next,
        IConnectionMultiplexer redis,
        IHttpContextAccessor accessor)
    {
        _next = next;
        _redis = redis.GetDatabase();
        _httpContextAccessor = accessor;
    }

    public async Task Invoke(HttpContext context)
    {
        if (context.Request.Method != HttpMethods.Post)
        {
            await _next(context);
            return;
        }

        var key = context.Request.Headers[HeaderName].FirstOrDefault();
        if (string.IsNullOrEmpty(key))
        {
            await _next(context);
            return;
        }

        var userId = context.User.FindFirst("sub")?.Value
             ?? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
             ?? "anonymous";

        var path = context.Request.Path.ToString().ToLower();

        var redisKey = $"idem:{userId}:{path}:{key}";

        // ✅ cek existing
        var existing = await _redis.StringGetAsync(redisKey);
        if (existing.HasValue)
        {
            context.Response.StatusCode = StatusCodes.Status200OK;
            await context.Response.WriteAsync(existing!);
            return;
        }

        // 👉 capture response
        var originalBody = context.Response.Body;
        using var ms = new MemoryStream();
        context.Response.Body = ms;

        await _next(context);

        ms.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(ms).ReadToEndAsync();

        // ✅ save ke redis (TTL 24 jam)
        await _redis.StringSetAsync(
            redisKey,
            body,
            TimeSpan.FromHours(24)
        );

        ms.Seek(0, SeekOrigin.Begin);
        await ms.CopyToAsync(originalBody);
    }
}