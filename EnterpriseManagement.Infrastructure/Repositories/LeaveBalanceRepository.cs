using EnterpriseManagement.Application.Interfaces;
using EnterpriseManagement.Domain.Entities.Leave;
using EnterpriseManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseManagement.Infrastructure.Repositories;

public class LeaveBalanceRepository : ILeaveBalanceRepository
{
    private readonly ApplicationDbContext _context;

    public LeaveBalanceRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<LeaveBalance?> GetAsync(long employeeId, long leaveTypeId, int year, int? month) =>
        await _context.LeaveBalances
            .Include(b => b.LeaveType)
            .FirstOrDefaultAsync(b =>
                b.EmployeeId == employeeId &&
                b.LeaveTypeId == leaveTypeId &&
                b.Year == year &&
                b.Month == month);

    public async Task<IEnumerable<LeaveBalance>> GetByEmployeeAndYearAsync(long employeeId, int year) =>
        await _context.LeaveBalances
            .Include(b => b.LeaveType)
            .Where(b => b.EmployeeId == employeeId && b.Year == year)
            .ToListAsync();

    public async Task AddAsync(LeaveBalance balance) =>
        await _context.LeaveBalances.AddAsync(balance);

    public Task<int> SaveChangesAsync() =>
        _context.SaveChangesAsync();
}
