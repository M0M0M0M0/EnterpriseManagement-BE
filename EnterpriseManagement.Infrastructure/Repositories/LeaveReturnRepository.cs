using EnterpriseManagement.Application.Interfaces;
using EnterpriseManagement.Domain.Entities.Leave;
using EnterpriseManagement.Domain.Enums;
using EnterpriseManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseManagement.Infrastructure.Repositories;

public class LeaveReturnRepository : ILeaveReturnRepository
{
    private readonly ApplicationDbContext _context;

    public LeaveReturnRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    private IQueryable<LeaveReturnRequest> Query() => _context.LeaveReturnRequests
        .Include(r => r.Employee)
        .Include(r => r.Manager)
        .Include(r => r.Processor)
        .Include(r => r.Sessions).ThenInclude(s => s.LeaveRequest).ThenInclude(l => l.LeaveType);

    public async Task<LeaveReturnRequest?> GetByIdAsync(long id) =>
        await Query().FirstOrDefaultAsync(r => r.Id == id);

    public async Task<IEnumerable<LeaveReturnRequest>> GetByEmployeeAsync(long employeeId) =>
        await Query().Where(r => r.EmployeeId == employeeId).OrderByDescending(r => r.Id).ToListAsync();

    public async Task<IEnumerable<LeaveReturnRequest>> GetByStatusAsync(ReturnRequestStatus status) =>
        await Query().Where(r => r.Status == status).OrderBy(r => r.Id).ToListAsync();

    public async Task<bool> HasOpenRequestAsync(long employeeId) =>
        await _context.LeaveReturnRequests.AnyAsync(r => r.EmployeeId == employeeId
            && (r.Status == ReturnRequestStatus.PendingManager || r.Status == ReturnRequestStatus.PendingHr));

    public async Task<IEnumerable<LeaveReturnSession>> GetActiveSessionsAsync(long employeeId, long leaveTypeId) =>
        await _context.LeaveReturnSessions
            .Include(s => s.LeaveRequest)
            .Where(s => s.State == ReturnSessionState.Active
                && s.LeaveRequest.EmployeeId == employeeId
                && s.LeaveRequest.LeaveTypeId == leaveTypeId
                && s.LeaveRequest.Status == LeaveRequestStatus.Approved)
            .ToListAsync();

    public async Task<IEnumerable<LeaveReturnSession>> GetActiveByLeaveRequestIdsAsync(IReadOnlyCollection<long> leaveRequestIds) =>
        await _context.LeaveReturnSessions
            .Where(s => s.State == ReturnSessionState.Active && leaveRequestIds.Contains(s.LeaveRequestId))
            .ToListAsync();

    public async Task AddAsync(LeaveReturnRequest request) =>
        await _context.LeaveReturnRequests.AddAsync(request);

    public Task<int> SaveChangesAsync() => _context.SaveChangesAsync();
}
