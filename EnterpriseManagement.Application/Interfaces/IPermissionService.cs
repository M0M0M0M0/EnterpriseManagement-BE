using EnterpriseManagement.Application.DTOs;

namespace EnterpriseManagement.Application.Interfaces;

public interface IPermissionService
{
    Task<IEnumerable<PermissionDto>> GetAllAsync();
    Task<PermissionDto> CreateAsync(CreatePermissionRequest request);
    Task<PermissionDto> UpdateAsync(string permissionCode, UpdatePermissionRequest request);
    Task<PermissionDto> SetActiveAsync(string permissionCode, bool isActive);
}
