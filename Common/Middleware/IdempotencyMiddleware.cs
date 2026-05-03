using System.Security.Claims;
using System.Text.Json;
using StackExchange.Redis;

public class IdempotencyMiddleware
{
    private const string HeaderName = "Idempotency-Key";

    private readonly RequestDelegate _next;
    private readonly IDatabase _redis;

    public IdempotencyMiddleware(
        RequestDelegate next,
        IConnectionMultiplexer redis)
    {
        _next = next;
        _redis = redis.GetDatabase();
    }

    public async Task Invoke(HttpContext context)
    {
        // Only apply to POST requests
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
        var lockKey = $"lock:{redisKey}";

        // ----------------------------------------
        // 1. Try get cached response
        // ----------------------------------------
        var existing = await _redis.StringGetAsync(redisKey);

        if (existing.HasValue)
        {
            var cached = JsonSerializer.Deserialize<CachedResponse>((string)existing!);

            context.Response.StatusCode = cached!.StatusCode;
            context.Response.ContentType = "application/json";

            await context.Response.WriteAsync(cached.Body);
            return;
        }

        // ----------------------------------------
        // 2. Acquire distributed lock (IMPORTANT)
        // ----------------------------------------
        var lockAcquired = await _redis.StringSetAsync(
            lockKey,
            "1",
            TimeSpan.FromSeconds(10),
            When.NotExists
        );

        if (!lockAcquired)
        {
            // Another request is processing the same key
            // Wait for cached result (polling)

            for (int i = 0; i < 10; i++)
            {
                await Task.Delay(100);

                var retry = await _redis.StringGetAsync(redisKey);
                if (retry.HasValue)
                {
                    var cached = JsonSerializer.Deserialize<CachedResponse>((string)retry!);

                    context.Response.StatusCode = cached!.StatusCode;
                    context.Response.ContentType = "application/json";

                    await context.Response.WriteAsync(cached.Body);
                    return;
                }
            }

            // If still no result, return conflict
            context.Response.StatusCode = StatusCodes.Status409Conflict;
            await context.Response.WriteAsync("Duplicate request in progress");
            return;
        }

        // ----------------------------------------
        // 3. Execute request and capture response
        // ----------------------------------------
        var originalBody = context.Response.Body;
        using var ms = new MemoryStream();

        try
        {
            context.Response.Body = ms;

            // 🔥 THIS is the _next(context) you asked about
            await _next(context);

            // Only cache successful responses (2xx)
            if (context.Response.StatusCode >= 200 && context.Response.StatusCode < 300)
            {
                ms.Seek(0, SeekOrigin.Begin);
                var body = await new StreamReader(ms).ReadToEndAsync();

                if (!string.IsNullOrWhiteSpace(body))
                {
                    var cachedResponse = new CachedResponse
                    {
                        StatusCode = context.Response.StatusCode,
                        Body = body
                    };

                    var serialized = JsonSerializer.Serialize(cachedResponse);

                    await _redis.StringSetAsync(
                        redisKey,
                        serialized,
                        TimeSpan.FromHours(24)
                    );
                }
            }

            // Copy response back to original stream
            ms.Seek(0, SeekOrigin.Begin);
            await ms.CopyToAsync(originalBody);
        }
        finally
        {
            // Always restore response body
            context.Response.Body = originalBody;

            // Release lock
            await _redis.KeyDeleteAsync(lockKey);
        }
    }
}