using EnterpriseManagement.Application.Common;
using EnterpriseManagement.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseManagement.Infrastructure.Persistence;

// Seed permission cho từng HÀNH ĐỘNG thật trong API (xem/duyệt/từ chối/quản lý...),
// tách biệt hoàn toàn khỏi permission "page.*" của MenuSeeder (permission "vào được
// trang nào"). Các permission ở đây được gắn trực tiếp lên controller qua
// [RequirePermission("code")], nên Admin có thể bật/tắt từng hành động (vd "duyệt đơn
// nghỉ") độc lập với việc có thấy trang đó hay không.
public static class ActionPermissionSeeder
{
    private record ActionSeed(string PermissionCode, string PermissionName, string Module, string Description, string[] RoleCodes);

    private static readonly ActionSeed[] Seeds =
    {
        new("attendance.punch", "Chấm công", "attendance", "Chấm công vào / ra hằng ngày.", new[] { "EMPLOYEE", "MANAGER" }),
        new("attendance.view.team", "Xem chấm công phòng ban", "attendance", "Xem bảng công của cả phòng ban.", new[] { "MANAGER" }),
        // Manager cũng tự chấm công như Employee (xem attendance.punch) nên cũng cần tự gửi/xem
        // yêu cầu điều chỉnh công của chính mình khi quên chấm công — thiếu MANAGER ở đây khiến
        // trang Chấm công của Manager gọi GET /attendance/adjustments/mine bị 403 ngay khi mở trang.
        new("attendance.adjustment.self", "Gửi yêu cầu điều chỉnh công", "attendance", "Gửi và xem yêu cầu điều chỉnh công của chính mình.", new[] { "EMPLOYEE", "MANAGER" }),
        new("attendance.adjustment.view", "Xem yêu cầu điều chỉnh công chờ duyệt", "attendance", "Xem danh sách yêu cầu điều chỉnh công đang chờ duyệt.", new[] { "MANAGER" }),
        new("attendance.adjustment.approve", "Duyệt/từ chối điều chỉnh công", "attendance", "Duyệt hoặc từ chối yêu cầu điều chỉnh công.", new[] { "MANAGER" }),

        // Các quyền xử lý chấm công toàn công ty. Không gán cho role nào ngoài ADMIN: Admin tự tạo role (vd HR) rồi tick đúng quyền.
        new("attendance.view.all", "Xem chấm công toàn công ty", "attendance", "Xem bảng chấm công của mọi nhân viên, lọc theo phòng ban.", new[] { "ADMIN" }),
        new("attendance.note.manage", "Ghi chú ngoại lệ chấm công", "attendance", "Ghi chú ngoại lệ theo buổi (quên chấm công, đi muộn, vắng...), thu hồi ghi chú và xác nhận yêu cầu đi làm lại.", new[] { "ADMIN" }),
        new("attendance.adjustment.apply", "Xử lý điều chỉnh công", "attendance", "Áp dụng hoặc từ chối yêu cầu điều chỉnh công sau khi quản lý đã duyệt.", new[] { "ADMIN" }),

        // Manager cũng tự chấm công/gửi điều chỉnh công như Employee (xem attendance.punch,
        // attendance.adjustment.self) nên cũng cần tự xin nghỉ phép cho chính mình — thiếu
        // MANAGER ở đây khiến trang "Xin nghỉ phép" của Manager gọi GET /leave-requests/mine bị
        // 403, làm Promise.all cùng batch fail theo và dropdown loại nghỉ không bao giờ hiện ra.
        new("leave.request.self", "Xin nghỉ phép", "leave", "Gửi, xem và hủy đơn xin nghỉ của chính mình.", new[] { "EMPLOYEE", "MANAGER" }),
        new("leave.request.view", "Xem đơn xin nghỉ", "leave", "Xem danh sách đơn xin nghỉ đang chờ và lịch sử duyệt.", new[] { "MANAGER" }),
        new("leave.request.approve", "Duyệt/từ chối đơn xin nghỉ", "leave", "Duyệt hoặc từ chối đơn xin nghỉ.", new[] { "MANAGER" }),
        new("leavebalance.manage", "Cấp lại số ngày/giờ nghỉ", "leave", "Chỉnh số ngày/giờ nghỉ còn lại của nhân viên.", new[] { "MANAGER" }),
        new("leavetype.manage", "Quản lý loại nghỉ phép", "leave", "Tạo/sửa loại nghỉ phép và quy tắc tích lũy.", new[] { "ADMIN" }),

        // Làm thêm giờ (OT): nhân viên và quản lý tự xin; quản lý duyệt đơn của team như đơn nghỉ.
        new("overtime.request.self", "Xin làm thêm giờ", "overtime", "Gửi, xem và hủy đơn xin làm thêm giờ của chính mình.", new[] { "EMPLOYEE", "MANAGER" }),
        new("overtime.request.view", "Xem đơn làm thêm giờ", "overtime", "Xem danh sách đơn làm thêm giờ đang chờ và lịch sử duyệt của team.", new[] { "MANAGER" }),
        new("overtime.request.approve", "Duyệt/từ chối làm thêm giờ", "overtime", "Duyệt hoặc từ chối đơn xin làm thêm giờ.", new[] { "MANAGER" }),

        // Lịch ngày lễ và bảng công tháng: việc của Nhân sự (HR được cấp trong HrRoleSeeder).
        new("holiday.manage", "Quản lý ngày nghỉ lễ", "attendance", "Thêm/xoá ngày nghỉ lễ của công ty.", new[] { "ADMIN" }),
        new("timesheet.manage", "Bảng công tháng", "attendance", "Xem và xuất file bảng công tháng của toàn công ty.", new[] { "ADMIN" }),
        // Chốt/mở lại kỳ công khoá dữ liệu cả công ty nên chỉ trưởng bộ phận nhân sự (vai trò HR_LEAD) có, không phải mọi HR.
        new("timesheet.close", "Chốt bảng công tháng", "attendance", "Chốt và mở lại bảng công tháng (trưởng bộ phận nhân sự).", new[] { "ADMIN" }),

        new("dashboard.view.team", "Xem tổng quan phòng ban", "dashboard", "Xem trang tổng quan của cả phòng ban.", new[] { "MANAGER" }),

        new("employee.view.all", "Xem toàn bộ nhân viên", "employee", "Xem danh sách toàn bộ nhân viên trong hệ thống.", new[] { "ADMIN" }),
        new("employee.manage", "Quản lý hồ sơ nhân viên", "employee", "Tạo, sửa, khóa/mở hồ sơ nhân viên.", new[] { "ADMIN" }),
        new("employee.view.team", "Xem nhân viên phòng ban", "employee", "Xem danh sách nhân viên trong phòng ban quản lý.", new[] { "MANAGER" }),

        new("department.manage", "Quản lý phòng ban", "organization", "Tạo, sửa, khóa/mở phòng ban.", new[] { "ADMIN" }),
        new("position.manage", "Quản lý chức vụ", "organization", "Tạo, sửa, khóa/mở chức vụ và lương chuẩn.", new[] { "ADMIN" }),

        new("user.manage", "Quản lý tài khoản", "system", "Tạo tài khoản, khóa/mở, đặt lại mật khẩu.", new[] { "ADMIN" }),
        new("role.manage", "Quản lý vai trò", "system", "Tạo, sửa vai trò và gán quyền.", new[] { "ADMIN" }),
        new("permission.manage", "Quản lý quyền", "system", "Tạo, sửa quyền trong hệ thống.", new[] { "ADMIN" }),
        new("menu.manage", "Quản lý menu", "system", "Tạo, sửa, ẩn/hiện menu sidebar.", new[] { "ADMIN" }),

        new("audit.view", "Xem nhật ký hệ thống", "system", "Xem lịch sử thao tác của toàn hệ thống.", new[] { "ADMIN", "MANAGER" }),
    };

