namespace EnterpriseManagement.Application.DTOs;

// Trạng thái hiệu lực của một ngày, tách theo buổi (Morning/Afternoon: None, OnTime, Late, Absent, OnLeave).
public class AttendanceCalendarDayDto
{
    public DateOnly Date { get; set; }
    public bool IsWeekend { get; set; }

    public string Morning { get; set; } = "None";
    public string Afternoon { get; set; } = "None";

    // Nguồn của từng buổi: None, Weekend, Leave, ReturnConfirmed, Note, Attendance, Missing.
    public string MorningSource { get; set; } = "None";
    public string AfternoonSource { get; set; } = "None";

    public string? LeaveTypeName { get; set; }
    public string? MorningNote { get; set; }
    public string? AfternoonNote { get; set; }

    public DateTime? CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
}
