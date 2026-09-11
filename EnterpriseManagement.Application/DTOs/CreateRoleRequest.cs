namespace EnterpriseManagement.Application.DTOs;

public class CreateRoleRequest
{
    public string RoleCode { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<string> Permissions { get; set; } = new();
}
