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
        new("attendance.punch", "Chấm công", "attendance", "Check-in / check-out hằng ngày.", new[] { "EMPLOYEE" }),
        new("attendance.view.team", "Xem chấm công phòng ban", "attendance", "Xem bảng công của cả phòng ban.", new[] { "MANAGER" }),
        new("attendance.adjustment.self", "Gửi yêu cầu điều chỉnh công", "attendance", "Gửi và xem yêu cầu điều chỉnh công của chính mình.", new[] { "EMPLOYEE" }),
        new("attendance.adjustment.view", "Xem yêu cầu điều chỉnh công chờ duyệt", "attendance", "Xem danh sách yêu cầu điều chỉnh công đang chờ duyệt.", new[] { "MANAGER" }),
        new("attendance.adjustment.approve", "Duyệt/từ chối điều chỉnh công", "attendance", "Duyệt hoặc từ chối yêu cầu điều chỉnh công.", new[] { "MANAGER" }),

        new("leave.request.self", "Xin nghỉ phép", "leave", "Gửi, xem và hủy đơn xin nghỉ của chính mình.", new[] { "EMPLOYEE" }),
        new("leave.request.view", "Xem đơn xin nghỉ", "leave", "Xem danh sách đơn xin nghỉ đang chờ và lịch sử duyệt.", new[] { "MANAGER" }),
        new("leave.request.approve", "Duyệt/từ chối đơn xin nghỉ", "leave", "Duyệt hoặc từ chối đơn xin nghỉ.", new[] { "MANAGER" }),
        new("leavebalance.manage", "Cấp lại số ngày/giờ nghỉ", "leave", "Chỉnh số ngày/giờ nghỉ còn lại của nhân viên.", new[] { "MANAGER" }),
        new("leavetype.manage", "Quản lý loại nghỉ phép", "leave", "Tạo/sửa loại nghỉ phép và quy tắc tích lũy.", new[] { "ADMIN" }),

        new("sales.self", "Ghi nhận Sale", "sales", "Gửi, xem và sửa đơn hàng Sale của chính mình.", new[] { "EMPLOYEE" }),
        new("sales.view", "Xem Sale phòng ban", "sales", "Xem danh sách Sale đang chờ duyệt và lịch sử.", new[] { "MANAGER" }),
        new("sales.approve", "Duyệt/từ chối Sale", "sales", "Duyệt hoặc từ chối đơn Sale.", new[] { "MANAGER" }),

        new("customer.create", "Tạo khách hàng", "customer", "Thêm khách hàng mới vào hệ thống.", new[] { "EMPLOYEE", "MANAGER", "ADMIN" }),

        new("dashboard.view.team", "Xem dashboard phòng ban", "dashboard", "Xem dashboard tổng quan của cả phòng ban.", new[] { "MANAGER" }),

        new("employee.view.all", "Xem toàn bộ nhân viên", "employee", "Xem danh sách toàn bộ nhân viên trong hệ thống.", new[] { "ADMIN" }),
        new("employee.manage", "Quản lý hồ sơ nhân viên", "employee", "Tạo, sửa, khóa/mở hồ sơ nhân viên.", new[] { "ADMIN" }),
        new("employee.view.team", "Xem nhân viên phòng ban", "employee", "Xem danh sách nhân viên trong phòng ban quản lý.", new[] { "MANAGER" }),

        new("department.manage", "Quản lý phòng ban", "organization", "Tạo, sửa, khóa/mở phòng ban.", new[] { "ADMIN" }),
        new("position.manage", "Quản lý chức vụ", "organization", "Tạo, sửa, khóa/mở chức vụ và lương chuẩn.", new[] { "ADMIN" }),

        new("payroll.calculate", "Tính lương", "payroll", "Chạy tính lương theo tháng.", new[] { "ADMIN" }),

        new("user.manage", "Quản lý tài khoản", "system", "Tạo tài khoản, khóa/mở, đặt lại mật khẩu.", new[] { "ADMIN" }),
        new("role.manage", "Quản lý vai trò", "system", "Tạo, sửa vai trò và gán permission.", new[] { "ADMIN" }),
        new("permission.manage", "Quản lý permission", "system", "Tạo, sửa permission trong hệ thống.", new[] { "ADMIN" }),
        new("menu.manage", "Quản lý menu", "system", "Tạo, sửa, ẩn/hiện menu sidebar.", new[] { "ADMIN" }),
    };

    public static async Task SeedAsync(ApplicationDbContext context)
    {
        if (await context.Permissions.AnyAsync(p => p.Module != "page"))
        {
            return;
        }

        var roles = await context.Roles.ToDictionaryAsync(r => r.RoleCode);
        var now = DateTime.UtcNow;

        foreach (var seed in Seeds)
        {
            var permission = new Permission
            {
                PermissionCode = seed.PermissionCode,
                PermissionName = seed.PermissionName,
                Module = seed.Module,
                Description = seed.Description,
                IsActive = true,
                CreatedAt = now
            };
            context.Permissions.Add(permission);

            foreach (var roleCode in seed.RoleCodes)
            {
                if (!roles.TryGetValue(roleCode, out var role)) continue;

                context.RolePermissions.Add(new RolePermission
                {
                    Role = role,
                    Permission = permission,
                    GrantedAt = now,
                    CreatedAt = now
                });
            }
        }

        await context.SaveChangesAsync();
    }
}
