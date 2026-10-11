using EnterpriseManagement.Domain.Entities.HR;
using EnterpriseManagement.Domain.Enums;

namespace EnterpriseManagement.Domain.Entities.Leave;

// Yêu cầu đi làm lại khi đang có nghỉ phép đã duyệt. Nhân viên chỉ chọn buổi bắt đầu đi làm lại, hệ thống tự gắn vào
// đơn nghỉ đang phủ buổi đó (xem LeaveReturnSession). Đơn nghỉ gốc không bao giờ bị sửa.
public class LeaveReturnRequest
{
    public long Id { get; set; }

    public long EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public DateOnly FromDate { get; set; }
    public DaySession FromSession { get; set; }
    public string Reason { get; set; } = string.Empty;

    public ReturnRequestStatus Status { get; set; }

    public long? ManagerId { get; set; }
    public Employee? Manager { get; set; }
    public DateTime? ManagerApprovedAt { get; set; }

    public long? ProcessedBy { get; set; }
    public Employee? Processor { get; set; }
    public DateTime? ProcessedAt { get; set; }

    public string? RejectionReason { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public ICollection<LeaveReturnSession> Sessions { get; set; } = new List<LeaveReturnSession>();
}
