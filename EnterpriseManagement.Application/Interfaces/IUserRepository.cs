using EnterpriseManagement.Domain.Entities.Identity;

namespace EnterpriseManagement.Application.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetByEmployeeCodeAsync(string employeeCode);
    Task<IEnumerable<User>> GetAllAsync();
    Task AddAsync(User user);
    void RemoveUserRole(Domain.Entities.Identity.UserRole userRole);
    Task<int> SaveChangesAsync();
}
