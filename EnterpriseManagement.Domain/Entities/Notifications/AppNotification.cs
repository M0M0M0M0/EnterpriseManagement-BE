using EnterpriseManagement.Domain.Entities.HR;

namespace EnterpriseManagement.Domain.Entities.Notifications;

// Một thông báo trong hộp thông báo của nhân viên (đồng thời là nội dung thông báo đẩy nếu có gửi qua Firebase).
public class AppNotification
{
    public long Id { get; set; }

    // Người nhận.
    public long EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    // Loại thông báo (LeaveSubmitted, LeaveApproved, CheckInReminder...), client dùng để chọn icon/màu.
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;

    // Route menu của app để mở màn hình liên quan khi bấm vào (vd /manager/leave), có thể null.
    public string? Route { get; set; }

    // Id của đối tượng liên quan (đơn nghỉ, yêu cầu...), có thể null.
    public long? RefId { get; set; }

    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }
}
