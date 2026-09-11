using EnterpriseManagement.Domain.Entities.Leave;

namespace EnterpriseManagement.Application.Interfaces;

public interface ILeaveBalanceRepository
{
    Task<LeaveBalance?> GetAsync(long employeeId, long leaveTypeId, int year, int? month);
    Task<IEnumerable<LeaveBalance>> GetByEmployeeAndYearAsync(long employeeId, int year);
    Task AddAsync(LeaveBalance balance);
    Task<int> SaveChangesAsync();
}
