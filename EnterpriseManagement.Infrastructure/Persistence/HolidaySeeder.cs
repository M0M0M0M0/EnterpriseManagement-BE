using EnterpriseManagement.Application.Common;
using EnterpriseManagement.Domain.Entities.Attendance;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseManagement.Infrastructure.Persistence;

// Ngày nghỉ lễ theo luật định (Bộ luật Lao động 2019, Điều 112). Chỉ seed MỘT LẦN khi bảng còn trống, để HR xoá/sửa không bị
// khôi phục lại lúc khởi động.
//   - 2026: đủ các kỳ nghỉ (Tết Âm lịch, Giỗ Tổ và nghỉ bù theo thông báo chính thức).
//   - 2027: chỉ các ngày lễ cố định theo dương lịch. Tết Âm lịch, Giỗ Tổ và ngày nghỉ bù của năm 2027 do HR bổ sung trong trang
//     "Ngày nghỉ lễ" khi có thông báo nghỉ chính thức hằng năm của Chính phủ.
public static class HolidaySeeder
{
    private static readonly (DateOnly Date, string Name)[] Seeds =
    {
        (new(2026, 1, 1), "Tết Dương lịch"),
        (new(2026, 2, 16), "Tết Nguyên đán (29 tháng Chạp)"),
        (new(2026, 2, 17), "Tết Nguyên đán (mùng 1)"),
        (new(2026, 2, 18), "Tết Nguyên đán (mùng 2)"),
        (new(2026, 2, 19), "Tết Nguyên đán (mùng 3)"),
        (new(2026, 2, 20), "Tết Nguyên đán (mùng 4)"),
        (new(2026, 4, 26), "Giỗ Tổ Hùng Vương (10/3 âm lịch)"),
        (new(2026, 4, 27), "Nghỉ bù Giỗ Tổ Hùng Vương"),
        (new(2026, 4, 30), "Ngày Giải phóng miền Nam"),
        (new(2026, 5, 1), "Quốc tế Lao động"),
        (new(2026, 9, 1), "Nghỉ Quốc khánh (liền kề)"),
        (new(2026, 9, 2), "Quốc khánh"),

        (new(2027, 1, 1), "Tết Dương lịch"),
        (new(2027, 4, 30), "Ngày Giải phóng miền Nam"),
        (new(2027, 5, 1), "Quốc tế Lao động"),
        (new(2027, 9, 2), "Quốc khánh"),
    };

    public static async Task SeedAsync(ApplicationDbContext context)
    {
        if (await context.Holidays.AnyAsync()) return;

        var now = VietnamClock.Now;
        foreach (var (date, name) in Seeds)
        {
            context.Holidays.Add(new Holiday { Date = date, Name = name, CreatedAt = now });
        }

        await context.SaveChangesAsync();
    }
}
