namespace EnterpriseManagement.Application.DTOs;

// Tạo nhân viên = tạo hồ sơ + tài khoản đăng nhập cùng lúc.
public class CreateEmployeeRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string DepartmentCode { get; set; } = string.Empty;
    public string PositionCode { get; set; } = string.Empty;
    public string? ManagerCode { get; set; }
    public DateOnly HireDate { get; set; }

    // Tuỳ chọn, mặc định Probation (thử việc).
    public string? EmploymentStatus { get; set; }

    // ---- Tài khoản ----
    // 3-50 ký tự [A-Za-z0-9._-], duy nhất.
    public string Username { get; set; } = string.Empty;

    // Bắt buộc, hợp lệ, duy nhất (email nằm ở hồ sơ, dùng chung cho tài khoản).
    public string Email { get; set; } = string.Empty;

    // Tuỳ chọn: bỏ trống thì lấy vai trò mặc định của chức vụ, chức vụ không có thì EMPLOYEE.
    public string? RoleCode { get; set; }
}
