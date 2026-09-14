namespace EnterpriseManagement.Application.DTOs;

public class OrgTreeNodeDto
{
    public string EmployeeCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string PositionName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string EmploymentStatus { get; set; } = string.Empty;
    public string TodayAttendanceStatus { get; set; } = string.Empty;
    public decimal MonthlyRevenue { get; set; }
    public int SubordinateCount { get; set; }
    public List<OrgTreeNodeDto> Subordinates { get; set; } = new();
}
