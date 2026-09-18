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

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
