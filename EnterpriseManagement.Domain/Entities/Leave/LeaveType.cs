using EnterpriseManagement.Domain.Enums;

namespace EnterpriseManagement.Domain.Entities.Leave;

public class LeaveType
{
    public long Id { get; set; }
    public string LeaveTypeCode { get; set; } = string.Empty;
    public string LeaveTypeName { get; set; } = string.Empty;

    // Mức cấp phép mặc định, vd 1 (ngày/tháng), 2 (giờ/tháng), 9 (ngày/năm) —
    // diễn giải cụ thể phụ thuộc AccrualUnit + AccrualPeriod. Xem LeaveBalanceService.
    public decimal AccrualAmount { get; set; }
    public LeaveUnit AccrualUnit { get; set; }
    public LeaveAccrualPeriod AccrualPeriod { get; set; }

    public bool IsPaid { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}
