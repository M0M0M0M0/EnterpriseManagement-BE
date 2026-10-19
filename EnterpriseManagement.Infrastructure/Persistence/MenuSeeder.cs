using EnterpriseManagement.Application.Common;
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
        new("EMPLOYEE_DASHBOARD", "Tổng quan của tôi", "HomeRegular", "/employee/dashboard", 1, "page.employee.dashboard", "EMPLOYEE"),
        new("EMPLOYEE_ATTENDANCE", "Chấm công của tôi", "ClockRegular", "/employee/attendance", 5, "page.employee.attendance", "EMPLOYEE"),
        new("EMPLOYEE_LEAVE", "Xin nghỉ phép", "CalendarRegular", "/employee/leave", 6, "page.employee.leave", "EMPLOYEE"),
        new("EMPLOYEE_OVERTIME", "Làm thêm giờ", "ClockRegular", "/employee/overtime", 7, "page.employee.overtime", "EMPLOYEE"),

        new("MANAGER_DASHBOARD", "Tổng quan phòng ban", "HomeRegular", "/manager/dashboard", 1, "page.manager.dashboard", "MANAGER"),
        new("MANAGER_LEAVE", "Duyệt đơn từ", "DocumentBulletListRegular", "/manager/leave", 2, "page.manager.leave", "MANAGER"),
        new("MANAGER_ATTENDANCE", "Quản lý chấm công", "ClockRegular", "/manager/attendance", 3, "page.manager.attendance", "MANAGER"),
        new("MANAGER_TIMESHEET", "Bảng công tháng", "TableRegular", "/manager/timesheet", 4, "page.manager.timesheet", "MANAGER"),
        new("MANAGER_EMPLOYEES", "Quản lý nhân viên", "PeopleTeamRegular", "/manager/employees", 5, "page.manager.employees", "MANAGER"),
        new("MANAGER_ORGANIZATION", "Phòng ban & chức vụ", "BuildingRegular", "/manager/organization", 8, "page.manager.organization", "MANAGER"),
        new("MANAGER_ORG_TREE", "Cây tổ chức", "PeopleTeamRegular", "/manager/org-tree", 9, "page.manager.orgtree", "MANAGER"),

        // Nhóm Nhân sự (10-19): việc hằng ngày của HR. Nhóm danh mục (20-29) và hệ thống (30+) đứng sau.
        new("HR_ATTENDANCE", "Chấm công toàn công ty", "ClockRegular", "/hr/attendance", 10, "page.hr.attendance", "ADMIN"),
        new("HR_ADJUSTMENTS", "Xử lý điều chỉnh công", "DocumentBulletListRegular", "/hr/adjustments", 11, "page.hr.adjustments", "ADMIN"),
        new("HR_LEAVE_RETURNS", "Xác nhận rút ngắn phép", "CalendarRegular", "/hr/leave-returns", 12, "page.hr.leave-returns", "ADMIN"),
        new("HR_ATTENDANCE_NOTES", "Ghi nhận chấm công", "DocumentBulletListRegular", "/hr/attendance-notes", 13, "page.hr.attendance-notes", "ADMIN"),
        new("HR_TIMESHEET", "Bảng công tháng", "TableRegular", "/hr/timesheet", 14, "page.hr.timesheet", "ADMIN"),
        new("HR_HOLIDAYS", "Ngày nghỉ lễ", "CalendarRegular", "/hr/holidays", 15, "page.hr.holidays", "ADMIN"),

        new("ADMIN_EMPLOYEES", "Quản lý nhân viên", "PeopleTeamRegular", "/admin/employees", 20, "page.admin.employees", "ADMIN"),
        new("ADMIN_DEPARTMENTS", "Phòng ban", "BuildingRegular", "/admin/departments", 21, "page.admin.departments", "ADMIN"),
        new("ADMIN_POSITIONS", "Chức vụ", "PersonRegular", "/admin/positions", 22, "page.admin.positions", "ADMIN"),
        new("ADMIN_LEAVE_TYPES", "Loại nghỉ phép", "CalendarRegular", "/admin/leave-types", 23, "page.admin.leavetypes", "ADMIN"),
        new("ADMIN_AUDIT", "Nhật ký hệ thống", "HistoryRegular", "/admin/audit", 30, "page.admin.audit", "ADMIN"),
        new("ADMIN_SYSTEM", "Quản trị hệ thống", "SettingsRegular", "/admin/system", 31, "page.admin.system", "ADMIN"),
    };

    // Idempotent: chỉ chèn những MenuCode chưa tồn tại, để khi thêm seed mới (vd MANAGER_AUDIT)
    // các DB đã chạy từ trước (Menus không rỗng) vẫn nhận được menu mới ở lần khởi động kế tiếp,
    // thay vì bị bỏ qua toàn bộ như trước (AnyAsync() chặn cả seed).
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        var now = VietnamClock.Now;

        // Audit Log là trang dùng chung cho toàn hệ thống (chỉ 1 menu, route /admin/audit),
        // không phải trang riêng theo role. Bản seed trước có lúc tạo MANAGER_AUDIT (route
        // /manager/audit) như 1 menu riêng cho Manager — dọn lại 1 lần: gộp về đúng 1 menu,
        // Manager muốn xem chỉ cần được cấp permission "page.admin.audit" như Admin.
        await MergeManagerAuditMenuAsync(context, now);
        await SplitAdminOrganizationMenuAsync(context);
        // Menu "Tài khoản" (ADMIN_USERS) đã bị bỏ: tài khoản gộp vào "Quản lý nhân viên" (migration MergeEmployeeAndUser
        // xoá menu + permission page.admin.users). Gọi lại ở đây để DB nào còn sót cũng được dọn.
        await RemoveAdminUsersMenuAsync(context);
        await RenameEnglishSeedMenusAsync(context, now);
        await ReorderExistingMenusAsync(context, now);

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
        await GrantManagerSelfMenuAsync(context, now, "page.employee.attendance");

        // Quản lý cũng có trang cá nhân (nơi chấm công); "Tổng quan phòng ban" chỉ còn số liệu team, không có nút chấm công.
        await GrantManagerSelfMenuAsync(context, now, "page.employee.dashboard");

        // Tương tự, Manager cũng tự xin nghỉ phép: tái dùng trang "Xin nghỉ phép" (route /employee/leave).
        await GrantManagerSelfMenuAsync(context, now, "page.employee.leave");

        // Và tự xin làm thêm giờ: dùng chung trang "Làm thêm giờ" (/employee/overtime).
        await GrantManagerSelfMenuAsync(context, now, "page.employee.overtime");
    }

    // Thứ tự menu theo tần suất dùng (việc hằng ngày lên trước). Chỉ đổi khi thứ tự hiện tại còn đúng bằng mặc định cũ,
    // để không ghi đè thứ tự Admin đã tự sắp xếp.
    private static readonly (string MenuCode, int OldOrder, int NewOrder)[] OrderChanges =
    {
        ("MANAGER_LEAVE", 4, 2), ("MANAGER_EMPLOYEES", 2, 4), ("MANAGER_EMPLOYEES", 4, 5), ("MANAGER_ORGANIZATION", 7, 8),
        ("EMPLOYEE_ATTENDANCE", 2, 5), ("EMPLOYEE_LEAVE", 3, 6), ("EMPLOYEE_OVERTIME", 4, 7),
        ("HR_ATTENDANCE", 1, 10), ("HR_ATTENDANCE_NOTES", 2, 13), ("HR_ADJUSTMENTS", 3, 11), ("HR_LEAVE_RETURNS", 4, 12),
        ("ADMIN_EMPLOYEES", 2, 20), ("ADMIN_DEPARTMENTS", 3, 21), ("ADMIN_POSITIONS", 4, 22), ("ADMIN_LEAVE_TYPES", 5, 23),
        ("ADMIN_AUDIT", 7, 30), ("ADMIN_SYSTEM", 8, 31),
    };

    private static async Task ReorderExistingMenusAsync(ApplicationDbContext context, DateTime now)
    {
        var menus = await context.Menus.ToDictionaryAsync(m => m.MenuCode);
        var changed = false;
        foreach (var (code, oldOrder, newOrder) in OrderChanges)
        {
            if (!menus.TryGetValue(code, out var menu) || menu.DisplayOrder != oldOrder) continue;
            menu.DisplayOrder = newOrder;
            menu.UpdatedAt = now;
            changed = true;
        }

        if (changed) await context.SaveChangesAsync();
    }

    private static async Task GrantManagerSelfMenuAsync(ApplicationDbContext context, DateTime now, string permissionCode)
    {
        var managerRole = await context.Roles.FirstOrDefaultAsync(r => r.RoleCode == "MANAGER");
        var permission = await context.Permissions.FirstOrDefaultAsync(p => p.PermissionCode == permissionCode);
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

    // Xoá menu ADMIN_USERS ("Tài khoản") cùng permission "page.admin.users" và mọi liên kết (MenuPermissions, RolePermissions).
    // Không đụng permission hành động "user.manage" (khoá/mở, đặt lại mật khẩu vẫn dùng).
    private static async Task RemoveAdminUsersMenuAsync(ApplicationDbContext context)
    {
        var menu = await context.Menus.Include(m => m.MenuPermissions).FirstOrDefaultAsync(m => m.MenuCode == "ADMIN_USERS");
        if (menu is not null)
        {
            context.MenuPermissions.RemoveRange(menu.MenuPermissions);
            context.Menus.Remove(menu);
        }

        var permission = await context.Permissions.FirstOrDefaultAsync(p => p.PermissionCode == "page.admin.users");
        if (permission is not null)
        {
            context.MenuPermissions.RemoveRange(await context.MenuPermissions.Where(mp => mp.PermissionId == permission.Id).ToListAsync());
            context.RolePermissions.RemoveRange(await context.RolePermissions.Where(rp => rp.PermissionId == permission.Id).ToListAsync());
            context.Permissions.Remove(permission);
        }

        if (menu is not null || permission is not null) await context.SaveChangesAsync();
    }

    // Việt hoá tên menu seed cũ còn là tiếng Anh/lẫn Anh-Việt (vd "Dashboard", "Audit Log",
    // "System Administration"). Seeder chỉ chèn MenuCode còn thiếu nên DB đã chạy từ trước vẫn
    // giữ tên cũ; đổi 1 lần ở đây, CHỈ khi tên hiện tại còn đúng bằng tên mặc định cũ để không
    // ghi đè tên Admin đã tự đặt. Permission "page.*" đi kèm (tên + mô tả sinh từ tên menu)
    // cũng được đồng bộ theo cùng điều kiện.
    private static readonly (string MenuCode, string PermissionCode, string OldName, string NewName)[] EnglishMenuRenames =
    {
        ("EMPLOYEE_DASHBOARD", "page.employee.dashboard", "Dashboard", "Tổng quan"),
        ("MANAGER_DASHBOARD", "page.manager.dashboard", "Dashboard tổng quan", "Tổng quan phòng ban"),
        ("ADMIN_AUDIT", "page.admin.audit", "Audit Log", "Nhật ký hệ thống"),
        ("ADMIN_SYSTEM", "page.admin.system", "System Administration", "Quản trị hệ thống"),
        // Việc chấm công đã chuyển sang Trang chủ/Tổng quan; trang này chỉ còn lịch và yêu cầu điều chỉnh.
        ("EMPLOYEE_ATTENDANCE", "page.employee.attendance", "Chấm công", "Điều chỉnh chấm công"),
        // Trang này có lịch chấm công của tôi và các yêu cầu điều chỉnh, nên gọi là "Chấm công của tôi".
        ("EMPLOYEE_ATTENDANCE", "page.employee.attendance", "Điều chỉnh chấm công", "Chấm công của tôi"),
        // Trang duyệt giờ gồm cả đơn nghỉ, đi làm lại và làm thêm giờ.
        ("MANAGER_LEAVE", "page.manager.leave", "Duyệt đơn xin nghỉ", "Duyệt đơn từ"),
        ("HR_LEAVE_RETURNS", "page.hr.leave-returns", "Xử lý đi làm lại", "Xác nhận rút ngắn phép"),
        // Trang chủ cá nhân (chấm công) khác với "Tổng quan phòng ban" của quản lý.
        ("EMPLOYEE_DASHBOARD", "page.employee.dashboard", "Tổng quan", "Tổng quan của tôi"),
    };

    private static async Task RenameEnglishSeedMenusAsync(ApplicationDbContext context, DateTime now)
    {
        var changed = false;
        foreach (var (menuCode, permissionCode, oldName, newName) in EnglishMenuRenames)
        {
            var menu = await context.Menus.FirstOrDefaultAsync(m => m.MenuCode == menuCode);
            if (menu is not null && menu.MenuName == oldName)
            {
                menu.MenuName = newName;
                menu.UpdatedAt = now;
                changed = true;
            }

            var permission = await context.Permissions.FirstOrDefaultAsync(p => p.PermissionCode == permissionCode);
            if (permission is null) continue;

            if (permission.PermissionName == oldName)
            {
                permission.PermissionName = newName;
                changed = true;
            }

            if (permission.Description == $"Quyền vào trang '{oldName}'.")
            {
                permission.Description = $"Quyền vào trang '{newName}'.";
                changed = true;
            }
        }

        if (changed) await context.SaveChangesAsync();
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
