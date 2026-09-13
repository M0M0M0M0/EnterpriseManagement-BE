using EnterpriseManagement.Domain.Entities.HR;
using EnterpriseManagement.Domain.Enums;

namespace EnterpriseManagement.Domain.Entities.Sales;

public class SalesCommission
{
    public long Id { get; set; }

    public long EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public DateOnly PeriodStartDate { get; set; }
    public DateOnly PeriodEndDate { get; set; }
    public decimal TotalRevenue { get; set; }

    public long KpiLevelId { get; set; }
    public KpiLevel KpiLevel { get; set; } = null!;

    public decimal CommissionRate { get; set; }
    public decimal CommissionAmount { get; set; }

    public CommissionStatus Status { get; set; }
    public long? ApprovedBy { get; set; }
    public Employee? Approver { get; set; }
    public DateTime? ApprovedAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
