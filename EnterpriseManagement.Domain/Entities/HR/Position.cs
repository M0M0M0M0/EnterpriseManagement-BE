namespace EnterpriseManagement.Domain.Entities.HR;

public class Position
{
    public long Id { get; set; }
    public string PositionCode { get; set; } = string.Empty;
    public string PositionName { get; set; } = string.Empty;
    public string? Description { get; set; }
    // Vai trò mặc định gán cho tài khoản khi tạo nhân viên có chức vụ này (null = EMPLOYEE).
    public string? RoleCode { get; set; }
    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
    public PositionSalary? Salary { get; set; }
}
