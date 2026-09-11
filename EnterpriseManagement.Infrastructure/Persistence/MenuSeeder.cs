using EnterpriseManagement.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseManagement.Infrastructure.Persistence;

// Seed Permission + Menu + RolePermission khớp 1-1 với sidebar tĩnh hiện tại của FE
// (navByRole trong AppShell.tsx), để khi FE chuyển sang lấy menu động từ
// GET /api/menus/mine, hành vi vẫn y hệt hôm nay — Admin có thể chỉnh sửa tiếp qua UI
// Role/Permission/Menu thay vì phải sửa code.
public static class MenuSeeder
{
    private record MenuSeed(string MenuCode, string MenuName, string Icon, string Route, int DisplayOrder, string PermissionCode, string RoleCode);

    private static readonly MenuSeed[] Seeds =
    {
        new("EMPLOYEE_DASHBOARD", "Dashboard", "HomeRegular", "/employee/dashboard", 1, "menu.employee.dashboard", "EMPLOYEE"),
        new("EMPLOYEE_ATTENDANCE", "Chấm công", "ClockRegular", "/employee/attendance", 2, "menu.employee.attendance", "EMPLOYEE"),
        new("EMPLOYEE_LEAVE", "Xin nghỉ phép", "CalendarRegular", "/employee/leave", 3, "menu.employee.leave", "EMPLOYEE"),
        new("EMPLOYEE_SALES", "Sale & KPI của tôi", "ChartMultipleRegular", "/employee/sales", 4, "menu.employee.sales", "EMPLOYEE"),
        new("EMPLOYEE_CUSTOMERS", "Khách hàng", "PeopleTeamRegular", "/employee/customers", 5, "menu.employee.customers", "EMPLOYEE"),

        new("MANAGER_DASHBOARD", "Dashboard tổng quan", "HomeRegular", "/manager/dashboard", 1, "menu.manager.dashboard", "MANAGER"),
        new("MANAGER_EMPLOYEES", "Quản lý nhân viên", "PeopleTeamRegular", "/manager/employees", 2, "menu.manager.employees", "MANAGER"),
        new("MANAGER_ATTENDANCE", "Quản lý chấm công", "ClockRegular", "/manager/attendance", 3, "menu.manager.attendance", "MANAGER"),
        new("MANAGER_LEAVE", "Duyệt đơn xin nghỉ", "DocumentBulletListRegular", "/manager/leave", 4, "menu.manager.leave", "MANAGER"),
        new("MANAGER_SALES", "Quản lý KPI & Sale", "ChartMultipleRegular", "/manager/sales", 5, "menu.manager.sales", "MANAGER"),
        new("MANAGER_CUSTOMERS", "Quản lý khách hàng", "PeopleTeamRegular", "/manager/customers", 6, "menu.manager.customers", "MANAGER"),
        new("MANAGER_ORGANIZATION", "Phòng ban & chức vụ", "BuildingRegular", "/manager/organization", 7, "menu.manager.organization", "MANAGER"),

        new("ADMIN_USERS", "User / Role / Permission", "ShieldRegular", "/admin/users", 1, "menu.admin.users", "ADMIN"),
        new("ADMIN_EMPLOYEES", "Quản lý nhân viên", "PeopleTeamRegular", "/admin/employees", 2, "menu.admin.employees", "ADMIN"),
        new("ADMIN_ORGANIZATION", "Phòng ban & chức vụ", "BuildingRegular", "/admin/organization", 3, "menu.admin.organization", "ADMIN"),
        new("ADMIN_CUSTOMERS", "Quản lý khách hàng", "PeopleTeamRegular", "/admin/customers", 4, "menu.admin.customers", "ADMIN"),
        new("ADMIN_AUDIT", "Audit Log", "HistoryRegular", "/admin/audit", 5, "menu.admin.audit", "ADMIN"),
        new("ADMIN_SYSTEM", "System Administration", "SettingsRegular", "/admin/system", 6, "menu.admin.system", "ADMIN"),
    };

    public static async Task SeedAsync(ApplicationDbContext context)
    {
        if (await context.Menus.AnyAsync())
        {
            return;
        }

        var roles = await context.Roles.ToDictionaryAsync(r => r.RoleCode);
        var now = DateTime.UtcNow;

        var permissionsByCode = new Dictionary<string, Permission>();
        foreach (var seed in Seeds)
        {
            if (permissionsByCode.ContainsKey(seed.PermissionCode)) continue;

            var permission = new Permission
            {
                PermissionCode = seed.PermissionCode,
                PermissionName = seed.MenuName,
                Module = seed.RoleCode.ToLowerInvariant(),
                Description = $"Quyền xem menu '{seed.MenuName}'.",
                IsActive = true,
                CreatedAt = now
            };
            permissionsByCode[seed.PermissionCode] = permission;
            context.Permissions.Add(permission);
        }

        foreach (var group in Seeds.GroupBy(s => s.RoleCode))
        {
            if (!roles.TryGetValue(group.Key, out var role)) continue;

            foreach (var seed in group)
            {
                context.RolePermissions.Add(new RolePermission
                {
                    Role = role,
                    Permission = permissionsByCode[seed.PermissionCode],
                    GrantedAt = now,
                    CreatedAt = now
                });
            }
        }

        foreach (var seed in Seeds)
        {
            var menu = new Menu
            {
                MenuCode = seed.MenuCode,
                MenuName = seed.MenuName,
                Icon = seed.Icon,
                Route = seed.Route,
                DisplayOrder = seed.DisplayOrder,
                IsVisible = true,
                IsActive = true,
                CreatedAt = now
            };
            menu.MenuPermissions.Add(new MenuPermission { Menu = menu, Permission = permissionsByCode[seed.PermissionCode] });
            context.Menus.Add(menu);
        }

        await context.SaveChangesAsync();
    }
}
