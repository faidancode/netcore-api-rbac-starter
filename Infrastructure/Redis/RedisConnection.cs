using StackExchange.Redis;

public static class RedisConnection
{
    public static void AddRedis(this IServiceCollection services, IConfiguration config)
    {
        var conn = config.GetConnectionString("Redis");

        if (string.IsNullOrWhiteSpace(conn))
            throw new InvalidOperationException("Redis connection string is not configured");

        services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            var options = ConfigurationOptions.Parse(conn);

            options.AbortOnConnectFail = false;

            return ConnectionMultiplexer.Connect(options);
        });
    }
}