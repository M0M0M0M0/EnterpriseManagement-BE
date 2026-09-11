using EnterpriseManagement.Domain.Entities.Identity;

namespace EnterpriseManagement.Application.Interfaces;

public interface IMenuRepository
{
    Task<Menu?> GetByCodeAsync(string menuCode);
    Task<IEnumerable<Menu>> GetAllAsync();
    Task AddAsync(Menu menu);
    Task<int> SaveChangesAsync();
}
