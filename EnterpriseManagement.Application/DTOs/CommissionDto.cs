namespace EnterpriseManagement.Application.DTOs;

public class CommissionDto
{
    public long Id { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public DateOnly PeriodStartDate { get; set; }
    public DateOnly PeriodEndDate { get; set; }
    public decimal TotalRevenue { get; set; }
    public string KpiPlanName { get; set; } = string.Empty;
    public int LevelOrder { get; set; }
    public decimal CommissionRate { get; set; }
    public decimal CommissionAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ApproverName { get; set; }
    public DateTime? ApprovedAt { get; set; }
}
