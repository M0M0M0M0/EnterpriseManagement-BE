namespace EnterpriseManagement.Application.DTOs;

public class CreateEmployeeResult
{
    public EmployeeDto Employee { get; set; } = new();

    // Mật khẩu sinh ngẫu nhiên, chỉ trả về đúng một lần lúc tạo.
    public string GeneratedPassword { get; set; } = string.Empty;
}
