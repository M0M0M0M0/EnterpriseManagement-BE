namespace EnterpriseManagement.Application.DTOs;

public class SetLeaveBalanceRequest
{
    public string EmployeeCode { get; set; } = string.Empty;
    public string LeaveTypeCode { get; set; } = string.Empty;
    public int Year { get; set; }
    public int? Month { get; set; }
    public decimal AllocatedTime { get; set; }
}
