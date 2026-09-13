using EnterpriseManagement.Domain.Entities.HR;

namespace EnterpriseManagement.Domain.Entities.Sales;

public class KpiPlan
{
    public long Id { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public ICollection<KpiLevel> Levels { get; set; } = new List<KpiLevel>();
    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}
