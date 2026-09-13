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
                new Role { RoleCode = "ADMIN", RoleName = "Administrator", IsActive = true, CreatedAt = VietnamClock.Now },
                new Role { RoleCode = "MANAGER", RoleName = "Manager", IsActive = true, CreatedAt = VietnamClock.Now },
                new Role { RoleCode = "EMPLOYEE", RoleName = "Employee", IsActive = true, CreatedAt = VietnamClock.Now });
            await context.SaveChangesAsync();
        }

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
