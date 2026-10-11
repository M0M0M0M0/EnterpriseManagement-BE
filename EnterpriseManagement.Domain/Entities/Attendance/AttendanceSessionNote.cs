using EnterpriseManagement.Domain.Entities.HR;
using EnterpriseManagement.Domain.Enums;

namespace EnterpriseManagement.Domain.Entities.Attendance;

// Ghi chú ngoại lệ chấm công do người có quyền (vd HR) ghi theo từng buổi: đi làm đúng giờ, đi muộn hoặc vắng.
// Không sửa dữ liệu chấm công gốc; khi tính ngày công, ghi chú còn hiệu lực được ưu tiên hơn chấm công gốc.
public class AttendanceSessionNote
{
    public long Id { get; set; }

    public long EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public DateOnly Date { get; set; }
    public DaySession Session { get; set; }

    public SessionStatus Status { get; set; }
    public string Note { get; set; } = string.Empty;

    public long CreatedBy { get; set; }
    public Employee Creator { get; set; } = null!;
    public DateTime CreatedAt { get; set; }

    // Có giá trị nghĩa là ghi chú đã bị thu hồi (không còn hiệu lực), vẫn giữ lại làm lịch sử.
    public long? RevokedBy { get; set; }
    public Employee? Revoker { get; set; }
    public DateTime? RevokedAt { get; set; }
}
