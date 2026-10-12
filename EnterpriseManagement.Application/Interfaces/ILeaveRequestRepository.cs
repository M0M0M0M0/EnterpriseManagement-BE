using EnterpriseManagement.Domain.Entities.Leave;

namespace EnterpriseManagement.Application.Interfaces;

public interface ILeaveRequestRepository
{
    Task<IEnumerable<LeaveRequest>> GetApprovedUnpaidByEmployeeAndPeriodAsync(
        long employeeId, DateOnly periodStart, DateOnly periodEnd);

    // Đơn Pending/Approved của nhân viên trùng khoảng [start, end] — dùng để chặn nộp đơn
    // chồng lấn thời gian với đơn đã có (đã duyệt hoặc đang chờ duyệt).
    Task<IEnumerable<LeaveRequest>> GetActiveByEmployeeAndRangeAsync(
        long employeeId, DateTime start, DateTime end);

    Task<LeaveRequest?> GetByIdAsync(long id);
    Task<IEnumerable<LeaveRequest>> GetByEmployeeIdAsync(long employeeId);
    Task<IEnumerable<LeaveRequest>> GetPendingAsync();
    Task<IEnumerable<LeaveRequest>> GetAllAsync();
    Task AddAsync(LeaveRequest leaveRequest);
    Task<int> SaveChangesAsync();
}
