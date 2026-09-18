namespace EnterpriseManagement.Application.DTOs;

public class ManagerDashboardDto
{
    public string ManagerCode { get; set; } = string.Empty;
    public int TeamSize { get; set; }
    public int PendingLeaveRequestsCount { get; set; }
    public int PendingAttendanceAdjustmentsCount { get; set; }
    public int TeamPresentTodayCount { get; set; }
    public int TeamAbsentTodayCount { get; set; }
}
