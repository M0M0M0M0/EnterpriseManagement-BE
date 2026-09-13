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

    public UserService(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IEmployeeRepository employeeRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _employeeRepository = employeeRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

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

        user.LastLoginAt = DateTime.UtcNow;
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
        string? roleCodeFromPosition = null;
        if (!string.IsNullOrWhiteSpace(request.EmployeeCode))
        {
            var employee = await _employeeRepository.GetByEmployeeCodeAsync(request.EmployeeCode)
                ?? throw new InvalidOperationException($"Employee code '{request.EmployeeCode}' not found.");
            employeeId = employee.Id;
            roleCodeFromPosition = employee.Position.RoleCode;
        }

        // Role được chọn tay ưu tiên; nếu không chọn thì lấy role mặc định gán sẵn cho chức
        // vụ của hồ sơ (Admin cấu hình ở màn hình Chức vụ), tránh phải nhớ chọn đúng role
        // mỗi lần tạo tài khoản.
        var roleCode = !string.IsNullOrWhiteSpace(request.RoleCode) ? request.RoleCode : roleCodeFromPosition;
        if (string.IsNullOrWhiteSpace(roleCode))
        {
            throw new InvalidOperationException(
                "Không xác định được vai trò cho tài khoản này — vui lòng chọn role thủ công, hoặc gán role cho chức vụ của nhân viên ở màn hình Chức vụ trước.");
        }

        var role = await _roleRepository.GetByCodeAsync(roleCode)
            ?? throw new InvalidOperationException($"Role code '{roleCode}' not found.");

        var generatedPassword = RandomCodeGenerator.Generate(10);

        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = _passwordHasher.Hash(generatedPassword),
            EmployeeId = employeeId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        user.UserRoles.Add(new UserRole { Role = role, AssignedAt = DateTime.UtcNow });

        await _userRepository.AddAsync(user);
        await _userRepository.SaveChangesAsync();

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
        user.UpdatedAt = DateTime.UtcNow;
        await _userRepository.SaveChangesAsync();

        return new CreateUserResult
        {
            User = ToDto(user),
            GeneratedPassword = generatedPassword
        };
    }

    public async Task<UserDto> SetActiveAsync(string username, bool isActive)
    {
        var user = await _userRepository.GetByUsernameAsync(username)
            ?? throw new InvalidOperationException($"Username '{username}' not found.");

        user.IsActive = isActive;
        user.UpdatedAt = DateTime.UtcNow;
        await _userRepository.SaveChangesAsync();

        return ToDto(user);
    }

    private static UserDto ToDto(User user) => new()
    {
        Username = user.Username,
        Email = user.Email,
        EmployeeCode = user.Employee?.EmployeeCode,
        Roles = user.UserRoles.Select(ur => ur.Role.RoleCode).ToList(),
        IsActive = user.IsActive,
        LastLoginAt = user.LastLoginAt
    };
}