    // Idempotent: chỉ tạo permission chưa tồn tại, và luôn đồng bộ RolePermission theo đúng
    // RoleCodes khai báo ở Seeds (kể cả permission đã tồn tại từ trước) — để khi code thêm 1
    // role vào seed có sẵn (vd Manager cũng được attendance.punch), DB cũ chạy lại tự nhận
    // đủ ở lần khởi động kế tiếp, không cần xoá permission để seed lại từ đầu.
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        var now = VietnamClock.Now;
        var existingPermissionCodes = (await context.Permissions.Select(p => p.PermissionCode).ToListAsync()).ToHashSet();
        var missingSeeds = Seeds.Where(s => !existingPermissionCodes.Contains(s.PermissionCode)).ToList();

        if (missingSeeds.Count > 0)
        {
            foreach (var seed in missingSeeds)
            {
                context.Permissions.Add(new Permission
                {
                    PermissionCode = seed.PermissionCode,
                    PermissionName = seed.PermissionName,
                    Module = seed.Module,
                    Description = seed.Description,
                    IsActive = true,
                    CreatedAt = now
                });
            }

            await context.SaveChangesAsync();
        }

        await RenameEnglishPermissionsAsync(context, now);

        var roles = await context.Roles.ToDictionaryAsync(r => r.RoleCode);
        var permissionsByCode = await context.Permissions.ToDictionaryAsync(p => p.PermissionCode);
        var existingRolePermissionKeys = (await context.RolePermissions
                .Select(rp => new { rp.RoleId, rp.PermissionId })
                .ToListAsync())
            .Select(x => (x.RoleId, x.PermissionId))
            .ToHashSet();

