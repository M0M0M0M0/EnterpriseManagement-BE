namespace EnterpriseManagement.Application.DTOs;

public class SubmitLeaveCancelRequest
{
    public long LeaveRequestId { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class RejectLeaveCancelRequest
{
    public string? RejectionReason { get; set; }
}

public class LeaveCancelRequestDto
{
    public long Id { get; set; }
    public long LeaveRequestId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string LeaveTypeName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Session { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal TotalTime { get; set; }
    public string Reason { get; set; } = string.Empty;

    // Pending, Approved, Rejected, Withdrawn
    public string Status { get; set; } = string.Empty;
    public string? ManagerName { get; set; }
    public DateTime? DecidedAt { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime CreatedAt { get; set; }
}
