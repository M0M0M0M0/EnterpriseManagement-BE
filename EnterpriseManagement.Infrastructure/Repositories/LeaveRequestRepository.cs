using EnterpriseManagement.Application.Interfaces;
using EnterpriseManagement.Domain.Entities.Leave;
using EnterpriseManagement.Domain.Enums;
using EnterpriseManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseManagement.Infrastructure.Repositories;

public class LeaveRequestRepository : ILeaveRequestRepository
{
    private readonly ApplicationDbContext _context;

    public LeaveRequestRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<LeaveRequest>> GetApprovedUnpaidByEmployeeAndPeriodAsync(
        long employeeId, DateOnly periodStart, DateOnly periodEnd)
    {
        var periodStartDateTime = periodStart.ToDateTime(TimeOnly.MinValue);
        var periodEndDateTime = periodEnd.ToDateTime(TimeOnly.MaxValue);

        return await _context.LeaveRequests
            .Include(l => l.LeaveType)
            .Where(l => l.EmployeeId == employeeId
                && l.Status == LeaveRequestStatus.Approved
                && !l.LeaveType.IsPaid
                && l.StartDate <= periodEndDateTime
                && l.EndDate >= periodStartDateTime)
            .ToListAsync();
    }

    public async Task<LeaveRequest?> GetByIdAsync(long id) =>
        await _context.LeaveRequests
            .Include(l => l.LeaveType)
            .Include(l => l.Employee)
            .FirstOrDefaultAsync(l => l.Id == id);

    public async Task<IEnumerable<LeaveRequest>> GetPendingAsync() =>
        await _context.LeaveRequests
            .Include(l => l.LeaveType)
            .Include(l => l.Employee)
            .Where(l => l.Status == LeaveRequestStatus.Pending)
            .ToListAsync();

    public async Task<IEnumerable<LeaveRequest>> GetByEmployeeIdAsync(long employeeId) =>
        await _context.LeaveRequests
            .Include(l => l.LeaveType)
            .Include(l => l.Employee)
            .Include(l => l.Approver)
            .Where(l => l.EmployeeId == employeeId)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync();

    public async Task<IEnumerable<LeaveRequest>> GetAllAsync() =>
        await _context.LeaveRequests
            .Include(l => l.LeaveType)
            .Include(l => l.Employee)
            .Include(l => l.Approver)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync();

    public async Task AddAsync(LeaveRequest leaveRequest) =>
        await _context.LeaveRequests.AddAsync(leaveRequest);

    public Task<int> SaveChangesAsync() =>
        _context.SaveChangesAsync();
}
