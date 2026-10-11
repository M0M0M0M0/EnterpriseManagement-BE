using EnterpriseManagement.Application.Common;

namespace EnterpriseManagement.Application.Interfaces;

public interface IEffectiveAttendanceRepository
{
    // Nạp mọi lớp dữ liệu cần để tính trạng thái hiệu lực của các nhân viên trong khoảng ngày [from, to]:
    // đơn nghỉ đã duyệt (theo buổi), các buổi đi làm lại đang có hiệu lực, ghi chú HR còn hiệu lực, chấm công thật
    // (chỉ bản ghi có giờ vào) và ngày vào làm. Một lần truy vấn cho nhiều nhân viên để tránh gọi từng người.
    Task<EffectiveInputs> LoadAsync(IReadOnlyCollection<long> employeeIds, DateOnly from, DateOnly to);
}
