using EnterpriseManagement.Application.Common;
using EnterpriseManagement.Application.Interfaces;
using EnterpriseManagement.Domain.Entities.HR;
using EnterpriseManagement.Domain.Entities.Identity;
using EnterpriseManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseManagement.Infrastructure.Persistence;

public static class DataSeeder
{
    // Hồ sơ của tài khoản hệ thống admin (khớp migration MergeEmployeeAndUser).
    public const string SystemDepartmentCode = "SYS";
    public const string SystemPositionCode = "SYSADMIN";
    public const string AdminEmployeeCode = "ADMIN";

    public static async Task SeedAsync(ApplicationDbContext context, IPasswordHasher passwordHasher)
    {
        if (!await context.Roles.AnyAsync())
        {
            context.Roles.AddRange(
                new Role { RoleCode = "ADMIN", RoleName = "Quản trị viên", IsActive = true, CreatedAt = VietnamClock.Now },
                new Role { RoleCode = "MANAGER", RoleName = "Quản lý", IsActive = true, CreatedAt = VietnamClock.Now },
                new Role { RoleCode = "EMPLOYEE", RoleName = "Nhân viên", IsActive = true, CreatedAt = VietnamClock.Now });
            await context.SaveChangesAsync();
        }

        // Việt hoá tên vai trò seed cũ (DB đã chạy từ trước): chỉ đổi khi tên còn đúng bản tiếng Anh mặc định.
        var roleRenames = new Dictionary<string, (string Old, string New)>
        {
            ["ADMIN"] = ("Administrator", "Quản trị viên"),
            ["MANAGER"] = ("Manager", "Quản lý"),
            ["EMPLOYEE"] = ("Employee", "Nhân viên"),
        };
        var renamed = false;
        foreach (var role in (await context.Roles.ToListAsync()).Where(r => roleRenames.ContainsKey(r.RoleCode)))
        {
            var (oldName, newName) = roleRenames[role.RoleCode];
            if (role.RoleName != oldName) continue;
            role.RoleName = newName;
            role.UpdatedAt = VietnamClock.Now;
            renamed = true;
        }
        if (renamed) await context.SaveChangesAsync();

        if (!await context.Users.AnyAsync())
        {
            var adminRole = await context.Roles.FirstAsync(r => r.RoleCode == "ADMIN");
            var now = VietnamClock.Now;

            // Admin cũng có hồ sơ nhân viên (tài khoản hệ thống, không chấm công) vì mọi tài khoản phải gắn 1 hồ sơ.
            var department = await context.Departments.FirstOrDefaultAsync(d => d.DepartmentCode == SystemDepartmentCode);
            if (department is null)
            {
                department = new Department
                {
                    DepartmentCode = SystemDepartmentCode,
                    DepartmentName = "Quản trị hệ thống",
                    Description = "Phòng ban chứa tài khoản hệ thống (không chấm công).",
                    IsActive = true,
                    CreatedAt = now
                };
                context.Departments.Add(department);
            }

            var position = await context.Positions.FirstOrDefaultAsync(p => p.PositionCode == SystemPositionCode);
            if (position is null)
            {
                position = new Position
                {
                    PositionCode = SystemPositionCode,
                    PositionName = "Quản trị viên",
                    RoleCode = "ADMIN",
                    IsActive = true,
                    CreatedAt = now
                };
                context.Positions.Add(position);
            }

            var employee = new Employee
            {
                EmployeeCode = AdminEmployeeCode,
                FirstName = "Quản trị",
                LastName = "Hệ thống",
                Email = "admin@enterprise.local",
                Department = department,
                Position = position,
                HireDate = VietnamClock.Today,
                EmploymentStatus = EmploymentStatus.Active,
                TracksAttendance = false,
                CreatedAt = now
            };

            var admin = new User
            {
                Employee = employee,
                Username = "admin",
                PasswordHash = passwordHasher.Hash("admin"),
                IsActive = true,
                CreatedAt = now
            };
            admin.UserRoles.Add(new UserRole { Role = adminRole, AssignedAt = now });

            context.Employees.Add(employee);
            context.Users.Add(admin);
            await context.SaveChangesAsync();
        }
    }
}
