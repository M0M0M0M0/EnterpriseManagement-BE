using EnterpriseManagement.Domain.Entities.HR;

namespace EnterpriseManagement.Application.Interfaces;

public interface IEmployeeRepository
{
    Task<Employee?> GetByIdAsync(long id);
    Task<IEnumerable<Employee>> GetAllAsync();
    Task<Employee?> GetByEmployeeCodeAsync(string employeeCode);
    Task<Employee?> GetByEmailAsync(string email);
    Task<IEnumerable<Employee>> GetByManagerIdAsync(long managerId);
    Task AddAsync(Employee employee);
    void Update(Employee employee);
    void Delete(Employee employee);
    Task<int> SaveChangesAsync();
}
