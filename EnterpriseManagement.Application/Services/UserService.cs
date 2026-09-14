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
            Roles = roles
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
