namespace EnterpriseManagement.Application.DTOs;

public class UserDto
{
    public string Username { get; set; } = string.Empty;

    // Email lấy từ hồ sơ nhân viên (tài khoản không còn email riêng).
    public string Email { get; set; } = string.Empty;
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
    public bool IsActive { get; set; }
    public DateTime? LastLoginAt { get; set; }
}
