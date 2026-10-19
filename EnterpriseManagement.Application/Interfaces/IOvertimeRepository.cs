using EnterpriseManagement.Domain.Entities.Attendance;

namespace EnterpriseManagement.Application.Interfaces;

public interface IOvertimeRepository
{
    Task<OvertimeRequest?> GetByIdAsync(long id);
    Task<List<OvertimeRequest>> GetByEmployeeAsync(long employeeId);
    Task<List<OvertimeRequest>> GetPendingAsync();
    Task<List<OvertimeRequest>> GetAllAsync();

    // Đơn Pending/Approved của nhân viên trong ngày (chặn trùng giờ).
    Task<List<OvertimeRequest>> GetActiveByEmployeeAndDateAsync(long employeeId, DateOnly date);

    // Tổng phút OT Pending/Approved của nhân viên trong khoảng ngày (kiểm tra hạn mức tháng/năm), có thể loại trừ một đơn.
    Task<int> SumActiveMinutesAsync(long employeeId, DateOnly from, DateOnly to, long? excludeId = null);

    Task AddAsync(OvertimeRequest request);
    Task<int> SaveChangesAsync();
}
