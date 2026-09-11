namespace EnterpriseManagement.Application.DTOs;

public class PermissionDto
{
    public string PermissionCode { get; set; } = string.Empty;
    public string PermissionName { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}
