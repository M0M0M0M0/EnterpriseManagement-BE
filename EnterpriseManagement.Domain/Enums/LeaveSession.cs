namespace EnterpriseManagement.Domain.Enums;

// Áp dụng cho các LeaveType tính theo buổi (không áp dụng cho loại nghỉ chọn giờ cụ thể
// như Nghỉ ngắn). Giờ cụ thể của từng buổi do backend gán cố định:
// Morning = 08:00-12:00, Afternoon = 13:00-17:00, FullDay = 08:00-17:00.
public enum LeaveSession
{
    Morning,
    Afternoon,
    FullDay
}
