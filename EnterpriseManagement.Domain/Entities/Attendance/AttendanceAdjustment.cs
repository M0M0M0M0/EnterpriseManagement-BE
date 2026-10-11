using EnterpriseManagement.Domain.Entities.HR;
using EnterpriseManagement.Domain.Enums;

namespace EnterpriseManagement.Domain.Entities.Attendance;

public class AttendanceAdjustment
{
    public long Id { get; set; }

    public long AttendanceId { get; set; }
    public AttendanceRecord Attendance { get; set; } = null!;

    public long RequestedBy { get; set; }
    public Employee Requester { get; set; } = null!;

    public string Reason { get; set; } = string.Empty;
    public DateTime? OldCheckInTime { get; set; }
    public DateTime? NewCheckInTime { get; set; }
    public DateTime? OldCheckOutTime { get; set; }
    public DateTime? NewCheckOutTime { get; set; }

    public ApprovalStatus Status { get; set; }
    public long? ApprovedBy { get; set; }
    public Employee? Approver { get; set; }
    public DateTime? ApprovedAt { get; set; }

    // Bước 2: người có quyền xử lý (vd HR) áp dụng vào dữ liệu chấm công hoặc từ chối sau khi Manager đã duyệt.
    public long? ProcessedBy { get; set; }
    public Employee? Processor { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? RejectionReason { get; set; }

    public DateTime CreatedAt { get; set; }
}
