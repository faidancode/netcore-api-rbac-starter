using Microsoft.AspNetCore.Authorization;

namespace netcore_api_rbac_starter.Security;

public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var permissions = context.User.FindAll("permission").Select(c => c.Value).ToList();

        bool granted =
            permissions.Contains("manage:all") ||
            permissions.Contains($"manage:{requirement.Subject}") ||
            permissions.Contains($"{requirement.Action}:{requirement.Subject}");

        if (granted)
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}