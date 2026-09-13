namespace EnterpriseManagement.Domain.Entities.Sales;

public class KpiLevel
{
    public long Id { get; set; }

    public long KpiPlanId { get; set; }
    public KpiPlan KpiPlan { get; set; } = null!;

    public int LevelOrder { get; set; }
    public decimal MinimumRevenue { get; set; }
    public decimal CommissionRate { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
