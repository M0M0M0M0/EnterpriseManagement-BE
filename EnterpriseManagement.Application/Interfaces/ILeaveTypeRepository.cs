using EnterpriseManagement.Domain.Entities.Leave;

namespace EnterpriseManagement.Application.Interfaces;

public interface ILeaveTypeRepository
{
    Task<LeaveType?> GetByCodeAsync(string leaveTypeCode);
    Task<LeaveType?> GetByIdAsync(long id);
    Task<IEnumerable<LeaveType>> GetAllAsync();
    Task AddAsync(LeaveType leaveType);
    void Remove(LeaveType leaveType);
    // Đã có đơn nghỉ hoặc số dư gắn với loại này chưa (có rồi thì không được xoá, chỉ tắt).
    Task<bool> IsInUseAsync(long leaveTypeId);
    Task<int> SaveChangesAsync();
}
