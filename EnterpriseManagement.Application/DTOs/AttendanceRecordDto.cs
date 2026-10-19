namespace EnterpriseManagement.Application.DTOs;

public class AttendanceRecordDto
{
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public DateOnly AttendanceDate { get; set; }
    public DateTime? CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public decimal? WorkingHours { get; set; }
    public string Status { get; set; } = string.Empty;

    public bool IsWeekend { get; set; }
    public bool IsHoliday { get; set; }
    public string? HolidayName { get; set; }

    // Số phút làm thêm giờ đã được duyệt trong ngày.
    public int OvertimeMinutes { get; set; }

    // Trạng thái hiệu lực tách theo buổi (None, OnTime, Late, Absent, OnLeave) và nguồn của nó; Status ở trên chỉ là
    // bản tóm tắt cả ngày theo cách gọi cũ.
    public string MorningStatus { get; set; } = "None";
    public string AfternoonStatus { get; set; } = "None";
    public string MorningSource { get; set; } = "None";
    public string AfternoonSource { get; set; } = "None";
    public string? LeaveTypeName { get; set; }
    public string? MorningNote { get; set; }
    public string? AfternoonNote { get; set; }
}
