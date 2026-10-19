using EnterpriseManagement.Domain.Entities.Leave;

namespace EnterpriseManagement.Application.Interfaces;

public interface ILeaveCancelRepository
{
    Task<LeaveCancelRequest?> GetByIdAsync(long id);
    Task<List<LeaveCancelRequest>> GetByEmployeeAsync(long employeeId);
    Task<List<LeaveCancelRequest>> GetPendingAsync();
    Task<List<LeaveCancelRequest>> GetAllAsync();

    // Đơn nghỉ này đang có yêu cầu hủy chờ duyệt không.
    Task<bool> HasPendingAsync(long leaveRequestId);

    Task AddAsync(LeaveCancelRequest request);
    Task<int> SaveChangesAsync();
}
