using EnterpriseManagement.Application.DTOs;
using EnterpriseManagement.Domain.Entities.HR;

namespace EnterpriseManagement.Application.Interfaces;

public interface INotificationService
{
    // ---- Gửi (dùng bởi các nghiệp vụ). Lỗi khi gửi KHÔNG BAO GIỜ làm hỏng nghiệp vụ chính. ----

    // Lưu thông báo vào hộp của người nhận và đẩy tới các thiết bị của họ (nếu đã cấu hình Firebase).
    Task NotifyAsync(long employeeId, string type, string title, string body, string? route = null, long? refId = null);

    // Gửi cho người đang có thẩm quyền duyệt của `requester` (quản lý trực tiếp, hoặc cấp trên nếu quản lý đang nghỉ phép).
    Task NotifyApproverAsync(Employee requester, string type, string title, string body, string? route = null, long? refId = null);

    // Gửi cho mọi nhân viên đang có một permission (vd attendance.note.manage), trừ người trong danh sách loại trừ.
    Task NotifyPermissionHoldersAsync(
        string permissionCode, long excludeEmployeeId, string type, string title, string body, string? route = null, long? refId = null);

    // ---- Đọc (cho API) ----
    Task<IEnumerable<NotificationDto>> GetListAsync(string employeeCode, bool unreadOnly, int take, long? beforeId);
    Task<int> GetUnreadCountAsync(string employeeCode);
    Task MarkReadAsync(string employeeCode, long id);
    Task MarkAllReadAsync(string employeeCode);
    Task RegisterDeviceTokenAsync(string employeeCode, RegisterDeviceTokenRequest request);
    Task UnregisterDeviceTokenAsync(string employeeCode, string token);
}
