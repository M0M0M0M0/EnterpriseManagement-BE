namespace EnterpriseManagement.Application.DTOs;

public class CalculateCommissionRequest
{
    public string EmployeeCode { get; set; } = string.Empty;
    public DateOnly PeriodStartDate { get; set; }
    public DateOnly PeriodEndDate { get; set; }
}
