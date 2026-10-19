namespace EnterpriseManagement.Application.Common;

public static class DateRangeHelper
{
    public static int CountWeekdays(DateOnly start, DateOnly end)
    {
        var count = 0;
        for (var date = start; date <= end; date = date.AddDays(1))
        {
            if (IsWeekday(date))
            {
                count++;
            }
        }

        return count;
    }

    // Ngày làm việc = không phải cuối tuần và không phải ngày nghỉ lễ của công ty (tên cũ giữ lại cho các nơi đang gọi).
    public static bool IsWeekday(DateOnly date) => !WorkCalendar.IsNonWorkingDay(date);
}
