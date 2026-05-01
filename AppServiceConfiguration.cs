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
        services.AddControllers();

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
            options.AddFixedWindowLimiter("global", opt =>
            {
                opt.PermitLimit = 100; // 100 request
                opt.Window = TimeSpan.FromMinutes(1);
                opt.QueueLimit = 0;
            });

            options.AddFixedWindowLimiter("login", opt =>
            {
                opt.PermitLimit = 5; // 5 request
                opt.Window = TimeSpan.FromMinutes(1);
                opt.QueueLimit = 0;
            });
        });

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
}
