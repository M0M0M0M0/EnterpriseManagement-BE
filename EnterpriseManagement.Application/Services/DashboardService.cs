using EnterpriseManagement.Application.Common;
using EnterpriseManagement.Application.DTOs;
using EnterpriseManagement.Application.Interfaces;
using EnterpriseManagement.Domain.Enums;

namespace EnterpriseManagement.Application.Services;

public class DashboardService : IDashboardService
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IEffectiveAttendanceService _effective;
    private readonly IAttendanceAdjustmentRepository _attendanceAdjustmentRepository;
    private readonly ILeaveBalanceService _leaveBalanceService;
    private readonly ILeaveRequestRepository _leaveRequestRepository;

    public DashboardService(
        IEmployeeRepository employeeRepository,
        IEffectiveAttendanceService effective,
        IAttendanceAdjustmentRepository attendanceAdjustmentRepository,
        ILeaveBalanceService leaveBalanceService,
        ILeaveRequestRepository leaveRequestRepository)
    {
        _employeeRepository = employeeRepository;
        _effective = effective;
        _attendanceAdjustmentRepository = attendanceAdjustmentRepository;
        _leaveBalanceService = leaveBalanceService;
        _leaveRequestRepository = leaveRequestRepository;
    }

    public async Task<EmployeeDashboardDto> GetEmployeeDashboardAsync(string employeeCode)
    {
        var employee = await _employeeRepository.GetByEmployeeCodeAsync(employeeCode)
            ?? throw new InvalidOperationException($"Không tìm thấy nhân viên có mã '{employeeCode}'.");

        var today = VietnamClock.Today;
        var todayDay = await _effective.GetDayAsync(employee.Id, today);

        // Số dư hiệu lực (đã tính phần phép được hoàn do đi làm lại), không đọc thẳng số dư lưu trong DB.
        var balances = (await _leaveBalanceService.GetByEmployeeAsync(employeeCode, today.Year)).ToList();
        var pendingLeaves = (await _leaveRequestRepository.GetByEmployeeIdAsync(employee.Id))
            .Count(l => l.Status == LeaveRequestStatus.Pending);
        var pendingAdjustments = (await _attendanceAdjustmentRepository.GetByEmployeeIdAsync(employee.Id))
            .Count(a => a.Status is ApprovalStatus.Pending or ApprovalStatus.PendingHr);

        return new EmployeeDashboardDto
        {
            EmployeeCode = employee.EmployeeCode,
            EmployeeName = $"{employee.FirstName} {employee.LastName}",
            TodayAttendance = todayDay.HasData ? AttendanceMapper.ToRecordDto(todayDay, employee) : null,
            // Chỉ cộng các loại nghỉ tính theo ngày; loại tính theo giờ (Nghỉ ngắn) không
            // cùng đơn vị nên không gộp chung vào tổng "ngày phép còn lại" này.
            TotalRemainingLeaveDays = balances.Where(b => b.Unit == LeaveUnit.Days.ToString()).Sum(b => b.RemainingTime),
            PendingLeaveRequestsCount = pendingLeaves,
            PendingAttendanceAdjustmentsCount = pendingAdjustments
        };
    }

    public async Task<ManagerDashboardDto> GetManagerDashboardAsync(string managerEmployeeCode)
    {
        var manager = await _employeeRepository.GetByEmployeeCodeAsync(managerEmployeeCode)
            ?? throw new InvalidOperationException($"Không tìm thấy nhân viên có mã '{managerEmployeeCode}'.");

        var team = (await _employeeRepository.GetByManagerIdAsync(manager.Id)).ToList();
        var teamIds = team.Select(e => e.Id).ToHashSet();

        var pendingLeaves = (await _leaveRequestRepository.GetPendingAsync())
            .Count(l => teamIds.Contains(l.EmployeeId));
        var pendingAdjustments = (await _attendanceAdjustmentRepository.GetPendingAsync())
            .Count(a => teamIds.Contains(a.RequestedBy));

        var today = VietnamClock.Today;

        var presentToday = 0;
        var onLeaveToday = 0;
        var absentToday = 0;

        foreach (var member in team)
        {
            var day = await _effective.GetDayAsync(member.Id, today);
            var worked = day.Morning is SessionStatus.OnTime or SessionStatus.Late
                || day.Afternoon is SessionStatus.OnTime or SessionStatus.Late;
            var fullyOnLeave = day.Morning == SessionStatus.OnLeave && day.Afternoon == SessionStatus.OnLeave;

            // Có mặt = đã có buổi đi làm (kể cả đi muộn); nghỉ phép cả ngày tính riêng; còn lại là chưa có mặt.
            if (worked) presentToday++;
            else if (fullyOnLeave) onLeaveToday++;
            else absentToday++;
        }

        return new ManagerDashboardDto
        {
            ManagerCode = manager.EmployeeCode,
            TeamSize = team.Count,
            PendingLeaveRequestsCount = pendingLeaves,
            PendingAttendanceAdjustmentsCount = pendingAdjustments,
            TeamPresentTodayCount = presentToday,
            TeamOnLeaveTodayCount = onLeaveToday,
            TeamAbsentTodayCount = absentToday
        };
    }
}