        var addedGrant = false;
        foreach (var seed in Seeds)
        {
            if (!permissionsByCode.TryGetValue(seed.PermissionCode, out var permission)) continue;

            foreach (var roleCode in seed.RoleCodes)
            {
                if (!roles.TryGetValue(roleCode, out var role)) continue;
                if (existingRolePermissionKeys.Contains((role.Id, permission.Id))) continue;

                context.RolePermissions.Add(new RolePermission
                {
                    Role = role,
                    Permission = permission,
                    GrantedAt = now,
                    CreatedAt = now
                });
                addedGrant = true;
            }
        }

        if (addedGrant)
        {
            await context.SaveChangesAsync();
        }
    }

    // Việt hoá tên/mô tả permission seed cũ còn tiếng Anh. Seeder chỉ chèn permission chưa có nên
    // DB cũ vẫn giữ bản tiếng Anh; chỉ đổi khi giá trị hiện tại còn đúng bằng bản mặc định cũ,
    // để không ghi đè nội dung Admin đã tự sửa.
    private static readonly (string PermissionCode, string? OldName, string? NewName, string? OldDescription, string? NewDescription)[] EnglishRenames =
    {
        ("attendance.punch", null, null, "Check-in / check-out hằng ngày.", "Chấm công vào / ra hằng ngày."),
        ("dashboard.view.team", "Xem dashboard phòng ban", "Xem tổng quan phòng ban", "Xem dashboard tổng quan của cả phòng ban.", "Xem trang tổng quan của cả phòng ban."),
        ("permission.manage", "Quản lý permission", "Quản lý quyền", "Tạo, sửa permission trong hệ thống.", "Tạo, sửa quyền trong hệ thống."),
        ("role.manage", null, null, "Tạo, sửa vai trò và gán permission.", "Tạo, sửa vai trò và gán quyền."),
        ("audit.view", "Xem Audit Log", "Xem nhật ký hệ thống", null, null),
    };

    private static async Task RenameEnglishPermissionsAsync(ApplicationDbContext context, DateTime now)
    {
        var changed = false;
        foreach (var (code, oldName, newName, oldDescription, newDescription) in EnglishRenames)
        {
            var permission = await context.Permissions.FirstOrDefaultAsync(p => p.PermissionCode == code);
            if (permission is null) continue;

            if (newName is not null && permission.PermissionName == oldName)
            {
                permission.PermissionName = newName;
                changed = true;
            }

            if (newDescription is not null && permission.Description == oldDescription)
            {
                permission.Description = newDescription;
                changed = true;
            }
        }

        if (changed) await context.SaveChangesAsync();
    }
}
