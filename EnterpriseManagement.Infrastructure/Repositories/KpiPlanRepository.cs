using EnterpriseManagement.Application.Interfaces;
using EnterpriseManagement.Domain.Entities.Sales;
using EnterpriseManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseManagement.Infrastructure.Repositories;

public class KpiPlanRepository : IKpiPlanRepository
{
    private readonly ApplicationDbContext _context;

    public KpiPlanRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<KpiPlan?> GetByIdAsync(long id) =>
        await _context.KpiPlans
            .Include(p => p.Levels)
            .Include(p => p.Employees)
            .FirstOrDefaultAsync(p => p.Id == id);

    public async Task<IEnumerable<KpiPlan>> GetAllAsync() =>
        await _context.KpiPlans
            .Include(p => p.Levels)
            .Include(p => p.Employees)
            .OrderBy(p => p.PlanName)
            .ToListAsync();

    public async Task AddAsync(KpiPlan plan) =>
        await _context.KpiPlans.AddAsync(plan);

    public Task<int> SaveChangesAsync() =>
        _context.SaveChangesAsync();
}
