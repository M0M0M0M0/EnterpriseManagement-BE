namespace EnterpriseManagement.Application.DTOs;

public class AttendanceAdjustmentDto
{
    public long Id { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public DateOnly AttendanceDate { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime? OldCheckInTime { get; set; }
    public DateTime? NewCheckInTime { get; set; }
    public DateTime? OldCheckOutTime { get; set; }
    public DateTime? NewCheckOutTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ApproverName { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? ProcessedByName { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? RejectionReason { get; set; }
}
