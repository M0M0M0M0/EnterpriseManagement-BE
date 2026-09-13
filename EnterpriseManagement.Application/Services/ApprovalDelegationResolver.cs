using EnterpriseManagement.Application.Common;
using EnterpriseManagement.Application.Interfaces;
using EnterpriseManagement.Domain.Entities.HR;

namespace EnterpriseManagement.Application.Services;

public class ApprovalDelegationResolver : IApprovalDelegationResolver
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ILeaveRequestRepository _leaveRequestRepository;
    private readonly IAttendanceRepository _attendanceRepository;

    public ApprovalDelegationResolver(
        IEmployeeRepository employeeRepository,
        ILeaveRequestRepository leaveRequestRepository,
        IAttendanceRepository attendanceRepository)
    {
        _employeeRepository = employeeRepository;
        _leaveRequestRepository = leaveRequestRepository;
        _attendanceRepository = attendanceRepository;
    }

    public async Task<Employee?> ResolveApproverAsync(Employee employee)
    {
        if (employee.ManagerId is null)
        {
            return null;
        }

        var current = await _employeeRepository.GetByIdAsync(employee.ManagerId.Value)
            ?? throw new InvalidOperationException($"Manager {employee.ManagerId} not found.");

        // Đi lên tiếp khi người hiện tại đang vắng mặt VÀ còn quản lý cấp trên để đẩy tới —
        // top-level (không có ManagerId) thì dừng lại tại đó dù có đang nghỉ, vì không còn ai
        // để đẩy lên nữa.
        while (current.ManagerId is not null && await IsDelegatingAsync(current))
        {
            current = await _employeeRepository.GetByIdAsync(current.ManagerId.Value)
                ?? throw new InvalidOperationException($"Manager {current.ManagerId} not found.");
        }

        return current;
    }

    // Quản lý đang "vắng mặt" = có đơn nghỉ Approved bao trùm thời điểm hiện tại VÀ chưa
    // check-in đi làm hôm nay. Tính lại mỗi lần gọi (không lưu trạng thái riêng), nên khi quản
    // lý check-in trở lại thì việc duyệt tự động quay về họ ngay mà không cần dọn dẹp gì thêm.
    private async Task<bool> IsDelegatingAsync(Employee manager)
    {
        var onApprovedLeave = await _leaveRequestRepository.HasApprovedLeaveAtAsync(manager.Id, VietnamClock.Now);
        if (!onApprovedLeave)
        {
            return false;
        }

        var attendance = await _attendanceRepository.GetByEmployeeAndDateAsync(manager.Id, VietnamClock.Today);
        return attendance?.CheckInTime is null;
    }
}
