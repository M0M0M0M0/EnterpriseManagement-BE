using EnterpriseManagement.Domain.Entities.Sales;

namespace EnterpriseManagement.Application.Interfaces;

public interface IKpiPlanRepository
{
    Task<KpiPlan?> GetByIdAsync(long id);
    Task<IEnumerable<KpiPlan>> GetAllAsync();
    Task AddAsync(KpiPlan plan);
    Task<int> SaveChangesAsync();
}
