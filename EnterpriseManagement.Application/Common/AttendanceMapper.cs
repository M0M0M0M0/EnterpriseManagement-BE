using EnterpriseManagement.Application.DTOs;
using EnterpriseManagement.Domain.Entities.HR;

namespace EnterpriseManagement.Application.Common;

// Chuyển trạng thái hiệu lực của một ngày thành DTO trả cho client. Dùng chung để mọi API trả cùng một cách hiểu.
public static class AttendanceMapper
{
    public static AttendanceRecordDto ToRecordDto(EffectiveDay day, Employee employee) => new()
    {
        EmployeeCode = employee.EmployeeCode,
        EmployeeName = $"{employee.FirstName} {employee.LastName}",
        AttendanceDate = day.Date,
        IsWeekend = day.IsWeekend,
        IsHoliday = day.IsHoliday,
        HolidayName = day.HolidayName,
        OvertimeMinutes = day.OvertimeMinutes,
        CheckInTime = day.CheckIn,
        CheckOutTime = day.CheckOut,
        WorkingHours = day.WorkingHours,
        Status = day.OverallStatus,
        MorningStatus = day.Morning.ToString(),
        AfternoonStatus = day.Afternoon.ToString(),
        MorningSource = day.MorningSource.ToString(),
        AfternoonSource = day.AfternoonSource.ToString(),
        LeaveTypeName = day.LeaveTypeName,
        MorningNote = day.MorningNote,
        AfternoonNote = day.AfternoonNote
    };

    public static AttendanceCalendarDayDto ToCalendarDto(EffectiveDay day) => new()
    {
        Date = day.Date,
        IsWeekend = day.IsWeekend,
        IsHoliday = day.IsHoliday,
        HolidayName = day.HolidayName,
        OvertimeMinutes = day.OvertimeMinutes,
        Morning = day.Morning.ToString(),
        Afternoon = day.Afternoon.ToString(),
        MorningSource = day.MorningSource.ToString(),
        AfternoonSource = day.AfternoonSource.ToString(),
        LeaveTypeName = day.LeaveTypeName,
        MorningNote = day.MorningNote,
        AfternoonNote = day.AfternoonNote,
        CheckInTime = day.CheckIn,
        CheckOutTime = day.CheckOut
    };
}
