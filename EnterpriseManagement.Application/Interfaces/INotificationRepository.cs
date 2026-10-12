using EnterpriseManagement.Domain.Entities.Notifications;

namespace EnterpriseManagement.Application.Interfaces;

public interface INotificationRepository
{
    Task AddAsync(AppNotification notification);
    Task<List<AppNotification>> GetListAsync(long employeeId, bool unreadOnly, int take, long? beforeId);
    Task<int> CountUnreadAsync(long employeeId);
    Task<AppNotification?> GetOwnedAsync(long id, long employeeId);
    Task MarkAllReadAsync(long employeeId, DateTime now);

    // Đã có thông báo loại này cho nhân viên kể từ mốc `since` chưa (chống gửi trùng nhắc nhở trong ngày).
    Task<bool> ExistsSinceAsync(long employeeId, string type, DateTime since);

    // Nhân viên đang có tài khoản hoạt động và có role đang bật chứa permission này (người xử lý như HR).
    Task<List<long>> GetEmployeeIdsWithPermissionAsync(string permissionCode);

    // Nhân viên đang làm việc, có tài khoản hoạt động (đối tượng của thông báo nhắc chấm công).
    Task<List<long>> GetActiveEmployeeIdsWithAccountAsync();

    Task UpsertDeviceTokenAsync(long employeeId, string token, string platform, DateTime now);
    Task RemoveDeviceTokenAsync(string token, long employeeId);
    Task<List<string>> GetDeviceTokensAsync(long employeeId);
    Task RemoveDeviceTokensAsync(IReadOnlyCollection<string> tokens);

    Task<int> SaveChangesAsync();
}
