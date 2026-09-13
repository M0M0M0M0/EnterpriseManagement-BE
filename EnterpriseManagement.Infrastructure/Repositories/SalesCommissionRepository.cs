using EnterpriseManagement.Application.Interfaces;
using EnterpriseManagement.Domain.Entities.Sales;
using EnterpriseManagement.Domain.Enums;
using EnterpriseManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseManagement.Infrastructure.Repositories;

public class SalesCommissionRepository : ISalesCommissionRepository
{
    private readonly ApplicationDbContext _context;

    public SalesCommissionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SalesCommission?> GetByIdAsync(long id) =>
        await _context.SalesCommissions
            .Include(c => c.Employee)
            .Include(c => c.KpiLevel).ThenInclude(l => l.KpiPlan)
            .Include(c => c.Approver)
            .FirstOrDefaultAsync(c => c.Id == id);

    public async Task<IEnumerable<SalesCommission>> GetByEmployeeIdAsync(long employeeId) =>
        await _context.SalesCommissions
            .Include(c => c.Employee)
            .Include(c => c.KpiLevel).ThenInclude(l => l.KpiPlan)
            .Include(c => c.Approver)
            .Where(c => c.EmployeeId == employeeId)
            .OrderByDescending(c => c.PeriodStartDate)
            .ToListAsync();

    public async Task<IEnumerable<SalesCommission>> GetPendingAsync() =>
        await _context.SalesCommissions
            .Include(c => c.Employee)
            .Include(c => c.KpiLevel).ThenInclude(l => l.KpiPlan)
            .Where(c => c.Status == CommissionStatus.Pending)
            .OrderByDescending(c => c.PeriodStartDate)
            .ToListAsync();

    public async Task<IEnumerable<SalesCommission>> GetAllAsync() =>
        await _context.SalesCommissions
            .Include(c => c.Employee)
            .Include(c => c.KpiLevel).ThenInclude(l => l.KpiPlan)
            .Include(c => c.Approver)
            .OrderByDescending(c => c.PeriodStartDate)
            .ToListAsync();

    public async Task AddAsync(SalesCommission commission) =>
        await _context.SalesCommissions.AddAsync(commission);

    public Task<int> SaveChangesAsync() =>
        _context.SaveChangesAsync();
}
