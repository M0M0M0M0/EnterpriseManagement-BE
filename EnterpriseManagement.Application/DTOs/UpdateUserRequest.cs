namespace EnterpriseManagement.Application.DTOs;

public class UpdateUserRolesRequest
{
    public List<string> RoleCodes { get; set; } = new();
}
