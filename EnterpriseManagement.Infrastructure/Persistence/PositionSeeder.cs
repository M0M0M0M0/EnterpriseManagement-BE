using EnterpriseManagement.Application.Common;
using EnterpriseManagement.Domain.Entities.HR;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseManagement.Infrastructure.Persistence;

// Seed chức vụ mẫu để Admin có sẵn list chọn nhanh khi tạo nhân viên, thay vì phải tự gõ từ đầu.
public static class PositionSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        // Bỏ qua chức vụ hệ thống SYSADMIN (do DataSeeder/migration tạo cho admin) khi xét bảng đã có dữ liệu hay chưa.
        if (await context.Positions.AnyAsync(p => p.PositionCode != DataSeeder.SystemPositionCode))
        {
            return;
        }

        var now = VietnamClock.Now;
        context.Positions.AddRange(
            new Position { PositionCode = "HEAD", RoleCode = "MANAGER", PositionName = "Trưởng phòng", IsActive = true, CreatedAt = now },
            new Position { PositionCode = "DEPUTY", RoleCode = "MANAGER", PositionName = "Phó phòng", IsActive = true, CreatedAt = now },
            new Position { PositionCode = "MANAGER", RoleCode = "MANAGER", PositionName = "Quản lý", IsActive = true, CreatedAt = now },
            new Position { PositionCode = "STAFF", RoleCode = "EMPLOYEE", PositionName = "Nhân viên", IsActive = true, CreatedAt = now });

        await context.SaveChangesAsync();
    }
}
