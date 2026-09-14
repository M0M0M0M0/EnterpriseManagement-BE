using EnterpriseManagement.Application.DTOs;

namespace EnterpriseManagement.Application.Interfaces;

public interface IAuditLogService
{
    // "Ghi thầm" — lỗi khi ghi log (nếu có) không được làm hỏng hành động nghiệp vụ chính,
    // nên không throw, xem AuditLogService.LogAsync.
    Task LogAsync(long? userId, string action, string module, long? entityId = null, string? ipAddress = null);

    Task<IEnumerable<AuditLogDto>> GetFilteredAsync(string? username, string? module, string? action, DateOnly? date);
}
