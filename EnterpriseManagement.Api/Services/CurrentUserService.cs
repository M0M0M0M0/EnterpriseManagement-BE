using System.Security.Claims;
using EnterpriseManagement.Application.Common;
using EnterpriseManagement.Application.Interfaces;

namespace EnterpriseManagement.Api.Services;

// Scoped: mỗi request có 1 HttpContext riêng, nên phải Scoped chứ không được Singleton —
// nếu Singleton thì EmployeeCode/Username của request đầu tiên sẽ bị "đóng băng" và dùng sai cho mọi request sau.
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? EmployeeCode => _httpContextAccessor.HttpContext?.User.FindFirstValue("employeeCode");

    public string? Username => _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Name);

    public bool IsInRole(string role) =>
        _httpContextAccessor.HttpContext?.User.IsInRole(role) ?? false;

    public IReadOnlyCollection<string> Permissions =>
        _httpContextAccessor.HttpContext?.User.FindAll(AppClaimTypes.Permission).Select(c => c.Value).ToList()
        ?? new List<string>();
}
