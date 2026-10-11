using EnterpriseManagement.Domain.Entities.Leave;
using EnterpriseManagement.Domain.Enums;

namespace EnterpriseManagement.Application.Interfaces;

public interface ILeaveReturnRepository
{
    Task<LeaveReturnRequest?> GetByIdAsync(long id);
    Task<IEnumerable<LeaveReturnRequest>> GetByEmployeeAsync(long employeeId);
    Task<IEnumerable<LeaveReturnRequest>> GetByStatusAsync(ReturnRequestStatus status);

    // Nhân viên đang có yêu cầu đi làm lại chưa xử lý xong (chờ Manager hoặc chờ HR) không.
    Task<bool> HasOpenRequestAsync(long employeeId);

    // Các buổi đi làm lại đang có hiệu lực của nhân viên theo loại nghỉ, dùng tính phần phép hoàn.
    Task<IEnumerable<LeaveReturnSession>> GetActiveSessionsAsync(long employeeId, long leaveTypeId);

    // Các buổi đi làm lại đang có hiệu lực của một tập đơn nghỉ, dùng khi kiểm tra trùng đơn.
    Task<IEnumerable<LeaveReturnSession>> GetActiveByLeaveRequestIdsAsync(IReadOnlyCollection<long> leaveRequestIds);

    Task AddAsync(LeaveReturnRequest request);
    Task<int> SaveChangesAsync();
}
