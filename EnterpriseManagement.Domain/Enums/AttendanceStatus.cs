namespace EnterpriseManagement.Domain.Enums;

public enum AttendanceStatus
{
    Present,
    Late,
    Absent,
    HalfDay,
    OnLeave,

    // Không check-in buổi sáng và không có đơn nghỉ được duyệt cho buổi sáng đó — khác với
    // HalfDay (nghỉ nửa ngày CÓ phép) ở chỗ đây là tự ý vắng, cần quản lý chú ý riêng.
    HalfDayAbsent
}
