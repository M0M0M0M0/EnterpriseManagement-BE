using EnterpriseManagement.Application.Interfaces;
using EnterpriseManagement.Domain.Entities.Notifications;
using EnterpriseManagement.Domain.Enums;
using EnterpriseManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseManagement.Infrastructure.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly ApplicationDbContext _context;

    public NotificationRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(AppNotification notification) =>
        await _context.Notifications.AddAsync(notification);

    public async Task<List<AppNotification>> GetListAsync(long employeeId, bool unreadOnly, int take, long? beforeId)
    {
        var query = _context.Notifications.AsNoTracking().Where(n => n.EmployeeId == employeeId);
        if (unreadOnly) query = query.Where(n => !n.IsRead);
        if (beforeId is not null) query = query.Where(n => n.Id < beforeId.Value);

        return await query.OrderByDescending(n => n.Id).Take(take).ToListAsync();
    }

    public async Task<int> CountUnreadAsync(long employeeId) =>
        await _context.Notifications.CountAsync(n => n.EmployeeId == employeeId && !n.IsRead);

    public async Task<AppNotification?> GetOwnedAsync(long id, long employeeId) =>
        await _context.Notifications.FirstOrDefaultAsync(n => n.Id == id && n.EmployeeId == employeeId);

    public async Task MarkAllReadAsync(long employeeId, DateTime now) =>
        await _context.Notifications
            .Where(n => n.EmployeeId == employeeId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true).SetProperty(n => n.ReadAt, now));

    public async Task<bool> ExistsSinceAsync(long employeeId, string type, DateTime since) =>
        await _context.Notifications.AnyAsync(n => n.EmployeeId == employeeId && n.Type == type && n.CreatedAt >= since);

    public async Task<List<long>> GetEmployeeIdsWithPermissionAsync(string permissionCode) =>
        await _context.UserRoles
            .Where(ur => ur.User.IsActive
                && ur.User.EmployeeId != null
                && ur.Role.IsActive
                && ur.Role.RolePermissions.Any(rp => rp.Permission.IsActive && rp.Permission.PermissionCode == permissionCode))
            .Select(ur => ur.User.EmployeeId!.Value)
            .Distinct()
            .ToListAsync();

    public async Task<List<long>> GetActiveEmployeeIdsWithAccountAsync() =>
        await _context.Users
            .Where(u => u.IsActive && u.EmployeeId != null && u.Employee!.EmploymentStatus != EmploymentStatus.Terminated)
            .Select(u => u.EmployeeId!.Value)
            .Distinct()
            .ToListAsync();

    public async Task UpsertDeviceTokenAsync(long employeeId, string token, string platform, DateTime now)
    {
        var existing = await _context.DeviceTokens.FirstOrDefaultAsync(t => t.Token == token);
        if (existing is null)
        {
            await _context.DeviceTokens.AddAsync(new DeviceToken
            {
                EmployeeId = employeeId, Token = token, Platform = platform, CreatedAt = now, LastSeenAt = now
            });
        }
        else
        {
            // Cùng một máy đăng nhập người khác thì token chuyển sang người đó.
            existing.EmployeeId = employeeId;
            existing.Platform = platform;
            existing.LastSeenAt = now;
        }

        await _context.SaveChangesAsync();
    }

    public async Task RemoveDeviceTokenAsync(string token, long employeeId) =>
        await _context.DeviceTokens.Where(t => t.Token == token && t.EmployeeId == employeeId).ExecuteDeleteAsync();

    public async Task<List<string>> GetDeviceTokensAsync(long employeeId) =>
        await _context.DeviceTokens.Where(t => t.EmployeeId == employeeId).Select(t => t.Token).ToListAsync();

    public async Task RemoveDeviceTokensAsync(IReadOnlyCollection<string> tokens) =>
        await _context.DeviceTokens.Where(t => tokens.Contains(t.Token)).ExecuteDeleteAsync();

    public Task<int> SaveChangesAsync() => _context.SaveChangesAsync();
}
