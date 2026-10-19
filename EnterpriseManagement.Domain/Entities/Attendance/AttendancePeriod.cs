using EnterpriseManagement.Domain.Entities.HR;

namespace EnterpriseManagement.Domain.Entities.Attendance;

// Một tháng công đã chốt: sau khi chốt không ai được thêm/sửa dữ liệu công của tháng đó (điều chỉnh, ghi nhận, nghỉ phép, OT...).
// Mở lại được (xoá dòng), mọi lần chốt/mở đều ghi nhật ký.
public class AttendancePeriod
{
    public long Id { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }

    public long ClosedBy { get; set; }
    public Employee Closer { get; set; } = null!;
    public DateTime ClosedAt { get; set; }
}
