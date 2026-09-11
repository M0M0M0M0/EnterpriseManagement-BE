using EnterpriseManagement.Domain.Entities.HR;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseManagement.Infrastructure.Persistence;

// Seed chức vụ mẫu để Admin có sẵn list chọn nhanh khi tạo nhân viên, thay vì phải tự gõ
// từ đầu. RankLevel để hở khoảng cách (10, 20, 30, 40) để sau này chèn thêm chức vụ mới
// ở giữa mà không cần đổi số của các chức vụ khác
public static class PositionSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        if (await context.Positions.AnyAsync())
        {
            return;
        }

        var now = DateTime.UtcNow;
        context.Positions.AddRange(
            new Position { PositionCode = "HEAD", PositionName = "Trưởng phòng", RankLevel = 10, IsActive = true, CreatedAt = now },
            new Position { PositionCode = "DEPUTY", PositionName = "Phó phòng", RankLevel = 20, IsActive = true, CreatedAt = now },
            new Position { PositionCode = "MANAGER", PositionName = "Quản lý", RankLevel = 30, IsActive = true, CreatedAt = now },
            new Position { PositionCode = "STAFF", PositionName = "Nhân viên", RankLevel = 40, IsActive = true, CreatedAt = now });

        await context.SaveChangesAsync();
    }
}
