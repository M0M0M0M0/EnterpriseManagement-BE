using EnterpriseManagement.Domain.Entities.HR;
using EnterpriseManagement.Domain.Enums;

namespace EnterpriseManagement.Domain.Entities.Attendance;

// Đơn xin làm thêm giờ (OT). Quản lý duyệt như đơn nghỉ; đơn đã duyệt được cộng vào công của chính ngày đó.
public class OvertimeRequest
{
    public long Id { get; set; }

    public long EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public DateOnly Date { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }

    // Số phút làm thêm (EndTime - StartTime), lưu sẵn để cộng dồn theo tháng/năm.
    public int Minutes { get; set; }

    public string Reason { get; set; } = string.Empty;
    public OvertimeStatus Status { get; set; }

    public long? ApprovedBy { get; set; }
    public Employee? Approver { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? RejectionReason { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
