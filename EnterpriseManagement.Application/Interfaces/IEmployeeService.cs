using EnterpriseManagement.Application.DTOs;

namespace EnterpriseManagement.Application.Interfaces;

public interface IEmployeeService
{
    Task<IEnumerable<EmployeeDto>> GetAllAsync();
    Task<EmployeeDto?> GetByCodeAsync(string employeeCode);
    Task<EmployeeDto> CreateAsync(CreateEmployeeRequest request);
    Task<IEnumerable<EmployeeDto>> GetTeamAsync(string managerEmployeeCode);
    Task<IEnumerable<OrgTreeNodeDto>> GetOrgTreeAsync(string requesterEmployeeCode, bool isAdmin);
    Task<EmployeeDto> UpdateAsync(string employeeCode, UpdateEmployeeRequest request);
    Task<EmployeeDto> SetActiveAsync(string employeeCode, bool isActive);
}
