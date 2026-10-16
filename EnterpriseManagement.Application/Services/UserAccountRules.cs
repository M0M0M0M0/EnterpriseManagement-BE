using System.Text.RegularExpressions;
using EnterpriseManagement.Application.Common;
using EnterpriseManagement.Application.Interfaces;
using EnterpriseManagement.Domain.Entities.Identity;

namespace EnterpriseManagement.Application.Services;

// Quy tắc dùng chung cho tài khoản đăng nhập (UserService và EmployeeService cùng dùng, vì tài khoản giờ gắn 1-1 với hồ sơ
// và được sửa ở cả màn hình Quản lý nhân viên). Không tự lưu DB: chỉ thay đổi entity đang được theo dõi, người gọi tự SaveChanges.
public class UserAccountRules
{
    public const string AdminRoleCode = "ADMIN";

    private static readonly Regex UsernamePattern = new("^[A-Za-z0-9._-]{3,50}$", RegexOptions.Compiled);

    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly ICurrentUserService _currentUserService;

    public UserAccountRules(IUserRepository userRepository, IRoleRepository roleRepository, ICurrentUserService currentUserService)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _currentUserService = currentUserService;
    }

    public static string NormalizeUsername(string? username)
    {
        var value = (username ?? string.Empty).Trim();
        if (!UsernamePattern.IsMatch(value))
        {
            throw new InvalidOperationException("Tên đăng nhập gồm 3-50 ký tự chữ, số, dấu chấm, gạch dưới hoặc gạch ngang (không dấu, không dấu cách).");
        }

        return value;
    }

    public static string NormalizeEmail(string? email)
    {
        var value = (email ?? string.Empty).Trim();
        if (!IsValidEmail(value))
        {
            throw new InvalidOperationException("Email không hợp lệ.");
        }

        return value;
    }

    public static bool IsValidEmail(string email)
    {
        if (email.Length == 0 || email.Length > 254) return false;
        try
        {
            var parsed = new System.Net.Mail.MailAddress(email);
            return parsed.Address == email && parsed.Host.Contains('.');
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public bool IsCurrentUser(User user) =>
        string.Equals(user.Username, _currentUserService.Username, StringComparison.OrdinalIgnoreCase);

    // Đặt đúng tập vai trò mới cho tài khoản (thêm cái chưa có, bỏ cái không còn). Luôn phải có ít nhất một vai trò.
    public async Task ApplyRolesAsync(User user, IReadOnlyCollection<string> roleCodes)
    {
        var wanted = roleCodes.Where(c => !string.IsNullOrWhiteSpace(c)).Select(c => c.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (wanted.Count == 0)
        {
            throw new InvalidOperationException("Tài khoản phải có ít nhất một vai trò.");
        }

        var roles = new List<Role>();
        foreach (var code in wanted)
        {
            roles.Add(await _roleRepository.GetByCodeAsync(code)
                ?? throw new InvalidOperationException($"Không tìm thấy vai trò có mã '{code}'."));
        }

        var wantedCodes = roles.Select(r => r.RoleCode).ToList();
        var removingAdmin = user.UserRoles.Any(ur => ur.Role.RoleCode == AdminRoleCode) && wantedCodes.All(c => c != AdminRoleCode);
        if (removingAdmin)
        {
            await EnsureNotLockingOutAsync(user, removingAdminRole: true);
        }

        foreach (var existing in user.UserRoles.Where(ur => wantedCodes.All(c => c != ur.Role.RoleCode)).ToList())
        {
            _userRepository.RemoveUserRole(existing);
            user.UserRoles.Remove(existing);
        }

        foreach (var role in roles.Where(r => user.UserRoles.All(ur => ur.Role.RoleCode != r.RoleCode)))
        {
            user.UserRoles.Add(new UserRole { UserId = user.Id, User = user, Role = role, RoleId = role.Id, AssignedAt = VietnamClock.Now });
        }
    }

    // Không cho tự khoá/tự gỡ ADMIN của chính mình, và luôn phải còn ít nhất một tài khoản ADMIN đang hoạt động khác.
    public async Task EnsureNotLockingOutAsync(User target, bool removingAdminRole)
    {
        if (IsCurrentUser(target))
        {
            throw new InvalidOperationException(removingAdminRole
                ? "Bạn không thể tự gỡ vai trò ADMIN của chính mình."
                : "Bạn không thể tự khoá tài khoản của chính mình.");
        }

        var isAdmin = target.UserRoles.Any(ur => ur.Role.RoleCode == AdminRoleCode);
        if (!isAdmin) return;

        var all = await _userRepository.GetAllAsync();
        var otherActiveAdmins = all.Count(u => u.Id != target.Id && u.IsActive && u.UserRoles.Any(ur => ur.Role.RoleCode == AdminRoleCode));
        if (otherActiveAdmins == 0)
        {
            throw new InvalidOperationException("Không thể thực hiện vì đây là tài khoản ADMIN hoạt động cuối cùng.");
        }
    }
}
