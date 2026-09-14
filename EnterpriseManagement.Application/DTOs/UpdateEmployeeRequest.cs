namespace EnterpriseManagement.Application.DTOs;

public class UpdateEmployeeRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string DepartmentCode { get; set; } = string.Empty;
    public string PositionCode { get; set; } = string.Empty;
    public string? ManagerCode { get; set; }
}
