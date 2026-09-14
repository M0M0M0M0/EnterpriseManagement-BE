namespace EnterpriseManagement.Application.Interfaces;

public interface ICurrentUserService
{
    long? UserId { get; }
    string? EmployeeCode { get; }
    string? Username { get; }
    string? IpAddress { get; }
    bool IsInRole(string role);
    IReadOnlyCollection<string> Permissions { get; }
}
