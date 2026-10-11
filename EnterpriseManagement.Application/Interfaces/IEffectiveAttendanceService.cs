using EnterpriseManagement.Application.Common;
using EnterpriseManagement.Domain.Enums;

namespace EnterpriseManagement.Application.Interfaces;

// Nơi DUY NHẤT tính "ngày này của nhân viên này là gì" (đi làm, đi muộn, vắng, nghỉ phép). Lịch, dashboard, bảng công,
// cây tổ chức, kiểm tra trùng đơn, số dư phép... đều phải đọc qua đây, không đọc thẳng dữ liệu chấm công hay đơn nghỉ.
public interface IEffectiveAttendanceService
{
    Task<EffectiveDay> GetDayAsync(long employeeId, DateOnly date);

    // Toàn bộ các ngày trong khoảng (kể cả cuối tuần và ngày không có dữ liệu), dùng cho lịch.
    Task<List<EffectiveDay>> GetRangeAsync(long employeeId, DateOnly from, DateOnly to);

    // Chỉ các ngày có dữ liệu (chấm công thật, nghỉ phép, ghi chú, đi làm lại), mới nhất trước. Dùng cho danh sách lịch sử.
    Task<List<EffectiveDay>> GetDaysWithDataAsync(long employeeId);

    // Như trên nhưng cho nhiều nhân viên trong một khoảng ngày (bảng công theo phòng ban).
    Task<List<EffectiveDay>> GetDaysWithDataAsync(IReadOnlyCollection<long> employeeIds, DateOnly from, DateOnly to);

    // Trạng thái hiệu lực của một buổi.
    Task<SessionStatus> GetSessionStatusAsync(long employeeId, DateOnly date, DaySession session);

    // Nhân viên đang nghỉ phép hiệu lực ở buổi hiện tại (theo giờ Việt Nam) không.
    Task<bool> IsOnLeaveNowAsync(long employeeId);
}
