namespace EnterpriseManagement.Application.DTOs;

public class AssignedEmployeeDto
{
    public string EmployeeCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
}

public class KpiPlanDto
{
    public long Id { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public List<KpiLevelDto> Levels { get; set; } = new();
    public List<AssignedEmployeeDto> AssignedEmployees { get; set; } = new();
}
