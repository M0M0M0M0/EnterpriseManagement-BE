namespace EnterpriseManagement.Application.Interfaces;

public interface ICurrentUserService
{
    string? EmployeeCode { get; }
    string? Username { get; }
    bool IsInRole(string role);
    IReadOnlyCollection<string> Permissions { get; }
}
