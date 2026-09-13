using EnterpriseManagement.Application.DTOs;

namespace EnterpriseManagement.Application.Interfaces;

public interface IUserService
{
    Task<LoginResult> LoginAsync(LoginRequest request);
    Task<CreateUserResult> CreateAsync(CreateUserRequest request);
    Task<IEnumerable<UserDto>> GetAllAsync();
    Task<UserDto> SetActiveAsync(string username, bool isActive);
    Task<CreateUserResult> ResetPasswordAsync(string username);
    Task ChangePasswordAsync(string username, string currentPassword, string newPassword);
}
