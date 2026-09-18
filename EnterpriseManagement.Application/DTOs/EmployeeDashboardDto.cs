namespace EnterpriseManagement.Application.DTOs;

public class EmployeeDashboardDto
{
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public AttendanceRecordDto? TodayAttendance { get; set; }
    public decimal TotalRemainingLeaveDays { get; set; }
    public int PendingLeaveRequestsCount { get; set; }
    public int PendingAttendanceAdjustmentsCount { get; set; }
}
