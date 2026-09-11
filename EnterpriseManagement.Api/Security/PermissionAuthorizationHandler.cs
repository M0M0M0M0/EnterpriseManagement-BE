using EnterpriseManagement.Application.Common;
using Microsoft.AspNetCore.Authorization;

namespace EnterpriseManagement.Api.Security;

public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        // ADMIN luôn có mọi quyền, không phụ thuộc RolePermissions đã gán hay chưa —
        // tránh tình huống admin tự khóa quyền của chính mình.
        if (context.User.IsInRole("ADMIN") ||
            context.User.HasClaim(AppClaimTypes.Permission, requirement.PermissionCode))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
