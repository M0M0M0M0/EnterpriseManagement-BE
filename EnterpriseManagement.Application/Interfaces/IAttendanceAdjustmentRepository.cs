using EnterpriseManagement.Domain.Entities.Attendance;

namespace EnterpriseManagement.Application.Interfaces;

public interface IAttendanceAdjustmentRepository
{
    Task<AttendanceAdjustment?> GetByIdAsync(long id);
    Task<IEnumerable<AttendanceAdjustment>> GetPendingAsync();
    Task<IEnumerable<AttendanceAdjustment>> GetByEmployeeIdAsync(long employeeId);

    // Các yêu cầu Manager đã duyệt, đang chờ người xử lý (HR) áp dụng.
    Task<IEnumerable<AttendanceAdjustment>> GetPendingHrAsync();

    // Nhân viên đã có yêu cầu điều chỉnh chưa xử lý xong (chờ Manager hoặc chờ HR) cho ngày này chưa.
    Task<bool> HasOpenAsync(long employeeId, DateOnly date);
    Task AddAsync(AttendanceAdjustment adjustment);
    Task<int> SaveChangesAsync();
}
