namespace EnterpriseManagement.Application.DTOs;

public class LoginResult
{
    public string Token { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    // Luôn có giá trị: mọi tài khoản đều gắn một hồ sơ nhân viên (kể cả admin).
    public string EmployeeCode { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();

    // Các permission đang có hiệu lực của tài khoản (cùng nguồn với claim trong JWT), để client ẩn/hiện nút theo từng hành động.
    public List<string> Permissions { get; set; } = new();
}
