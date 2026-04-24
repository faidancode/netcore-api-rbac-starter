using netcore_api_rbac_starter.Modules.Auth.Dtos;

namespace netcore_api_rbac_starter.Modules.Auth;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request);
    Task<LoginResponse> RefreshAsync(RefreshRequest request);
    Task<MeResponse> GetMeAsync(Guid userId);
    Task<IEnumerable<PermissionDto>> GetMyPermissionsAsync(Guid userId);
}

