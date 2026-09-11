using EnterpriseManagement.Domain.Entities.Identity;

namespace EnterpriseManagement.Application.Interfaces;

public interface IRoleRepository
{
    Task<Role?> GetByCodeAsync(string roleCode);
    Task<IEnumerable<Role>> GetAllAsync();
    Task AddAsync(Role role);
    Task<int> SaveChangesAsync();
}
