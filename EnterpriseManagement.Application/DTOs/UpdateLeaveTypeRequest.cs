namespace EnterpriseManagement.Application.DTOs;

// Mã loại nghỉ (LeaveTypeCode) không đổi được vì các đơn nghỉ và logic khác tham chiếu theo mã.
public class UpdateLeaveTypeRequest
{
    public string LeaveTypeName { get; set; } = string.Empty;
    public decimal AccrualAmount { get; set; }
    public string AccrualUnit { get; set; } = string.Empty;
    public string AccrualPeriod { get; set; } = string.Empty;
    public bool IsPaid { get; set; }
    public string? Description { get; set; }
}
