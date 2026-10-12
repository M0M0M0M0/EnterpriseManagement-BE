namespace EnterpriseManagement.Application.DTOs;

public class DepartmentDto
{
    public string DepartmentCode { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string? ManagerCode { get; set; }
    public string? ManagerName { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}
