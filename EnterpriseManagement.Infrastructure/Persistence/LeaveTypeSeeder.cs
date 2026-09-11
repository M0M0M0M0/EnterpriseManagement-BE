using EnterpriseManagement.Domain.Entities.Leave;
using EnterpriseManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseManagement.Infrastructure.Persistence;

public static class LeaveTypeSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        if (await context.LeaveTypes.AnyAsync())
        {
            return;
        }

        context.LeaveTypes.AddRange(
            new LeaveType
            {
                LeaveTypeCode = "ANNUAL",
                LeaveTypeName = "Nghỉ có phép",
                AccrualAmount = 1,
                AccrualUnit = LeaveUnit.Days,
                AccrualPeriod = LeaveAccrualPeriod.ProratedYearly,
                IsPaid = true,
                Description = "1 ngày phép mỗi tháng, cộng dồn theo số tháng còn lại trong năm kể từ khi tạo hồ sơ.",
                IsActive = true
            },
            new LeaveType
            {
                LeaveTypeCode = "SHORT",
                LeaveTypeName = "Nghỉ ngắn",
                AccrualAmount = 2,
                AccrualUnit = LeaveUnit.Hours,
                AccrualPeriod = LeaveAccrualPeriod.MonthlyReset,
                IsPaid = true,
                Description = "2 giờ mỗi tháng, không cộng dồn qua tháng sau.",
                IsActive = true
            },
            new LeaveType
            {
                LeaveTypeCode = "UNPAID",
                LeaveTypeName = "Nghỉ không lương",
                AccrualAmount = 2,
                AccrualUnit = LeaveUnit.Days,
                AccrualPeriod = LeaveAccrualPeriod.ProratedYearly,
                IsPaid = false,
                Description = "2 ngày mỗi tháng, cộng dồn theo số tháng còn lại trong năm kể từ khi tạo hồ sơ.",
                IsActive = true
            },
            new LeaveType
            {
                LeaveTypeCode = "REGIME",
                LeaveTypeName = "Nghỉ chế độ",
                AccrualAmount = 9,
                AccrualUnit = LeaveUnit.Days,
                AccrualPeriod = LeaveAccrualPeriod.FlatYearly,
                IsPaid = true,
                Description = "9 ngày mỗi năm (kết hôn, tang chế), không cộng dồn theo tháng, chỉ reset khi sang năm mới.",
                IsActive = true
            });

        await context.SaveChangesAsync();
    }
}
