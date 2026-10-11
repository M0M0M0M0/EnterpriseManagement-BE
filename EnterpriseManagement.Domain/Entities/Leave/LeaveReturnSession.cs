using EnterpriseManagement.Domain.Enums;

namespace EnterpriseManagement.Domain.Entities.Leave;

// Một buổi bị gỡ khỏi một đơn nghỉ cụ thể. Gắn với đơn bằng LeaveRequestId nên chỉ tác động lên đúng đơn đó,
// không che đơn nghỉ khác nằm cùng ngày.
public class LeaveReturnSession
{
    public long Id { get; set; }

    public long ReturnRequestId { get; set; }
    public LeaveReturnRequest ReturnRequest { get; set; } = null!;

    public long LeaveRequestId { get; set; }
    public LeaveRequest LeaveRequest { get; set; } = null!;

    public DateOnly Date { get; set; }
    public DaySession Session { get; set; }

    public ReturnSessionState State { get; set; }

    // Trạng thái đi làm do HR xác nhận (đúng giờ hay đi muộn) khi xử lý buổi đã bắt đầu. Null với buổi tự có hiệu lực
    // lúc Manager duyệt; khi đó trạng thái lấy từ chấm công thật.
    public SessionStatus? WorkedStatus { get; set; }
}
