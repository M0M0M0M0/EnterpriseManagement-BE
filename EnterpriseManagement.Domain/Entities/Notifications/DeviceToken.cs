using EnterpriseManagement.Domain.Entities.HR;

namespace EnterpriseManagement.Domain.Entities.Notifications;

// Token Firebase (FCM) của một thiết bị đang đăng nhập bằng tài khoản của nhân viên này.
// Một token chỉ thuộc về một nhân viên tại một thời điểm: đăng nhập người khác trên cùng máy thì token chuyển sang người đó.
public class DeviceToken
{
    public long Id { get; set; }

    public long EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public string Token { get; set; } = string.Empty;

    // android | ios | web
    public string Platform { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    public DateTime LastSeenAt { get; set; }
}
