using EnterpriseManagement.Application.Common;
using EnterpriseManagement.Application.DTOs;
using EnterpriseManagement.Application.Interfaces;
using EnterpriseManagement.Domain.Entities.Auditing;

namespace EnterpriseManagement.Application.Services;

public class AuditLogService : IAuditLogService
{
    private readonly IAuditLogRepository _auditLogRepository;

    public AuditLogService(IAuditLogRepository auditLogRepository)
    {
        _auditLogRepository = auditLogRepository;
    }

    // Được gọi thêm vào SAU hành động nghiệp vụ chính (đã SaveChanges thành công) ở các service
    // khác — cố tình nuốt lỗi thay vì throw, để 1 sự cố ghi audit log (vd mất kết nối DB đúng
    // lúc đó) không bao giờ làm hỏng hay rollback hành động thật đã thành công.
    public async Task LogAsync(long? userId, string action, string module, long? entityId = null, string? ipAddress = null)
    {
        try
        {
            var log = new AuditLog
            {
                UserId = userId,
                Action = action,
                EntityName = module,
                EntityId = entityId,
                IpAddress = ipAddress,
                CreatedAt = VietnamClock.Now
            };

            await _auditLogRepository.AddAsync(log);
            await _auditLogRepository.SaveChangesAsync();
        }
        catch
        {
            // Nuốt lỗi có chủ đích — xem ghi chú trên LogAsync.
        }
    }

    public async Task<IEnumerable<AuditLogDto>> GetFilteredAsync(string? username, string? module, string? action, DateOnly? date)
    {
        var logs = await _auditLogRepository.GetFilteredAsync(username, module, action, date);
        return logs.Select(ToDto);
    }

    private static AuditLogDto ToDto(AuditLog log) => new()
    {
        Id = log.Id,
        CreatedAt = log.CreatedAt,
        Username = log.User?.Username,
        Module = log.EntityName,
        Action = log.Action,
        Target = log.EntityId.HasValue ? $"{log.EntityName} #{log.EntityId}" : null,
        IpAddress = log.IpAddress
    };
}
