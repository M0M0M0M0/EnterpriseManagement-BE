using EnterpriseManagement.Application.DTOs;
using EnterpriseManagement.Domain.Entities.HR;
using EnterpriseManagement.Domain.Entities.Leave;

namespace EnterpriseManagement.Application.Interfaces;

public interface ILeaveBalanceService
{
    Task<IEnumerable<LeaveBalanceDto>> GetByEmployeeAsync(string employeeCode, int year);
    Task<LeaveBalanceDto> SetAllocatedTimeAsync(SetLeaveBalanceRequest request);

    // Lấy LeaveBalance của kỳ chứa targetDate cho (employee, leaveType); tự tạo với mức cấp
    // mặc định (theo LeaveType.AccrualPeriod) nếu chưa có. Dùng bởi LeaveRequestService khi
    // nộp đơn, để mọi loại nghỉ đều có hạn mức thay vì "chưa có hạn mức thì coi như không giới hạn".
    Task<LeaveBalance> GetOrCreateAsync(Employee employee, LeaveType leaveType, DateTime targetDate);
}
