namespace EnterpriseManagement.Application.DTOs;

// Ghi chú ngoại lệ chấm công. Chọn nhân viên, từ ngày đến ngày và buổi giống form xin nghỉ; hệ thống tách thành từng (ngày, buổi).
public class CreateAttendanceNoteRequest
{
    public string EmployeeCode { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }

    // "Morning", "Afternoon" hoặc "FullDay"
    public string Session { get; set; } = "FullDay";

    // "OnTime" (đi làm đúng giờ), "Late" (đi muộn) hoặc "Absent" (vắng)
    public string Status { get; set; } = "OnTime";

    public string Note { get; set; } = string.Empty;
}

public class AttendanceNoteDto
{
    public long Id { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public string Session { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
    public string CreatedByName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
