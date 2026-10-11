using EnterpriseManagement.Application.DTOs;

namespace EnterpriseManagement.Application.Interfaces;

public interface IUserService
{
    Task<LoginResult> LoginAsync(LoginRequest request);
    Task<CreateUserResult> CreateAsync(CreateUserRequest request);
    Task<IEnumerable<UserDto>> GetAllAsync();
    Task<UserDto> SetActiveAsync(string username, bool isActive);

    // Sửa email, hồ sơ nhân viên liên kết, vai trò và trạng thái. Vai trò mới có hiệu lực ở lần đăng nhập kế tiếp của tài khoản đó.
    Task<UserDto> UpdateAsync(string username, UpdateUserRequest request);
    Task<UserDto> UpdateRolesAsync(string username, IReadOnlyCollection<string> roleCodes);
    Task<CreateUserResult> ResetPasswordAsync(string username);
    Task ChangePasswordAsync(string username, string currentPassword, string newPassword);
}
