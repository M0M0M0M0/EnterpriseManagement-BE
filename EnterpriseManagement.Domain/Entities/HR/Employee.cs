using EnterpriseManagement.Domain.Entities.Identity;
using EnterpriseManagement.Domain.Enums;

namespace EnterpriseManagement.Domain.Entities.HR;

public class Employee
{
    public long Id { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateOnly? DateOfBirth { get; set; }
    public Gender? Gender { get; set; }
    public string? Phone { get; set; }
    // Email duy nhất của người này (bắt buộc). Tài khoản đăng nhập không còn email riêng.
    public string Email { get; set; } = string.Empty;
    public string? Address { get; set; }

    public long DepartmentId { get; set; }
    public Department Department { get; set; } = null!;

    public long PositionId { get; set; }
    public Position Position { get; set; } = null!;

    public long? ManagerId { get; set; }
    public Employee? Manager { get; set; }
    public ICollection<Employee> Subordinates { get; set; } = new List<Employee>();

    public DateOnly HireDate { get; set; }
    public EmploymentStatus EmploymentStatus { get; set; }

    // false = tài khoản hệ thống (vd admin): không chấm công, không bị tính vắng mặt, không nhận nhắc chấm công,
    // không nằm trong bảng công/nghỉ phép và không được chọn làm quản lý/trưởng phòng.
    public bool TracksAttendance { get; set; } = true;

    // Tài khoản đăng nhập của người này (quan hệ 1-1 bắt buộc, tạo cùng lúc với hồ sơ).
    public User? User { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
