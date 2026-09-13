namespace EnterpriseManagement.Application.Common;

// Toàn bộ hệ thống (business logic, audit timestamp, "hôm nay"...) dùng giờ Việt Nam
// (UTC+7, không có DST) làm chuẩn duy nhất — thay vì DateTime.Now (phụ thuộc múi giờ hệ
// điều hành của máy chạy server, có thể sai nếu deploy lên server đặt UTC) hoặc
// DateTime.UtcNow (đúng nhưng lệch 7 tiếng, gây sai "ngày hôm nay"/giờ cắt-off gần nửa
// đêm giờ VN). Dùng offset cố định thay vì TimeZoneInfo vì Việt Nam không có DST.
public static class VietnamClock
{
    public static readonly TimeSpan Offset = TimeSpan.FromHours(7);

    // Kind=Unspecified vì giá trị này là "giờ tường" (wall-clock) của VN, không phải một
    // thời điểm UTC thật — tránh nhầm lẫn nếu code khác coi Kind=Utc là instant thật.
    public static DateTime Now => DateTime.SpecifyKind(DateTime.UtcNow + Offset, DateTimeKind.Unspecified);

    public static DateOnly Today => DateOnly.FromDateTime(Now);
}
