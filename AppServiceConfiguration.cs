using FluentValidation;
using FluentValidation.AspNetCore;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using netcore_api_rbac_starter.Data;
using netcore_api_rbac_starter.Modules.Auth;
using netcore_api_rbac_starter.Modules.Dashboard;
using netcore_api_rbac_starter.Modules.Departments;
using netcore_api_rbac_starter.Modules.Positions;
using netcore_api_rbac_starter.Modules.Roles;
using netcore_api_rbac_starter.Modules.Users;
using netcore_api_rbac_starter.Modules.Employees;
using netcore_api_rbac_starter.Security;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using netcore_api_rbac_starter.Common.Models;
using System.Text.Json.Serialization;
using System.Security.Claims;
using System.Threading.RateLimiting;

namespace netcore_api_rbac_starter;

public static class AppServiceConfiguration
{
    public static WebApplicationBuilder AddAppServices(this WebApplicationBuilder builder)
    {
        var services = builder.Services;
        var config = builder.Configuration;
        var env = builder.Environment;

        services.AddOpenApi();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(
                        new JsonStringEnumConverter()
                    );
                });

        services.AddApiVersioning(options =>
        {
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.ReportApiVersions = true;
        });

        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var errors = context.ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
                    );

                return new BadRequestObjectResult(
                    Response<object>.Fail("Validation failed", errors)
                );
            };
        });

        services.AddFluentValidationAutoValidation();
        services.AddValidatorsFromAssemblyContaining<Program>();
        services.AddHttpContextAccessor();

        services.AddCors(options =>
        {
            options.AddPolicy("AngularApp", policy =>
            {
                policy.WithOrigins("http://localhost:4200")
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        // Services
        services.AddScoped<IUsersService, UsersService>();
        services.AddScoped<IRolesService, RolesService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IDepartmentsService, DepartmentsService>();
        services.AddScoped<IPositionsService, PositionsService>();
        services.AddScoped<IEmployeesService, EmployeesService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<AuditService>();
        services.AddScoped<IEventHandler<IAuditableEvent>, GenericAuditHandler>();
        services.AddScoped<IEventDispatcher, EventDispatcher>();


        // Authorization
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddAuthorization();

        // ✅ DATABASE (clean & standard)
        if (env.IsEnvironment("Testing"))
        {
            services.AddDbContext<AppDbContext>(opt =>
                opt.UseInMemoryDatabase("TestDb"));
        }
        else
        {
            services.AddDbContext<AppDbContext>(opt =>
                opt.UseNpgsql(config.GetConnectionString("Default")));
        }

        services.AddRedis(config);

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.ContentType = "application/json";
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.HttpContext.Response.WriteAsJsonAsync(
                    Response<object>.Fail("Too many requests.", code: "RATE_LIMITED"),
                    cancellationToken);
            };

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            {
                var clientKey = GetClientPartitionKey(httpContext);

                return RateLimitPartition.GetFixedWindowLimiter(clientKey, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 300,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                });
            });

            options.AddPolicy("per-user", httpContext =>
            {
                var userKey = GetUserPartitionKey(httpContext);

                return RateLimitPartition.GetFixedWindowLimiter(userKey, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 120,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                });
            });

            options.AddPolicy("login", httpContext =>
            {
                var clientKey = GetClientPartitionKey(httpContext);

                return RateLimitPartition.GetFixedWindowLimiter(clientKey, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 5,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                });
            });
        });

        // Collection 
        services.Configure<JwtOptions>(config.GetSection("Jwt"));

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                var jwtSection = config.GetSection("Jwt");
                var jwtSecret = jwtSection["Secret"];

                if (string.IsNullOrEmpty(jwtSecret))
                    throw new InvalidOperationException("Missing configuration value: Jwt:Secret");

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtSecret)
                    ),
                    ValidateIssuer = true,
                    ValidIssuer = jwtSection["Issuer"],
                    ValidateAudience = true,
                    ValidAudience = jwtSection["Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
            });

        services.AddHealthChecks().AddDbContextCheck<AppDbContext>("database").AddRedis("localhost:6381", name: "redis");

        return builder;
    }

    private static string GetClientPartitionKey(HttpContext context)
    {
        return context.Request.Headers["X-Forwarded-For"].FirstOrDefault()
               ?? context.Connection.RemoteIpAddress?.ToString()
               ?? "unknown-ip";
    }

    private static string GetUserPartitionKey(HttpContext context)
    {
        return context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
               ?? context.User.FindFirst("sub")?.Value
               ?? GetClientPartitionKey(context);
    }
}
