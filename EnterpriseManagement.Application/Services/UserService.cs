using EnterpriseManagement.Application.Common;
using EnterpriseManagement.Application.DTOs;
using EnterpriseManagement.Application.Interfaces;
using EnterpriseManagement.Domain.Entities.Identity;

namespace EnterpriseManagement.Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IAuditLogService _auditLogService;
    private readonly ICurrentUserService _currentUserService;
    private readonly UserAccountRules _rules;

    public UserService(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IAuditLogService auditLogService,
        ICurrentUserService currentUserService)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _auditLogService = auditLogService;
        _currentUserService = currentUserService;
        _rules = new UserAccountRules(userRepository, roleRepository, currentUserService);
    }

    private Task LogAsync(string action, long? entityId) =>
        _auditLogService.LogAsync(_currentUserService.UserId, action, "User", entityId, _currentUserService.IpAddress);

    public async Task<LoginResult> LoginAsync(LoginRequest request)
    {
        var user = await _userRepository.GetByUsernameAsync(request.Username)
            ?? throw new InvalidOperationException("Tên đăng nhập hoặc mật khẩu không đúng.");

        if (!user.IsActive)
        {
            throw new InvalidOperationException("Tài khoản này đã bị khoá.");
        }

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new InvalidOperationException("Tên đăng nhập hoặc mật khẩu không đúng.");
        }

        user.LastLoginAt = VietnamClock.Now;
        await _userRepository.SaveChangesAsync();

        var roles = user.UserRoles.Select(ur => ur.Role.RoleCode).ToList();
        var permissions = user.UserRoles
            .Where(ur => ur.Role.IsActive)
            .SelectMany(ur => ur.Role.RolePermissions)
            .Where(rp => rp.Permission.IsActive)
            .Select(rp => rp.Permission.PermissionCode)
            .Distinct()
            .ToList();
        var token = _jwtTokenGenerator.GenerateToken(user, roles, permissions);

        return new LoginResult
        {
            Token = token,
            Username = user.Username,
            EmployeeCode = user.Employee.EmployeeCode,
            Roles = roles,
            Permissions = permissions
        };
    }

    public async Task<IEnumerable<UserDto>> GetAllAsync()
    {
        var users = await _userRepository.GetAllAsync();
        return users.Select(ToDto);
    }

    public async Task<UserDto> UpdateRolesAsync(string username, IReadOnlyCollection<string> roleCodes)
    {
        var user = await _userRepository.GetByUsernameAsync(username)
            ?? throw new InvalidOperationException($"Không tìm thấy tên đăng nhập '{username}'.");

        await _rules.ApplyRolesAsync(user, roleCodes);

        await _userRepository.SaveChangesAsync();
        await LogAsync("UpdateRoles", user.Id);

        var updated = await _userRepository.GetByUsernameAsync(username);
        return ToDto(updated!);
    }

    public async Task<ResetPasswordResult> ResetPasswordAsync(string username)
    {
        var user = await _userRepository.GetByUsernameAsync(username)
            ?? throw new InvalidOperationException($"Không tìm thấy tên đăng nhập '{username}'.");

        var generatedPassword = RandomCodeGenerator.Generate(10);
        user.PasswordHash = _passwordHasher.Hash(generatedPassword);
        user.UpdatedAt = VietnamClock.Now;
        await _userRepository.SaveChangesAsync();
        await LogAsync("ResetPassword", user.Id);

        return new ResetPasswordResult
        {
            Username = user.Username,
            GeneratedPassword = generatedPassword,
            User = ToDto(user)
        };
    }

    public async Task ChangePasswordAsync(string username, string currentPassword, string newPassword)
    {
        var user = await _userRepository.GetByUsernameAsync(username)
            ?? throw new InvalidOperationException($"Không tìm thấy tên đăng nhập '{username}'.");

        if (!_passwordHasher.Verify(currentPassword, user.PasswordHash))
        {
            throw new InvalidOperationException("Mật khẩu hiện tại không đúng.");
        }

        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 8)
        {
            throw new InvalidOperationException("Mật khẩu mới phải có ít nhất 8 ký tự.");
        }

        if (_passwordHasher.Verify(newPassword, user.PasswordHash))
        {
            throw new InvalidOperationException("Mật khẩu mới phải khác mật khẩu hiện tại.");
        }

        user.PasswordHash = _passwordHasher.Hash(newPassword);
        user.UpdatedAt = VietnamClock.Now;
        await _userRepository.SaveChangesAsync();
    }

    public async Task<UserDto> SetActiveAsync(string username, bool isActive)
    {
        var user = await _userRepository.GetByUsernameAsync(username)
            ?? throw new InvalidOperationException($"Không tìm thấy tên đăng nhập '{username}'.");

        if (!isActive && user.IsActive)
        {
            await _rules.EnsureNotLockingOutAsync(user, removingAdminRole: false);
        }

        // Chỉ khoá/mở đăng nhập, không đổi trạng thái làm việc (Cho nghỉ việc/Phục hồi nằm ở PUT /employees/{code}/active).
        user.IsActive = isActive;
        user.UpdatedAt = VietnamClock.Now;
        await _userRepository.SaveChangesAsync();
        await LogAsync(isActive ? "Unlock" : "Lock", user.Id);

        return ToDto(user);
    }

    private static UserDto ToDto(User user) => new()
    {
        Username = user.Username,
        Email = user.Employee.Email,
        EmployeeCode = user.Employee.EmployeeCode,
        EmployeeName = $"{user.Employee.FirstName} {user.Employee.LastName}",
        Roles = user.UserRoles.Select(ur => ur.Role.RoleCode).ToList(),
        IsActive = user.IsActive,
        LastLoginAt = user.LastLoginAt
    };
}
