using EnterpriseManagement.Application.DTOs;

namespace EnterpriseManagement.Application.Interfaces;

// Tài khoản đăng nhập gắn 1-1 với hồ sơ nhân viên: tạo/sửa email nằm ở IEmployeeService.
// Ở đây chỉ còn đăng nhập, khoá/mở, đặt lại/đổi mật khẩu và đổi vai trò.
public interface IUserService
{
    Task<LoginResult> LoginAsync(LoginRequest request);
    Task<IEnumerable<UserDto>> GetAllAsync();
    Task<UserDto> SetActiveAsync(string username, bool isActive);

    // Vai trò mới có hiệu lực ở lần đăng nhập kế tiếp của tài khoản đó.
    Task<UserDto> UpdateRolesAsync(string username, IReadOnlyCollection<string> roleCodes);
    Task<ResetPasswordResult> ResetPasswordAsync(string username);
    Task ChangePasswordAsync(string username, string currentPassword, string newPassword);
}
