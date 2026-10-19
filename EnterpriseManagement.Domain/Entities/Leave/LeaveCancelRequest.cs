using EnterpriseManagement.Domain.Entities.HR;
using EnterpriseManagement.Domain.Enums;

namespace EnterpriseManagement.Domain.Entities.Leave;

// Yêu cầu hủy một đơn nghỉ phép ĐÃ DUYỆT và CHƯA bắt đầu (chưa có buổi nghỉ nào bắt đầu). Quản lý duyệt thì đơn nghỉ gốc
// chuyển sang Cancelled và phép được hoàn lại. Kỳ nghỉ đã bắt đầu thì không hủy được mà dùng "Rút ngắn nghỉ phép" (LeaveReturnRequest).
public class LeaveCancelRequest
{
    public long Id { get; set; }

    public long LeaveRequestId { get; set; }
    public LeaveRequest LeaveRequest { get; set; } = null!;

    public long EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public string Reason { get; set; } = string.Empty;
    public LeaveCancelStatus Status { get; set; }

    public long? ManagerId { get; set; }
    public Employee? Manager { get; set; }
    public DateTime? DecidedAt { get; set; }
    public string? RejectionReason { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
