using EnterpriseManagement.Application.Common;
using EnterpriseManagement.Domain.Entities.HR;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseManagement.Infrastructure.Persistence;

// Seed chức vụ mẫu để Admin có sẵn list chọn nhanh khi tạo nhân viên, thay vì phải tự gõ từ đầu.
public static class PositionSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        if (await context.Positions.AnyAsync())
        {
            return;
        }

        var now = VietnamClock.Now;
        context.Positions.AddRange(
            new Position { PositionCode = "HEAD", PositionName = "Trưởng phòng", IsActive = true, CreatedAt = now },
            new Position { PositionCode = "DEPUTY", PositionName = "Phó phòng", IsActive = true, CreatedAt = now },
            new Position { PositionCode = "MANAGER", PositionName = "Quản lý", IsActive = true, CreatedAt = now },
            new Position { PositionCode = "STAFF", PositionName = "Nhân viên", IsActive = true, CreatedAt = now });

        await context.SaveChangesAsync();
    }
}
