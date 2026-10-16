namespace EnterpriseManagement.Application.DTOs;

public class UpdateEmployeeRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string DepartmentCode { get; set; } = string.Empty;
    public string PositionCode { get; set; } = string.Empty;
    public string? ManagerCode { get; set; }

    // Tuỳ chọn: null = giữ nguyên email hiện tại.
    public string? Email { get; set; }

    // Tuỳ chọn: null = giữ nguyên vai trò; nếu gửi thì phải có ít nhất 1 mã vai trò tồn tại.
    public List<string>? RoleCodes { get; set; }
}
