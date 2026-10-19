using EnterpriseManagement.Application.Common;
using EnterpriseManagement.Domain.Entities.HR;
using EnterpriseManagement.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseManagement.Infrastructure.Persistence;

// Vai trò Nhân sự (HR): người xử lý nghiệp vụ nhân sự toàn công ty, tách khỏi Quản lý (chỉ duyệt đơn của team) và Admin (kỹ thuật).
// HR cũng là nhân viên đi làm nên có đủ quyền "Nhân viên" cơ bản.
// Chạy SAU MenuSeeder và ActionPermissionSeeder để mọi permission đã tồn tại; idempotent (chỉ cấp phần còn thiếu).
public static class HrRoleSeeder
{
    public const string RoleCode = "HR";
    public const string PositionCode = "HRSTAFF";

    private static readonly string[] Grants =
    {
        // Việc của một nhân viên bình thường.
        "page.employee.dashboard", "page.employee.attendance", "page.employee.leave", "page.employee.overtime",
        "attendance.punch", "attendance.adjustment.self", "leave.request.self", "overtime.request.self",

        // Nghiệp vụ nhân sự toàn công ty.
        "page.hr.attendance", "page.hr.attendance-notes", "page.hr.adjustments", "page.hr.leave-returns",
        "page.hr.timesheet", "page.hr.holidays",
        "attendance.view.all", "attendance.note.manage", "attendance.adjustment.apply",
        "employee.view.all", "employee.manage", "page.admin.employees",
        "leavebalance.manage", "leavetype.manage", "page.admin.leavetypes",
        "holiday.manage", "timesheet.manage",
    };

    // Quyền của việc xử lý nhân sự mà bản seed cũ/cấu hình tay từng đưa cho Quản lý. Gỡ MỘT LẦN khi tạo vai trò HR để người
    // duyệt (Quản lý) và người xử lý (HR) là hai người khác nhau.
    private static readonly string[] RevokedFromManager =
    {
        "attendance.view.all", "attendance.note.manage", "attendance.adjustment.apply",
        "page.hr.attendance", "page.hr.attendance-notes", "page.hr.adjustments", "page.hr.leave-returns",
    };

    public static async Task SeedAsync(ApplicationDbContext context)
    {
        var now = VietnamClock.Now;

        var role = await context.Roles.FirstOrDefaultAsync(r => r.RoleCode == RoleCode);
        var created = role is null;
        if (role is null)
        {
            role = new Role { RoleCode = RoleCode, RoleName = "Nhân sự", IsActive = true, CreatedAt = now };
            context.Roles.Add(role);
            await context.SaveChangesAsync();
        }

        var permissions = await context.Permissions.ToDictionaryAsync(p => p.PermissionCode);
        var granted = (await context.RolePermissions.Where(rp => rp.RoleId == role.Id).Select(rp => rp.PermissionId).ToListAsync()).ToHashSet();
        foreach (var code in Grants)
        {
            if (!permissions.TryGetValue(code, out var permission) || granted.Contains(permission.Id)) continue;
            context.RolePermissions.Add(new RolePermission { Role = role, Permission = permission, GrantedAt = now, CreatedAt = now });
        }

        if (created)
        {
            var manager = await context.Roles.FirstOrDefaultAsync(r => r.RoleCode == "MANAGER");
            if (manager is not null)
            {
                var revokeIds = RevokedFromManager.Where(permissions.ContainsKey).Select(c => permissions[c].Id).ToList();
                var stale = await context.RolePermissions.Where(rp => rp.RoleId == manager.Id && revokeIds.Contains(rp.PermissionId)).ToListAsync();
                context.RolePermissions.RemoveRange(stale);
            }
        }

        if (!await context.Positions.AnyAsync(p => p.PositionCode == PositionCode))
        {
            context.Positions.Add(new Position { PositionCode = PositionCode, RoleCode = RoleCode, PositionName = "Chuyên viên nhân sự", IsActive = true, CreatedAt = now });
        }

        await context.SaveChangesAsync();
    }
}
