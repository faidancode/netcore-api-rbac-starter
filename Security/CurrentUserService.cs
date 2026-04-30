using System.Security.Claims;

namespace netcore_api_rbac_starter.Security;

public interface ICurrentUserService
{
    Guid UserId { get; }
    string? RequestId { get; }
    string Email { get; }
    bool IsAuthenticated { get; }
    IEnumerable<string> Permissions { get; }
    bool HasPermission(string action, string subject);
}

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    public Guid UserId
    {
        get
        {
            var value = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                        ?? User?.FindFirst("nameid")?.Value;
            return Guid.TryParse(value, out var id) ? id : Guid.Empty;
        }
    }

    public string? RequestId =>
       _httpContextAccessor.HttpContext?.Items["X-Request-ID"]?.ToString();

    public string Email => User?.FindFirst(ClaimTypes.Email)?.Value ?? "";

    public IEnumerable<string> Permissions =>
        User?.FindAll("permission").Select(c => c.Value) ?? Enumerable.Empty<string>();

    public bool HasPermission(string action, string subject)
    {
        // "manage:all" is super-admin
        if (Permissions.Contains("manage:all")) return true;
        // exact match action:subject
        if (Permissions.Contains($"{action}:{subject}")) return true;
        // manage:Subject grants all actions on that subject
        if (Permissions.Contains($"manage:{subject}")) return true;
        return false;
    }
}
