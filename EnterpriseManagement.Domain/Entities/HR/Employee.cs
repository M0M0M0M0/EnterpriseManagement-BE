using EnterpriseManagement.Domain.Entities.Sales;
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
    public string? Email { get; set; }
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

    // KPI Plan (mức doanh số + tỷ lệ hoa hồng) áp dụng cho nhân viên này khi tính hoa hồng —
    // 1 nhân viên chỉ thuộc đúng 1 Plan tại 1 thời điểm, gán/gỡ qua trang quản lý KPI Plan.
    public long? KpiPlanId { get; set; }
    public KpiPlan? KpiPlan { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
