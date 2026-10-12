using EnterpriseManagement.Application.Common;
using EnterpriseManagement.Application.Interfaces;
using EnterpriseManagement.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseManagement.Infrastructure.Persistence;

public static class DataSeeder
{
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
            var admin = new User
            {
                Username = "admin",
                Email = "admin@enterprise.local",
                PasswordHash = passwordHasher.Hash("admin"),
                IsActive = true,
                CreatedAt = VietnamClock.Now
            };
            admin.UserRoles.Add(new UserRole { Role = adminRole, AssignedAt = VietnamClock.Now });

            context.Users.Add(admin);
            await context.SaveChangesAsync();
        }
    }
}
