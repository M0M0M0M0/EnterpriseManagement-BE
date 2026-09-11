using EnterpriseManagement.Application.DTOs;
using EnterpriseManagement.Application.Interfaces;
using EnterpriseManagement.Domain.Entities.Identity;

namespace EnterpriseManagement.Application.Services;

public class PermissionService : IPermissionService
{
    private readonly IPermissionRepository _permissionRepository;

    public PermissionService(IPermissionRepository permissionRepository)
    {
        _permissionRepository = permissionRepository;
    }

    public async Task<IEnumerable<PermissionDto>> GetAllAsync()
    {
        var permissions = await _permissionRepository.GetAllAsync();
        return permissions.Select(ToDto);
    }

    public async Task<PermissionDto> CreateAsync(CreatePermissionRequest request)
    {
        var existing = await _permissionRepository.GetByCodeAsync(request.PermissionCode);
        if (existing is not null)
        {
            throw new InvalidOperationException($"Permission code '{request.PermissionCode}' already exists.");
        }

        var permission = new Permission
        {
            PermissionCode = request.PermissionCode,
            PermissionName = request.PermissionName,
            Module = request.Module,
            Description = request.Description,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _permissionRepository.AddAsync(permission);
        await _permissionRepository.SaveChangesAsync();

        var created = await _permissionRepository.GetByCodeAsync(permission.PermissionCode);
        return ToDto(created!);
    }

    public async Task<PermissionDto> UpdateAsync(string permissionCode, UpdatePermissionRequest request)
    {
        var permission = await _permissionRepository.GetByCodeAsync(permissionCode)
            ?? throw new InvalidOperationException($"Permission code '{permissionCode}' not found.");

        permission.PermissionName = request.PermissionName;
        permission.Module = request.Module;
        permission.Description = request.Description;

        await _permissionRepository.SaveChangesAsync();

        return ToDto(permission);
    }

    public async Task<PermissionDto> SetActiveAsync(string permissionCode, bool isActive)
    {
        var permission = await _permissionRepository.GetByCodeAsync(permissionCode)
            ?? throw new InvalidOperationException($"Permission code '{permissionCode}' not found.");

        permission.IsActive = isActive;

        await _permissionRepository.SaveChangesAsync();

        return ToDto(permission);
    }

    private static PermissionDto ToDto(Permission permission) => new()
    {
        PermissionCode = permission.PermissionCode,
        PermissionName = permission.PermissionName,
        Module = permission.Module,
        Description = permission.Description,
        IsActive = permission.IsActive
    };
}
