namespace EnterpriseManagement.Application.DTOs;

public class EmployeeDto
{
    public string EmployeeCode { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string DepartmentCode { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string PositionCode { get; set; } = string.Empty;
    public string PositionName { get; set; } = string.Empty;
    public string? ManagerCode { get; set; }
    public string? ManagerName { get; set; }
    public string EmploymentStatus { get; set; } = string.Empty;
    public DateOnly HireDate { get; set; }

    // false = tài khoản hệ thống (vd admin): không chấm công, không nằm trong danh sách chọn quản lý/trưởng phòng.
    public bool TracksAttendance { get; set; } = true;

    // ---- Tài khoản đăng nhập gắn 1-1 với hồ sơ ----
    public string Username { get; set; } = string.Empty;
    public List<string> RoleCodes { get; set; } = new();
    public List<string> RoleNames { get; set; } = new();
    public bool IsAccountActive { get; set; }
    public DateTime? LastLoginAt { get; set; }
}
