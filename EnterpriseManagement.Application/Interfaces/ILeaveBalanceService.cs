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
    // Phần phép được hoàn cho số dư này do các buổi đi làm lại đang có hiệu lực (tính lúc đọc, không sửa số dư đã lưu).
    // Số dư hiệu lực = RemainingTime lưu trong DB + giá trị này.
    Task<decimal> GetRefundedTimeAsync(LeaveBalance balance);

    Task<LeaveBalance> GetOrCreateAsync(Employee employee, LeaveType leaveType, DateTime targetDate);
}
