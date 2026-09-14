using EnterpriseManagement.Domain.Entities.Auditing;

namespace EnterpriseManagement.Application.Interfaces;

public interface IAuditLogRepository
{
    Task AddAsync(AuditLog log);
    Task<int> SaveChangesAsync();

    Task<IEnumerable<AuditLog>> GetFilteredAsync(string? username, string? module, string? action, DateOnly? date);
}
