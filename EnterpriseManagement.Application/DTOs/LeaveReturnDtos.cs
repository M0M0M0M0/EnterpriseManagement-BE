namespace EnterpriseManagement.Application.DTOs;

// Nhân viên chỉ chọn ngày và buổi bắt đầu đi làm lại; hệ thống tự tìm đơn nghỉ đang phủ buổi đó.
public class SubmitLeaveReturnRequest
{
    public DateOnly FromDate { get; set; }

    // "Morning" hoặc "Afternoon".
    public string FromSession { get; set; } = string.Empty;

    public string Reason { get; set; } = string.Empty;
}

public class LeaveReturnSessionDto
{
    public DateOnly Date { get; set; }
    public string Session { get; set; } = string.Empty;
    public long LeaveRequestId { get; set; }
    public string LeaveTypeName { get; set; } = string.Empty;

    // Pending, AwaitingHr, Active, Rejected
    public string State { get; set; } = string.Empty;
    public string? WorkedStatus { get; set; }
}

// Bản xem trước: các buổi sẽ được gỡ khỏi nghỉ phép nếu yêu cầu được duyệt.
public class LeaveReturnPreviewDto
{
    public List<LeaveReturnSessionDto> Sessions { get; set; } = new();
    public int SessionCount { get; set; }

    // Số ngày phép sẽ được hoàn (mỗi buổi 0,5 ngày).
    public decimal RefundDays { get; set; }
    public List<long> LeaveRequestIds { get; set; } = new();
}

public class LeaveReturnRequestDto
{
    public long Id { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public DateOnly FromDate { get; set; }
    public string FromSession { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;

    // PendingManager, PendingHr, Completed, Rejected
    public string Status { get; set; } = string.Empty;

    public List<LeaveReturnSessionDto> Sessions { get; set; } = new();
    public string? ManagerName { get; set; }
    public DateTime? ManagerApprovedAt { get; set; }
    public string? ProcessedByName { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ProcessLeaveReturnItem
{
    public DateOnly Date { get; set; }
    public string Session { get; set; } = string.Empty;

    // "OnTime" hoặc "Late"
    public string Status { get; set; } = "OnTime";
}

// Người xử lý (có quyền) xác nhận các buổi đã bắt đầu. Buổi không có trong Items mặc định là đi làm đúng giờ.
public class ProcessLeaveReturnRequest
{
    public List<ProcessLeaveReturnItem> Items { get; set; } = new();
}

public class RejectLeaveReturnRequest
{
    public string? Reason { get; set; }
}
