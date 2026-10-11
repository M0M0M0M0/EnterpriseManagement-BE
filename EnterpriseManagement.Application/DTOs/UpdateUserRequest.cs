namespace EnterpriseManagement.Application.DTOs;

// Trường nào null thì giữ nguyên. EmployeeCode rỗng ("") nghĩa là gỡ liên kết hồ sơ nhân viên.
public class UpdateUserRequest
{
    public string? Email { get; set; }
    public string? EmployeeCode { get; set; }
    public List<string>? RoleCodes { get; set; }
    public bool? IsActive { get; set; }
}

public class UpdateUserRolesRequest
{
    public List<string> RoleCodes { get; set; } = new();
}
