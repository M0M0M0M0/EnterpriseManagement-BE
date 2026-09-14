using EnterpriseManagement.Application.Interfaces;
using EnterpriseManagement.Domain.Entities.Auditing;
using EnterpriseManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseManagement.Infrastructure.Repositories;

public class AuditLogRepository : IAuditLogRepository
{
    private readonly ApplicationDbContext _context;

    public AuditLogRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(AuditLog log) =>
        await _context.AuditLogs.AddAsync(log);

    public Task<int> SaveChangesAsync() =>
        _context.SaveChangesAsync();

    public async Task<IEnumerable<AuditLog>> GetFilteredAsync(string? username, string? module, string? action, DateOnly? date)
    {
        var query = _context.AuditLogs.Include(x => x.User).AsQueryable();

        if (!string.IsNullOrWhiteSpace(username))
        {
            query = query.Where(x => x.User != null && x.User.Username.Contains(username));
        }
        if (!string.IsNullOrWhiteSpace(module))
        {
            query = query.Where(x => x.EntityName.Contains(module));
        }
        if (!string.IsNullOrWhiteSpace(action))
        {
            query = query.Where(x => x.Action.Contains(action));
        }
        if (date.HasValue)
        {
            var dayStart = date.Value.ToDateTime(TimeOnly.MinValue);
            var dayEnd = date.Value.ToDateTime(TimeOnly.MaxValue);
            query = query.Where(x => x.CreatedAt >= dayStart && x.CreatedAt <= dayEnd);
        }

        return await query.OrderByDescending(x => x.CreatedAt).Take(500).ToListAsync();
    }
}
