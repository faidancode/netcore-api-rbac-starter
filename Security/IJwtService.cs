
using System.Security.Claims;
using netcore_api_rbac_starter.Entities;
namespace netcore_api_rbac_starter.Security;

public interface IJwtService
{
    string GenerateAccessToken(User user, IEnumerable<Permission> permissions);
    string GenerateRefreshToken();
    ClaimsPrincipal? ValidateRefreshToken(string token);
    Guid? GetUserIdFromToken(string token);
}