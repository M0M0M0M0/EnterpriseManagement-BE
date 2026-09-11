using EnterpriseManagement.Application.DTOs;
using EnterpriseManagement.Application.Interfaces;
using EnterpriseManagement.Domain.Entities.Identity;

namespace EnterpriseManagement.Application.Services;

public class MenuService : IMenuService
{
    private readonly IMenuRepository _menuRepository;
    private readonly IPermissionRepository _permissionRepository;

    public MenuService(IMenuRepository menuRepository, IPermissionRepository permissionRepository)
    {
        _menuRepository = menuRepository;
        _permissionRepository = permissionRepository;
    }

    public async Task<IEnumerable<MenuDto>> GetAllAsync()
    {
        var menus = await _menuRepository.GetAllAsync();
        return menus.Select(ToDto);
    }

    public async Task<IEnumerable<MenuDto>> GetMineAsync(IEnumerable<string> userPermissions)
    {
        var permissionSet = userPermissions.ToHashSet();
        var menus = await _menuRepository.GetAllAsync();

        // Menu không gán permission nào thì coi như mở cho mọi tài khoản đã đăng nhập;
        // menu có gán permission thì chỉ hiện khi user có ít nhất 1 permission khớp.
        return menus
            .Where(m => m.IsActive && m.IsVisible)
            .Where(m => m.MenuPermissions.Count == 0
                || m.MenuPermissions.Any(mp => permissionSet.Contains(mp.Permission.PermissionCode)))
            .Select(ToDto);
    }

    public async Task<MenuDto> CreateAsync(CreateMenuRequest request)
    {
        var existing = await _menuRepository.GetByCodeAsync(request.MenuCode);
        if (existing is not null)
        {
            throw new InvalidOperationException($"Menu code '{request.MenuCode}' already exists.");
        }

        var permissions = await ResolvePermissionsAsync(request.Permissions);

        var menu = new Menu
        {
            MenuCode = request.MenuCode,
            MenuName = request.MenuName,
            Icon = request.Icon,
            Route = request.Route,
            DisplayOrder = request.DisplayOrder,
            IsVisible = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        menu.MenuPermissions = permissions.Select(p => new MenuPermission { Permission = p }).ToList();

        await _menuRepository.AddAsync(menu);
        await _menuRepository.SaveChangesAsync();

        var created = await _menuRepository.GetByCodeAsync(menu.MenuCode);
        return ToDto(created!);
    }

    public async Task<MenuDto> UpdateAsync(string menuCode, UpdateMenuRequest request)
    {
        var menu = await _menuRepository.GetByCodeAsync(menuCode)
            ?? throw new InvalidOperationException($"Menu code '{menuCode}' not found.");

        var permissions = await ResolvePermissionsAsync(request.Permissions);

        menu.MenuName = request.MenuName;
        menu.Icon = request.Icon;
        menu.Route = request.Route;
        menu.DisplayOrder = request.DisplayOrder;
        menu.UpdatedAt = DateTime.UtcNow;
        menu.MenuPermissions.Clear();
        foreach (var permission in permissions)
        {
            menu.MenuPermissions.Add(new MenuPermission { Menu = menu, Permission = permission });
        }

        await _menuRepository.SaveChangesAsync();

        var updated = await _menuRepository.GetByCodeAsync(menuCode);
        return ToDto(updated!);
    }

    public async Task<MenuDto> SetVisibleAsync(string menuCode, bool isVisible)
    {
        var menu = await _menuRepository.GetByCodeAsync(menuCode)
            ?? throw new InvalidOperationException($"Menu code '{menuCode}' not found.");

        menu.IsVisible = isVisible;
        menu.UpdatedAt = DateTime.UtcNow;

        await _menuRepository.SaveChangesAsync();

        return ToDto(menu);
    }

    public async Task<MenuDto> SetActiveAsync(string menuCode, bool isActive)
    {
        var menu = await _menuRepository.GetByCodeAsync(menuCode)
            ?? throw new InvalidOperationException($"Menu code '{menuCode}' not found.");

        menu.IsActive = isActive;
        menu.UpdatedAt = DateTime.UtcNow;

        await _menuRepository.SaveChangesAsync();

        return ToDto(menu);
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

    private static MenuDto ToDto(Menu menu) => new()
    {
        MenuCode = menu.MenuCode,
        MenuName = menu.MenuName,
        Icon = menu.Icon,
        Route = menu.Route ?? string.Empty,
        DisplayOrder = menu.DisplayOrder,
        Permissions = menu.MenuPermissions.Select(mp => mp.Permission.PermissionCode).ToList(),
        IsVisible = menu.IsVisible,
        IsActive = menu.IsActive
    };
}
