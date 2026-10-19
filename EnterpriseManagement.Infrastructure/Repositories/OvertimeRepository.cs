using EnterpriseManagement.Application.Interfaces;
using EnterpriseManagement.Domain.Entities.Attendance;
using EnterpriseManagement.Domain.Enums;
using EnterpriseManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseManagement.Infrastructure.Repositories;

public class OvertimeRepository : IOvertimeRepository
{
    private readonly ApplicationDbContext _context;

    public OvertimeRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    private IQueryable<OvertimeRequest> WithPeople() =>
        _context.OvertimeRequests.Include(o => o.Employee).Include(o => o.Approver);

    public Task<OvertimeRequest?> GetByIdAsync(long id) => WithPeople().FirstOrDefaultAsync(o => o.Id == id);

    public Task<List<OvertimeRequest>> GetByEmployeeAsync(long employeeId) =>
        WithPeople().Where(o => o.EmployeeId == employeeId).OrderByDescending(o => o.CreatedAt).ToListAsync();

    public Task<List<OvertimeRequest>> GetPendingAsync() =>
        WithPeople().Where(o => o.Status == OvertimeStatus.Pending).OrderByDescending(o => o.CreatedAt).ToListAsync();

    public Task<List<OvertimeRequest>> GetAllAsync() => WithPeople().OrderByDescending(o => o.CreatedAt).ToListAsync();

    public Task<List<OvertimeRequest>> GetActiveByEmployeeAndDateAsync(long employeeId, DateOnly date) =>
        _context.OvertimeRequests
            .Where(o => o.EmployeeId == employeeId && o.Date == date
                && (o.Status == OvertimeStatus.Pending || o.Status == OvertimeStatus.Approved))
            .ToListAsync();

    public async Task<int> SumActiveMinutesAsync(long employeeId, DateOnly from, DateOnly to, long? excludeId = null) =>
        await _context.OvertimeRequests
            .Where(o => o.EmployeeId == employeeId && o.Date >= from && o.Date <= to
                && (o.Status == OvertimeStatus.Pending || o.Status == OvertimeStatus.Approved)
                && (excludeId == null || o.Id != excludeId))
            .SumAsync(o => o.Minutes);

    public async Task AddAsync(OvertimeRequest request) => await _context.OvertimeRequests.AddAsync(request);

    public Task<int> SaveChangesAsync() => _context.SaveChangesAsync();
}
