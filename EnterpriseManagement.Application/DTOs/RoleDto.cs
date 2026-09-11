namespace EnterpriseManagement.Application.DTOs;

public class RoleDto
{
    public string RoleCode { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<string> Permissions { get; set; } = new();
    public bool IsActive { get; set; }
}
