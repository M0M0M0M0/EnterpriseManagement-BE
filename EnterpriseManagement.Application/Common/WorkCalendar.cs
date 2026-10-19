namespace EnterpriseManagement.Application.Common;

// Lịch làm việc của công ty: cuối tuần + ngày nghỉ lễ. Các hàm tính (EffectiveDayCalculator, đếm ngày nghỉ...) là hàm tĩnh không
// truy cập DB nên ngày lễ được nạp sẵn vào bộ nhớ lúc khởi động và nạp lại mỗi khi HR thêm/xoá ngày lễ.
public static class WorkCalendar
{
    private static volatile IReadOnlyDictionary<DateOnly, string> _holidays = new Dictionary<DateOnly, string>();

    public static void Load(IEnumerable<(DateOnly Date, string Name)> holidays)
    {
        var map = new Dictionary<DateOnly, string>();
        foreach (var (date, name) in holidays) map[date] = name;
        _holidays = map;
    }

    public static bool IsWeekend(DateOnly date) => date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

    public static bool IsHoliday(DateOnly date) => _holidays.ContainsKey(date);

    public static string? HolidayName(DateOnly date) => _holidays.TryGetValue(date, out var name) ? name : null;

    public static bool IsNonWorkingDay(DateOnly date) => IsWeekend(date) || IsHoliday(date);

    // Lý do ngày không phải ngày làm việc, dùng trong thông báo lỗi.
    public static string NonWorkingReason(DateOnly date) =>
        IsWeekend(date) ? "thứ Bảy hoặc Chủ nhật" : $"ngày lễ ({HolidayName(date)})";
}
