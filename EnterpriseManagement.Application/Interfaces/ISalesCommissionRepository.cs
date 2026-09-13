using EnterpriseManagement.Domain.Entities.Sales;

namespace EnterpriseManagement.Application.Interfaces;

public interface ISalesCommissionRepository
{
    Task<SalesCommission?> GetByIdAsync(long id);
    Task<IEnumerable<SalesCommission>> GetByEmployeeIdAsync(long employeeId);
    Task<IEnumerable<SalesCommission>> GetPendingAsync();
    Task<IEnumerable<SalesCommission>> GetAllAsync();
    Task AddAsync(SalesCommission commission);
    Task<int> SaveChangesAsync();
}
