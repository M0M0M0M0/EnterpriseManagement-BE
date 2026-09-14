namespace EnterpriseManagement.Application.DTOs;

public class SaleDto
{
    public long Id { get; set; }
    public string SaleCode { get; set; } = string.Empty;
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerPhone { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string DepartmentCode { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime OrderDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Note { get; set; }
    public string? RejectionReason { get; set; }
    public string? ApproverEmployeeCode { get; set; }
    public string? ApproverName { get; set; }
    public DateTime? ApprovedAt { get; set; }
}
