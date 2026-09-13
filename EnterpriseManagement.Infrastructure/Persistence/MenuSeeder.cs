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

        new("ADMIN_USERS", "Quản lý tài khoản", "ShieldRegular", "/admin/users", 1, "page.admin.users", "ADMIN"),
        new("ADMIN_EMPLOYEES", "Quản lý nhân viên", "PeopleTeamRegular", "/admin/employees", 2, "page.admin.employees", "ADMIN"),
        new("ADMIN_DEPARTMENTS", "Phòng ban", "BuildingRegular", "/admin/departments", 3, "page.admin.departments", "ADMIN"),
        new("ADMIN_POSITIONS", "Chức vụ", "PersonRegular", "/admin/positions", 4, "page.admin.positions", "ADMIN"),
        new("ADMIN_CUSTOMERS", "Quản lý khách hàng", "PeopleTeamRegular", "/admin/customers", 5, "page.admin.customers", "ADMIN"),
        new("ADMIN_AUDIT", "Audit Log", "HistoryRegular", "/admin/audit", 6, "page.admin.audit", "ADMIN"),
        new("ADMIN_SYSTEM", "System Administration", "SettingsRegular", "/admin/system", 7, "page.admin.system", "ADMIN"),
    };

    // Idempotent: chỉ chèn những MenuCode chưa tồn tại, để khi thêm seed mới (vd MANAGER_AUDIT)
    // các DB đã chạy từ trước (Menus không rỗng) vẫn nhận được menu mới ở lần khởi động kế tiếp,
    // thay vì bị bỏ qua toàn bộ như trước (AnyAsync() chặn cả seed).
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        var now = DateTime.UtcNow;

        // Audit Log là trang dùng chung cho toàn hệ thống (chỉ 1 menu, route /admin/audit),
        // không phải trang riêng theo role. Bản seed trước có lúc tạo MANAGER_AUDIT (route
        // /manager/audit) như 1 menu riêng cho Manager — dọn lại 1 lần: gộp về đúng 1 menu,
        // Manager muốn xem chỉ cần được cấp permission "page.admin.audit" như Admin.
        await MergeManagerAuditMenuAsync(context, now);
        await SplitAdminOrganizationMenuAsync(context);

        var existingMenuCodes = (await context.Menus.Select(m => m.MenuCode).ToListAsync()).ToHashSet();
        var missingSeeds = Seeds.Where(s => !existingMenuCodes.Contains(s.MenuCode)).ToList();
        if (missingSeeds.Count > 0)
        {
            var roles = await context.Roles.ToDictionaryAsync(r => r.RoleCode);
            var permissionsByCode = await context.Permissions.ToDictionaryAsync(p => p.PermissionCode);

            foreach (var seed in missingSeeds)
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

            // Lưu trước để permission mới (nếu có) có Id thật, dùng để so trùng RolePermission bên dưới.
            await context.SaveChangesAsync();

            var existingRolePermissionKeys = (await context.RolePermissions
                    .Select(rp => new { rp.RoleId, rp.PermissionId })
                    .ToListAsync())
                .Select(x => (x.RoleId, x.PermissionId))
                .ToHashSet();

            foreach (var group in missingSeeds.GroupBy(s => s.RoleCode))
            {
                if (!roles.TryGetValue(group.Key, out var role)) continue;

                foreach (var seed in group)
                {
                    var permission = permissionsByCode[seed.PermissionCode];
                    if (existingRolePermissionKeys.Contains((role.Id, permission.Id))) continue;

                    context.RolePermissions.Add(new RolePermission
                    {
                        Role = role,
                        Permission = permission,
                        GrantedAt = now,
                        CreatedAt = now
                    });
                }
            }

            foreach (var seed in missingSeeds)
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

        // Manager cũng đi làm nên cần tự chấm công như Employee. Không tạo menu/route riêng —
        // tái dùng thẳng trang "Chấm công" của Employee (route /employee/attendance) bằng cách
        // cấp thêm permission "page.employee.attendance" cho MANAGER; nhờ MenuService.GetMineAsync
        // khớp menu theo BẤT KỲ permission nào trong MenuPermissions, menu EMPLOYEE_ATTENDANCE tự
        // xuất hiện trong /api/menus/mine của Manager mà không cần thêm MenuCode mới.
        await GrantManagerSelfAttendanceMenuAsync(context, now);
    }

    private static async Task GrantManagerSelfAttendanceMenuAsync(ApplicationDbContext context, DateTime now)
    {
        var managerRole = await context.Roles.FirstOrDefaultAsync(r => r.RoleCode == "MANAGER");
        var permission = await context.Permissions.FirstOrDefaultAsync(p => p.PermissionCode == "page.employee.attendance");
        if (managerRole is null || permission is null) return;

        var alreadyGranted = await context.RolePermissions
            .AnyAsync(rp => rp.RoleId == managerRole.Id && rp.PermissionId == permission.Id);
        if (alreadyGranted) return;

        context.RolePermissions.Add(new RolePermission
        {
            Role = managerRole,
            Permission = permission,
            GrantedAt = now,
            CreatedAt = now
        });
        await context.SaveChangesAsync();
    }

    // Xoá menu MANAGER_AUDIT (nếu có từ bước seed trước) và mọi permission "page.manager.audit"
    // đi kèm, đồng thời chuyển quyền xem audit của Manager (nếu đã được cấp) sang dùng chung
    // permission "page.admin.audit" của menu ADMIN_AUDIT — để chỉ còn đúng 1 Audit Log.
    private static async Task MergeManagerAuditMenuAsync(ApplicationDbContext context, DateTime now)
    {
        var menu = await context.Menus
            .Include(m => m.MenuPermissions)
            .ThenInclude(mp => mp.Permission)
            .FirstOrDefaultAsync(m => m.MenuCode == "MANAGER_AUDIT");
        if (menu is null) return;

        var linkedPermissions = menu.MenuPermissions.Select(mp => mp.Permission).ToList();
        var managerRole = await context.Roles.FirstOrDefaultAsync(r => r.RoleCode == "MANAGER");
        var sharedAuditPermission = await context.Permissions.FirstOrDefaultAsync(p => p.PermissionCode == "page.admin.audit");

        if (managerRole is not null && sharedAuditPermission is not null && linkedPermissions.Count > 0)
        {
            var alreadyHasShared = await context.RolePermissions
                .AnyAsync(rp => rp.RoleId == managerRole.Id && rp.PermissionId == sharedAuditPermission.Id);
            if (!alreadyHasShared)
            {
                context.RolePermissions.Add(new RolePermission
                {
                    Role = managerRole,
                    Permission = sharedAuditPermission,
                    GrantedAt = now,
                    CreatedAt = now
                });
            }
        }

        context.MenuPermissions.RemoveRange(menu.MenuPermissions);
        context.Menus.Remove(menu);
        await context.SaveChangesAsync();

        // permission "page.manager.audit" (tạo tạm ở bước trước) không còn menu nào dùng nữa -> xoá hẳn.
        // Không đụng "page.admin.audit" vì đó là permission dùng chung, ADMIN_AUDIT vẫn cần.
        foreach (var permission in linkedPermissions.Where(p => p.PermissionCode != "page.admin.audit"))
        {
            var stillReferenced = await context.MenuPermissions.AnyAsync(mp => mp.PermissionId == permission.Id);
            if (stillReferenced) continue;

            var grants = await context.RolePermissions.Where(rp => rp.PermissionId == permission.Id).ToListAsync();
            context.RolePermissions.RemoveRange(grants);
            context.Permissions.Remove(permission);
        }

        await context.SaveChangesAsync();
    }

    // "Phòng ban & chức vụ" (ADMIN_ORGANIZATION) tách thành 2 menu riêng — ADMIN_DEPARTMENTS
    // và ADMIN_POSITIONS — vì chức vụ giờ cần gán Role riêng (Position.RoleCode), không còn
    // hợp lý gộp chung 1 trang/1 permission với phòng ban. Dọn 1 lần: xoá menu cũ + permission
    // "page.admin.organization" đi kèm; 2 menu mới được seed lại bình thường qua vòng lặp
    // missingSeeds bên trên vì MenuCode của chúng chưa tồn tại.
    private static async Task SplitAdminOrganizationMenuAsync(ApplicationDbContext context)
    {
        var menu = await context.Menus
            .Include(m => m.MenuPermissions)
            .ThenInclude(mp => mp.Permission)
            .FirstOrDefaultAsync(m => m.MenuCode == "ADMIN_ORGANIZATION");
        if (menu is null) return;

        var linkedPermissions = menu.MenuPermissions.Select(mp => mp.Permission).ToList();

        context.MenuPermissions.RemoveRange(menu.MenuPermissions);
        context.Menus.Remove(menu);
        await context.SaveChangesAsync();

        foreach (var permission in linkedPermissions)
        {
            var stillReferenced = await context.MenuPermissions.AnyAsync(mp => mp.PermissionId == permission.Id);
            if (stillReferenced) continue;

            var grants = await context.RolePermissions.Where(rp => rp.PermissionId == permission.Id).ToListAsync();
            context.RolePermissions.RemoveRange(grants);
            context.Permissions.Remove(permission);
        }

        await context.SaveChangesAsync();
    }
}
