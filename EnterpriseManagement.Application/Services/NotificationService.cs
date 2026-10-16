using EnterpriseManagement.Application.Common;
using EnterpriseManagement.Application.DTOs;
using EnterpriseManagement.Application.Interfaces;
using EnterpriseManagement.Domain.Entities.HR;
using EnterpriseManagement.Domain.Entities.Notifications;

namespace EnterpriseManagement.Application.Services;

public class NotificationService : INotificationService
{
    private static readonly string[] Platforms = { "android", "ios", "web" };

    private readonly INotificationRepository _repository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IApprovalDelegationResolver _approvalDelegationResolver;
    private readonly IPushSender _pushSender;

    public NotificationService(
        INotificationRepository repository,
        IEmployeeRepository employeeRepository,
        IApprovalDelegationResolver approvalDelegationResolver,
        IPushSender pushSender)
    {
        _repository = repository;
        _employeeRepository = employeeRepository;
        _approvalDelegationResolver = approvalDelegationResolver;
        _pushSender = pushSender;
    }

    // ---------------- Gửi ----------------

    public async Task NotifyAsync(long employeeId, string type, string title, string body, string? route = null, long? refId = null)
    {
        try
        {
            var notification = new AppNotification
            {
                EmployeeId = employeeId,
                Type = type,
                Title = title,
                Body = body,
                Route = route,
                RefId = refId,
                IsRead = false,
                CreatedAt = VietnamClock.Now
            };

            await _repository.AddAsync(notification);
            await _repository.SaveChangesAsync();

            await PushAsync(notification);
        }
        catch
        {
            // Thông báo là việc phụ: lỗi (mất mạng tới Firebase, DB bận...) không được làm hỏng duyệt đơn, chấm công...
        }
    }

    public async Task NotifyApproverAsync(Employee requester, string type, string title, string body, string? route = null, long? refId = null)
    {
        try
        {
            var approver = await _approvalDelegationResolver.ResolveApproverAsync(requester);
            if (approver is null) return;

            await NotifyAsync(approver.Id, type, title, body, route, refId);
        }
        catch
        {
            // Xem ghi chú ở NotifyAsync.
        }
    }

    public async Task NotifyPermissionHoldersAsync(
        string permissionCode, long excludeEmployeeId, string type, string title, string body, string? route = null, long? refId = null)
    {
        try
        {
            var recipients = await _repository.GetEmployeeIdsWithPermissionAsync(permissionCode);
            foreach (var employeeId in recipients.Where(id => id != excludeEmployeeId).Distinct())
            {
                await NotifyAsync(employeeId, type, title, body, route, refId);
            }
        }
        catch
        {
            // Xem ghi chú ở NotifyAsync.
        }
    }

    private async Task PushAsync(AppNotification notification)
    {
        if (!_pushSender.IsEnabled) return;

        var tokens = await _repository.GetDeviceTokensAsync(notification.EmployeeId);
        if (tokens.Count == 0) return;

        var data = new Dictionary<string, string>
        {
            ["notificationId"] = notification.Id.ToString(),
            ["type"] = notification.Type,
            ["route"] = notification.Route ?? string.Empty
        };

        var invalid = await _pushSender.SendAsync(tokens, notification.Title, notification.Body, data);
        if (invalid.Count > 0)
        {
            await _repository.RemoveDeviceTokensAsync(invalid);
        }
    }

    // ---------------- Đọc ----------------

    public async Task<IEnumerable<NotificationDto>> GetListAsync(string employeeCode, bool unreadOnly, int take, long? beforeId)
    {
        var employee = await GetEmployeeAsync(employeeCode);

        var items = await _repository.GetListAsync(employee.Id, unreadOnly, Math.Clamp(take, 1, 100), beforeId);
        return items.Select(n => new NotificationDto
        {
            Id = n.Id,
            Type = n.Type,
            Title = n.Title,
            Body = n.Body,
            Route = n.Route,
            RefId = n.RefId,
            IsRead = n.IsRead,
            CreatedAt = n.CreatedAt
        });
    }

    public async Task<int> GetUnreadCountAsync(string employeeCode)
    {
        var employee = await GetEmployeeAsync(employeeCode);
        return await _repository.CountUnreadAsync(employee.Id);
    }

    public async Task MarkReadAsync(string employeeCode, long id)
    {
        var employee = await GetEmployeeAsync(employeeCode);

        var notification = await _repository.GetOwnedAsync(id, employee.Id)
            ?? throw new InvalidOperationException("Không tìm thấy thông báo.");

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAt = VietnamClock.Now;
            await _repository.SaveChangesAsync();
        }
    }

    public async Task MarkAllReadAsync(string employeeCode)
    {
        var employee = await GetEmployeeAsync(employeeCode);
        await _repository.MarkAllReadAsync(employee.Id, VietnamClock.Now);
    }

    public async Task RegisterDeviceTokenAsync(string employeeCode, RegisterDeviceTokenRequest request)
    {
        var employee = await GetEmployeeAsync(employeeCode);

        var token = request.Token?.Trim() ?? string.Empty;
        if (token.Length == 0 || token.Length > 512)
        {
            throw new InvalidOperationException("Token thiết bị không hợp lệ.");
        }

        var platform = Platforms.Contains(request.Platform?.ToLowerInvariant()) ? request.Platform!.ToLowerInvariant() : "android";
        await _repository.UpsertDeviceTokenAsync(employee.Id, token, platform, VietnamClock.Now);
    }

    public async Task UnregisterDeviceTokenAsync(string employeeCode, string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return;
        var employee = await GetEmployeeAsync(employeeCode);

        await _repository.RemoveDeviceTokenAsync(token.Trim(), employee.Id);
    }

    // Mọi tài khoản đều có hồ sơ nhân viên; không tìm thấy nghĩa là token hỏng/hồ sơ đã bị xoá.
    private async Task<Employee> GetEmployeeAsync(string employeeCode) =>
        await _employeeRepository.GetByEmployeeCodeAsync(employeeCode)
            ?? throw new InvalidOperationException($"Không tìm thấy nhân viên có mã '{employeeCode}'.");
}
