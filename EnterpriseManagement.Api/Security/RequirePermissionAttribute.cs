using Microsoft.AspNetCore.Authorization;

namespace EnterpriseManagement.Api.Security;

// Dùng [RequirePermission("employee.view")] thay vì [Authorize(Roles = "...")] khi muốn
// endpoint được mở/khóa theo Permission thật (do Admin gán qua màn hình Role/Permission),
// thay vì gắn cứng theo 3 role ADMIN/MANAGER/EMPLOYEE trong code.
public class RequirePermissionAttribute : AuthorizeAttribute
{
    public const string PolicyPrefix = "permission:";

    public RequirePermissionAttribute(string permissionCode) : base(PolicyPrefix + permissionCode)
    {
    }
}
