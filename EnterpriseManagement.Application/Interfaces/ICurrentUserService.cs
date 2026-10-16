namespace EnterpriseManagement.Application.Interfaces;

public interface ICurrentUserService
{
    long? UserId { get; }
    // Mọi tài khoản đều gắn hồ sơ nhân viên nên token đăng nhập luôn có mã nhân viên (rỗng chỉ khi chưa đăng nhập).
    string EmployeeCode { get; }
    string? Username { get; }
    string? IpAddress { get; }
    bool IsInRole(string role);
    IReadOnlyCollection<string> Permissions { get; }
}
