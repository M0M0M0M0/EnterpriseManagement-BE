namespace EnterpriseManagement.Application.DTOs;

public class TimesheetRowDto
{
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;

    // Số ngày công đi làm (mỗi buổi đúng giờ hoặc đi muộn = 0,5 ngày).
    public decimal WorkedDays { get; set; }
    public decimal LeaveDays { get; set; }
    public decimal AbsentDays { get; set; }
    public int LateSessions { get; set; }
    public decimal OvertimeHours { get; set; }
}

public class TimesheetDto
{
    public int Year { get; set; }
    public int Month { get; set; }

    // Số ngày làm việc chuẩn trong tháng (đã trừ cuối tuần và ngày lễ).
    public int StandardDays { get; set; }

    public bool IsClosed { get; set; }
    public DateTime? ClosedAt { get; set; }
    public string? ClosedByName { get; set; }
    public List<TimesheetRowDto> Rows { get; set; } = new();
}

public class AttendancePeriodDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public DateTime ClosedAt { get; set; }
    public string ClosedByName { get; set; } = string.Empty;
}
