namespace EnterpriseManagement.Application.DTOs;

// Kết quả đặt lại mật khẩu: mật khẩu mới chỉ trả về đúng một lần.
public class ResetPasswordResult
{
    public string Username { get; set; } = string.Empty;
    public string GeneratedPassword { get; set; } = string.Empty;
    public UserDto User { get; set; } = new();
}
