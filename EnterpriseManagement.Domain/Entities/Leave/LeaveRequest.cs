using EnterpriseManagement.Domain.Entities.HR;
using EnterpriseManagement.Domain.Enums;

namespace EnterpriseManagement.Domain.Entities.Leave;

public class LeaveRequest
{
    public long Id { get; set; }

    public long EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public long LeaveTypeId { get; set; }
    public LeaveType LeaveType { get; set; } = null!;

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    // Chỉ có giá trị khi LeaveType tính theo buổi (AccrualPeriod != MonthlyReset theo quy
    // ước hiện tại — cụ thể là mọi loại trừ Nghỉ ngắn). Null với Nghỉ ngắn vì loại đó cho
    // chọn giờ bắt đầu/kết thúc cụ thể thay vì chọn buổi.
    public LeaveSession? Session { get; set; }

    public LeaveUnit Unit { get; set; }
    public decimal TotalTime { get; set; }
    public string? Reason { get; set; }

    public LeaveRequestStatus Status { get; set; }
    public long? ApprovedBy { get; set; }
    public Employee? Approver { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? RejectionReason { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
