namespace EnterpriseManagement.Application.DTOs;

public class CreateUserRequest
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? RoleCode { get; set; }
    public string? EmployeeCode { get; set; }
}
