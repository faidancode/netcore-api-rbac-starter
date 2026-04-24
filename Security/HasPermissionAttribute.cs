using Microsoft.AspNetCore.Authorization;

namespace netcore_api_rbac_starter.Security;

public class HasPermissionAttribute : AuthorizeAttribute
{
    public HasPermissionAttribute(string action, string subject)
        : base(policy: $"{action}:{subject}")
    {
    }
}