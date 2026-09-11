using EnterpriseManagement.Application.DTOs;
using EnterpriseManagement.Application.Interfaces;
using EnterpriseManagement.Domain.Entities.Identity;

namespace EnterpriseManagement.Application.Services;

public class RoleService : IRoleService
{
    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionRepository _permissionRepository;

    public RoleService(IRoleRepository roleRepository, IPermissionRepository permissionRepository)
    {
        _roleRepository = roleRepository;
        _permissionRepository = permissionRepository;
    }

    public async Task<IEnumerable<RoleDto>> GetAllAsync()
    {
        var roles = await _roleRepository.GetAllAsync();
        return roles.Select(ToDto);
    }

    public async Task<RoleDto> CreateAsync(CreateRoleRequest request)
    {
        var existing = await _roleRepository.GetByCodeAsync(request.RoleCode);
        if (existing is not null)
        {
            throw new InvalidOperationException($"Role code '{request.RoleCode}' already exists.");
        }

        var permissions = await ResolvePermissionsAsync(request.Permissions);

        var role = new Role
        {
            RoleCode = request.RoleCode,
            RoleName = request.RoleName,
            Description = request.Description,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        role.RolePermissions = permissions
            .Select(p => new RolePermission { Permission = p, GrantedAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow })
            .ToList();

        await _roleRepository.AddAsync(role);
        await _roleRepository.SaveChangesAsync();

        var created = await _roleRepository.GetByCodeAsync(role.RoleCode);
        return ToDto(created!);
    }

    public async Task<RoleDto> UpdateAsync(string roleCode, UpdateRoleRequest request)
    {
        var role = await _roleRepository.GetByCodeAsync(roleCode)
            ?? throw new InvalidOperationException($"Role code '{roleCode}' not found.");

        var permissions = await ResolvePermissionsAsync(request.Permissions);

        role.RoleName = request.RoleName;
        role.Description = request.Description;
        role.UpdatedAt = DateTime.UtcNow;
        role.RolePermissions.Clear();
        foreach (var permission in permissions)
        {
            role.RolePermissions.Add(new RolePermission
            {
                Role = role,
                Permission = permission,
                GrantedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _roleRepository.SaveChangesAsync();

        var updated = await _roleRepository.GetByCodeAsync(roleCode);
        return ToDto(updated!);
    }

    public async Task<RoleDto> SetActiveAsync(string roleCode, bool isActive)
    {
        var role = await _roleRepository.GetByCodeAsync(roleCode)
            ?? throw new InvalidOperationException($"Role code '{roleCode}' not found.");

        role.IsActive = isActive;
        role.UpdatedAt = DateTime.UtcNow;

        await _roleRepository.SaveChangesAsync();

        return ToDto(role);
    }

    private async Task<List<Permission>> ResolvePermissionsAsync(IEnumerable<string> permissionCodes)
    {
        var codes = permissionCodes.Distinct().ToList();
        if (codes.Count == 0) return new List<Permission>();

        var permissions = (await _permissionRepository.GetByCodesAsync(codes)).ToList();
        var missing = codes.Except(permissions.Select(p => p.PermissionCode)).ToList();
        if (missing.Count > 0)
        {
            throw new InvalidOperationException($"Permission code(s) not found: {string.Join(", ", missing)}.");
        }

        return permissions;
    }

    private static RoleDto ToDto(Role role) => new()
    {
        RoleCode = role.RoleCode,
        RoleName = role.RoleName,
        Description = role.Description,
        Permissions = role.RolePermissions.Select(rp => rp.Permission.PermissionCode).ToList(),
        IsActive = role.IsActive
    };
}
