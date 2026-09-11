using EnterpriseManagement.Domain.Entities.Identity;

namespace EnterpriseManagement.Application.Interfaces;

public interface IPermissionRepository
{
    Task<Permission?> GetByCodeAsync(string permissionCode);
    Task<IEnumerable<Permission>> GetAllAsync();
    Task<IEnumerable<Permission>> GetByCodesAsync(IEnumerable<string> permissionCodes);
    Task AddAsync(Permission permission);
    Task<int> SaveChangesAsync();
}
