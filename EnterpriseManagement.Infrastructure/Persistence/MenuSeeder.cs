using EnterpriseManagement.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseManagement.Infrastructure.Persistence;

// Seed Menu + permission "page.*" dùng riêng để gate hiển thị sidebar/route
// (navByRole trong AppShell.tsx cũ), để khi FE lấy menu động từ GET /api/menus/mine,
// hành vi giữ nguyên. Đây CHỈ là permission cấp trang (vào được trang hay không) —
// permission cho từng hành động thật bên trong trang (xem/duyệt/từ chối...) nằm ở
// ActionPermissionSeeder và được gán trực tiếp vào controller qua [RequirePermission],
// tách biệt hoàn toàn khỏi Menu để tránh 1 permission phải gánh cả 2 việc.
public static class MenuSeeder
{
    private record MenuSeed(string MenuCode, string MenuName, string Icon, string Route, int DisplayOrder, string PermissionCode, string RoleCode);

    private static readonly MenuSeed[] Seeds =
    {
        new("EMPLOYEE_DASHBOARD", "Dashboard", "HomeRegular", "/employee/dashboard", 1, "page.employee.dashboard", "EMPLOYEE"),
        new("EMPLOYEE_ATTENDANCE", "Chấm công", "ClockRegular", "/employee/attendance", 2, "page.employee.attendance", "EMPLOYEE"),
        new("EMPLOYEE_LEAVE", "Xin nghỉ phép", "CalendarRegular", "/employee/leave", 3, "page.employee.leave", "EMPLOYEE"),
        new("EMPLOYEE_SALES", "Sale & KPI của tôi", "ChartMultipleRegular", "/employee/sales", 4, "page.employee.sales", "EMPLOYEE"),
        new("EMPLOYEE_CUSTOMERS", "Khách hàng", "PeopleTeamRegular", "/employee/customers", 5, "page.employee.customers", "EMPLOYEE"),

        new("MANAGER_DASHBOARD", "Dashboard tổng quan", "HomeRegular", "/manager/dashboard", 1, "page.manager.dashboard", "MANAGER"),
        new("MANAGER_EMPLOYEES", "Quản lý nhân viên", "PeopleTeamRegular", "/manager/employees", 2, "page.manager.employees", "MANAGER"),
        new("MANAGER_ATTENDANCE", "Quản lý chấm công", "ClockRegular", "/manager/attendance", 3, "page.manager.attendance", "MANAGER"),
        new("MANAGER_LEAVE", "Duyệt đơn xin nghỉ", "DocumentBulletListRegular", "/manager/leave", 4, "page.manager.leave", "MANAGER"),
        new("MANAGER_SALES", "Quản lý KPI & Sale", "ChartMultipleRegular", "/manager/sales", 5, "page.manager.sales", "MANAGER"),
        new("MANAGER_CUSTOMERS", "Quản lý khách hàng", "PeopleTeamRegular", "/manager/customers", 6, "page.manager.customers", "MANAGER"),
        new("MANAGER_ORGANIZATION", "Phòng ban & chức vụ", "BuildingRegular", "/manager/organization", 7, "page.manager.organization", "MANAGER"),

        new("ADMIN_USERS", "User / Role / Permission", "ShieldRegular", "/admin/users", 1, "page.admin.users", "ADMIN"),
        new("ADMIN_EMPLOYEES", "Quản lý nhân viên", "PeopleTeamRegular", "/admin/employees", 2, "page.admin.employees", "ADMIN"),
        new("ADMIN_ORGANIZATION", "Phòng ban & chức vụ", "BuildingRegular", "/admin/organization", 3, "page.admin.organization", "ADMIN"),
        new("ADMIN_CUSTOMERS", "Quản lý khách hàng", "PeopleTeamRegular", "/admin/customers", 4, "page.admin.customers", "ADMIN"),
        new("ADMIN_AUDIT", "Audit Log", "HistoryRegular", "/admin/audit", 5, "page.admin.audit", "ADMIN"),
        new("ADMIN_SYSTEM", "System Administration", "SettingsRegular", "/admin/system", 6, "page.admin.system", "ADMIN"),
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
                Module = "page",
                Description = $"Quyền vào trang '{seed.MenuName}'.",
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
