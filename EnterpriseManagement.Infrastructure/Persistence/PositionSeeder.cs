using EnterpriseManagement.Application.Common;
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

        var now = VietnamClock.Now;
        // RoleCode bắt buộc set ngay từ đây: đây là field dùng để tự suy ra role khi Admin tạo
        // tài khoản cho 1 hồ sơ (UserService.CreateAsync) — để trống (null) sẽ khiến MỌI tài
        // khoản tạo cho các chức vụ này tự rơi về EMPLOYEE thay vì đúng role, kể cả chức vụ rõ
        // ràng là quản lý (Trưởng/Phó phòng, Quản lý). Từng gây bug thật: DemoDataSeeder seed
        // 20 tài khoản mẫu dựa vào các chức vụ này lúc RoleCode còn null, cả team quản lý (HEAD/
        // DEPUTY/MANAGER) đều bị gán nhầm role EMPLOYEE.
        context.Positions.AddRange(
            new Position { PositionCode = "HEAD", PositionName = "Trưởng phòng", RankLevel = 10, RoleCode = "MANAGER", IsActive = true, CreatedAt = now },
            new Position { PositionCode = "DEPUTY", PositionName = "Phó phòng", RankLevel = 20, RoleCode = "MANAGER", IsActive = true, CreatedAt = now },
            new Position { PositionCode = "MANAGER", PositionName = "Quản lý", RankLevel = 30, RoleCode = "MANAGER", IsActive = true, CreatedAt = now },
            new Position { PositionCode = "STAFF", PositionName = "Nhân viên", RankLevel = 40, RoleCode = "EMPLOYEE", IsActive = true, CreatedAt = now });

        await context.SaveChangesAsync();
    }
}
