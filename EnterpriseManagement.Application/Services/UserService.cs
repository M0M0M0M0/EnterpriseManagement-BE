using EnterpriseManagement.Application.Common;
using EnterpriseManagement.Application.DTOs;
using EnterpriseManagement.Application.Interfaces;
using EnterpriseManagement.Domain.Entities.Identity;

namespace EnterpriseManagement.Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IAuditLogService _auditLogService;
    private readonly ICurrentUserService _currentUserService;

    public UserService(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IEmployeeRepository employeeRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IAuditLogService auditLogService,
        ICurrentUserService currentUserService)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _employeeRepository = employeeRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _auditLogService = auditLogService;
        _currentUserService = currentUserService;
    }

    private Task LogAsync(string action, long? entityId) =>
        _auditLogService.LogAsync(_currentUserService.UserId, action, "User", entityId, _currentUserService.IpAddress);

    public async Task<LoginResult> LoginAsync(LoginRequest request)
    {
        var user = await _userRepository.GetByUsernameAsync(request.Username)
            ?? throw new InvalidOperationException("Invalid username or password.");

        if (!user.IsActive)
        {
            throw new InvalidOperationException("This account has been locked.");
        }

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new InvalidOperationException("Invalid username or password.");
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
            EmployeeCode = user.Employee?.EmployeeCode,
            Roles = roles,
            Permissions = permissions
        };
    }

    public async Task<CreateUserResult> CreateAsync(CreateUserRequest request)
    {
        var existing = await _userRepository.GetByUsernameAsync(request.Username);
        if (existing is not null)
        {
            throw new InvalidOperationException($"Username '{request.Username}' already exists.");
        }

        long? employeeId = null;
        if (!string.IsNullOrWhiteSpace(request.EmployeeCode))
        {
            var employee = await _employeeRepository.GetByEmployeeCodeAsync(request.EmployeeCode)
                ?? throw new InvalidOperationException($"Employee code '{request.EmployeeCode}' not found.");
            employeeId = employee.Id;
        }

        // Chức vụ giờ chỉ là chức danh hiển thị, không còn gắn role mặc định — Admin luôn phải
        // chọn tay role khi tạo tài khoản.
        if (string.IsNullOrWhiteSpace(request.RoleCode))
        {
            throw new InvalidOperationException("Vui lòng chọn vai trò cho tài khoản này.");
        }

        var role = await _roleRepository.GetByCodeAsync(request.RoleCode)
            ?? throw new InvalidOperationException($"Role code '{request.RoleCode}' not found.");

        var generatedPassword = RandomCodeGenerator.Generate(10);

        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = _passwordHasher.Hash(generatedPassword),
            EmployeeId = employeeId,
            IsActive = true,
            CreatedAt = VietnamClock.Now
        };
        user.UserRoles.Add(new UserRole { Role = role, AssignedAt = VietnamClock.Now });

        await _userRepository.AddAsync(user);
        await _userRepository.SaveChangesAsync();
        await LogAsync("Create", user.Id);

        var created = await _userRepository.GetByUsernameAsync(request.Username);
        return new CreateUserResult
        {
            User = ToDto(created!),
            GeneratedPassword = generatedPassword
        };
    }

    public async Task<IEnumerable<UserDto>> GetAllAsync()
    {
        var users = await _userRepository.GetAllAsync();
        return users.Select(ToDto);
    }

    public async Task<UserDto> UpdateAsync(string username, UpdateUserRequest request)
    {
        var user = await _userRepository.GetByUsernameAsync(username)
            ?? throw new InvalidOperationException($"Username '{username}' not found.");

        if (request.Email is not null)
        {
            var email = request.Email.Trim();
            if (email.Length == 0 || !email.Contains('@'))
            {
                throw new InvalidOperationException("Email không hợp lệ.");
            }

            var all = await _userRepository.GetAllAsync();
            if (all.Any(u => u.Id != user.Id && string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException($"Email '{email}' đã được dùng cho tài khoản khác.");
            }

            user.Email = email;
        }

        if (request.EmployeeCode is not null)
        {
            if (string.IsNullOrWhiteSpace(request.EmployeeCode))
            {
                user.EmployeeId = null;
                user.Employee = null;
            }
            else
            {
                var employee = await _employeeRepository.GetByEmployeeCodeAsync(request.EmployeeCode)
                    ?? throw new InvalidOperationException($"Employee code '{request.EmployeeCode}' not found.");
                user.EmployeeId = employee.Id;
                user.Employee = employee;
            }
        }

        if (request.RoleCodes is not null)
        {
            await ApplyRolesAsync(user, request.RoleCodes);
        }

        if (request.IsActive is not null && request.IsActive.Value != user.IsActive)
        {
            if (!request.IsActive.Value)
            {
                await EnsureNotLockingOutAsync(user, removingAdminRole: false, deactivating: true);
            }

            user.IsActive = request.IsActive.Value;
        }

        await _userRepository.SaveChangesAsync();
        await LogAsync("Update", user.Id);

        var updated = await _userRepository.GetByUsernameAsync(username);
        return ToDto(updated!);
    }

    public async Task<UserDto> UpdateRolesAsync(string username, IReadOnlyCollection<string> roleCodes)
    {
        var user = await _userRepository.GetByUsernameAsync(username)
            ?? throw new InvalidOperationException($"Username '{username}' not found.");

        await ApplyRolesAsync(user, roleCodes);

        await _userRepository.SaveChangesAsync();
        await LogAsync("UpdateRoles", user.Id);

        var updated = await _userRepository.GetByUsernameAsync(username);
        return ToDto(updated!);
    }

    // Đặt đúng tập vai trò mới cho tài khoản (thêm cái chưa có, bỏ cái không còn). Luôn phải có ít nhất một vai trò.
    private async Task ApplyRolesAsync(User user, IReadOnlyCollection<string> roleCodes)
    {
        var wanted = roleCodes.Where(c => !string.IsNullOrWhiteSpace(c)).Select(c => c.Trim()).Distinct().ToList();
        if (wanted.Count == 0)
        {
            throw new InvalidOperationException("Tài khoản phải có ít nhất một vai trò.");
        }

        var roles = new List<Domain.Entities.Identity.Role>();
        foreach (var code in wanted)
        {
            roles.Add(await _roleRepository.GetByCodeAsync(code)
                ?? throw new InvalidOperationException($"Role code '{code}' not found."));
        }

        var removingAdmin = user.UserRoles.Any(ur => ur.Role.RoleCode == "ADMIN") && wanted.All(c => c != "ADMIN");
        if (removingAdmin)
        {
            await EnsureNotLockingOutAsync(user, removingAdminRole: true, deactivating: false);
        }

        foreach (var existing in user.UserRoles.Where(ur => wanted.All(c => c != ur.Role.RoleCode)).ToList())
        {
            _userRepository.RemoveUserRole(existing);
            user.UserRoles.Remove(existing);
        }

        foreach (var role in roles.Where(r => user.UserRoles.All(ur => ur.RoleId != r.Id)))
        {
            user.UserRoles.Add(new UserRole { UserId = user.Id, Role = role, RoleId = role.Id, AssignedAt = VietnamClock.Now });
        }
    }

    // Không cho tự khoá mình khỏi hệ thống, và luôn phải còn ít nhất một tài khoản ADMIN đang hoạt động khác.
    private async Task EnsureNotLockingOutAsync(User target, bool removingAdminRole, bool deactivating)
    {
        var isAdmin = target.UserRoles.Any(ur => ur.Role.RoleCode == "ADMIN");
        if (!isAdmin) return;

        if (string.Equals(target.Username, _currentUserService.Username, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(removingAdminRole
                ? "Bạn không thể tự gỡ vai trò ADMIN của chính mình."
                : "Bạn không thể tự khoá tài khoản của chính mình.");
        }

        var all = await _userRepository.GetAllAsync();
        var otherActiveAdmins = all.Count(u => u.Id != target.Id && u.IsActive && u.UserRoles.Any(ur => ur.Role.RoleCode == "ADMIN"));
        if (otherActiveAdmins == 0)
        {
            throw new InvalidOperationException("Không thể thực hiện vì đây là tài khoản ADMIN hoạt động cuối cùng.");
        }
    }

    public async Task<CreateUserResult> ResetPasswordAsync(string username)
    {
        var user = await _userRepository.GetByUsernameAsync(username)
            ?? throw new InvalidOperationException($"Username '{username}' not found.");

        var generatedPassword = RandomCodeGenerator.Generate(10);
        user.PasswordHash = _passwordHasher.Hash(generatedPassword);
        user.UpdatedAt = VietnamClock.Now;
        await _userRepository.SaveChangesAsync();
        await LogAsync("ResetPassword", user.Id);

        return new CreateUserResult
        {
            User = ToDto(user),
            GeneratedPassword = generatedPassword
        };
    }

    public async Task ChangePasswordAsync(string username, string currentPassword, string newPassword)
    {
        var user = await _userRepository.GetByUsernameAsync(username)
            ?? throw new InvalidOperationException($"Username '{username}' not found.");

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
            ?? throw new InvalidOperationException($"Username '{username}' not found.");

        if (!isActive && user.IsActive)
        {
            await EnsureNotLockingOutAsync(user, removingAdminRole: false, deactivating: true);
        }

        user.IsActive = isActive;
        user.UpdatedAt = VietnamClock.Now;
        await _userRepository.SaveChangesAsync();
        await LogAsync(isActive ? "Unlock" : "Lock", user.Id);

        return ToDto(user);
    }

    private static UserDto ToDto(User user) => new()
    {
        Username = user.Username,
        Email = user.Email,
        EmployeeCode = user.Employee?.EmployeeCode,
        EmployeeName = user.Employee is null ? null : $"{user.Employee.FirstName} {user.Employee.LastName}",
        Roles = user.UserRoles.Select(ur => ur.Role.RoleCode).ToList(),
        IsActive = user.IsActive,
        LastLoginAt = user.LastLoginAt
    };
}
