namespace EnterpriseManagement.Application.DTOs;

// Trạng thái hiệu lực của một ngày, tách theo buổi (Morning/Afternoon: None, OnTime, Late, Absent, OnLeave).
public class AttendanceCalendarDayDto
{
    public DateOnly Date { get; set; }
    public bool IsWeekend { get; set; }

    // Ngày nghỉ lễ của công ty (có tên) và số phút làm thêm giờ đã được duyệt trong ngày.
    public bool IsHoliday { get; set; }
    public string? HolidayName { get; set; }
    public int OvertimeMinutes { get; set; }

    public string Morning { get; set; } = "None";
    public string Afternoon { get; set; } = "None";

    // Nguồn của từng buổi: None, Weekend, Holiday, Leave, ReturnConfirmed, Note, Attendance, Missing.
    public string MorningSource { get; set; } = "None";
    public string AfternoonSource { get; set; } = "None";

    public string? LeaveTypeName { get; set; }
    public string? MorningNote { get; set; }
    public string? AfternoonNote { get; set; }

    public DateTime? CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
}
