namespace EnterpriseManagement.Application.DTOs;

public class SubmitOvertimeRequest
{
    public DateOnly Date { get; set; }

    // Giờ dạng "HH:mm".
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}

public class RejectOvertimeRequest
{
    public string? RejectionReason { get; set; }
}

public class OvertimeRequestDto
{
    public long Id { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public int Minutes { get; set; }
    public string Reason { get; set; } = string.Empty;

    // Pending, Approved, Rejected, Cancelled
    public string Status { get; set; } = string.Empty;
    public string? ApproverName { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime CreatedAt { get; set; }
}

// Hạn mức OT theo luật (tháng 40 giờ, năm 200 giờ) và số đã dùng/đang chờ của nhân viên.
public class OvertimeQuotaDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public int MonthUsedMinutes { get; set; }
    public int MonthLimitMinutes { get; set; }
    public int YearUsedMinutes { get; set; }
    public int YearLimitMinutes { get; set; }
}
