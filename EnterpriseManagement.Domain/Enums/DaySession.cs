namespace EnterpriseManagement.Domain.Enums;

// Một buổi trong ngày làm việc: sáng 08:00-12:00, chiều 13:00-17:00 (nghỉ trưa 12:00-13:00 không tính).
// Khác LeaveSession ở chỗ không có FullDay, vì dữ liệu hiệu lực luôn được tách theo từng buổi.
public enum DaySession
{
    Morning,
    Afternoon
}
