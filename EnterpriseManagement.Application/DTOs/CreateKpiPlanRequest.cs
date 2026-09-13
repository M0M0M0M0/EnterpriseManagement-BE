namespace EnterpriseManagement.Application.DTOs;

public class CreateKpiPlanRequest
{
    public string PlanName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<KpiLevelInput> Levels { get; set; } = new();
}
