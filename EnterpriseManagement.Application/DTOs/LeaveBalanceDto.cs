namespace EnterpriseManagement.Application.DTOs;

public class LeaveBalanceDto
{
    public string LeaveTypeCode { get; set; } = string.Empty;
    public string LeaveTypeName { get; set; } = string.Empty;
    public int Year { get; set; }
    public int? Month { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal AllocatedTime { get; set; }
    public decimal UsedTime { get; set; }
    public decimal RemainingTime { get; set; }
}
