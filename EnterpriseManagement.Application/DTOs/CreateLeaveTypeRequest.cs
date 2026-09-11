namespace EnterpriseManagement.Application.DTOs;

public class CreateLeaveTypeRequest
{
    public string LeaveTypeCode { get; set; } = string.Empty;
    public string LeaveTypeName { get; set; } = string.Empty;
    public decimal AccrualAmount { get; set; }
    public string AccrualUnit { get; set; } = string.Empty;
    public string AccrualPeriod { get; set; } = string.Empty;
    public bool IsPaid { get; set; }
    public string? Description { get; set; }
}
