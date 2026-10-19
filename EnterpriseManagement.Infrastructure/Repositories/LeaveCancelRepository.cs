using EnterpriseManagement.Application.Interfaces;
using EnterpriseManagement.Domain.Entities.Leave;
using EnterpriseManagement.Domain.Enums;
using EnterpriseManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseManagement.Infrastructure.Repositories;

public class LeaveCancelRepository : ILeaveCancelRepository
{
    private readonly ApplicationDbContext _context;

    public LeaveCancelRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    private IQueryable<LeaveCancelRequest> WithDetails() =>
        _context.LeaveCancelRequests
            .Include(c => c.Employee)
            .Include(c => c.Manager)
            .Include(c => c.LeaveRequest).ThenInclude(l => l.LeaveType)
            .Include(c => c.LeaveRequest).ThenInclude(l => l.Employee);

    public Task<LeaveCancelRequest?> GetByIdAsync(long id) => WithDetails().FirstOrDefaultAsync(c => c.Id == id);

    public Task<List<LeaveCancelRequest>> GetByEmployeeAsync(long employeeId) =>
        WithDetails().Where(c => c.EmployeeId == employeeId).OrderByDescending(c => c.Id).ToListAsync();

    public Task<List<LeaveCancelRequest>> GetPendingAsync() =>
        WithDetails().Where(c => c.Status == LeaveCancelStatus.Pending).OrderByDescending(c => c.Id).ToListAsync();

    public Task<List<LeaveCancelRequest>> GetAllAsync() => WithDetails().OrderByDescending(c => c.Id).ToListAsync();

    public Task<bool> HasPendingAsync(long leaveRequestId) =>
        _context.LeaveCancelRequests.AnyAsync(c => c.LeaveRequestId == leaveRequestId && c.Status == LeaveCancelStatus.Pending);

    public async Task AddAsync(LeaveCancelRequest request) => await _context.LeaveCancelRequests.AddAsync(request);

    public Task<int> SaveChangesAsync() => _context.SaveChangesAsync();
}
