using EnterpriseManagement.Application.DTOs;
using EnterpriseManagement.Application.Interfaces;
using EnterpriseManagement.Domain.Enums;

namespace EnterpriseManagement.Application.Services;

public class DashboardService : IDashboardService
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IAttendanceRepository _attendanceRepository;
    private readonly IAttendanceAdjustmentRepository _attendanceAdjustmentRepository;
    private readonly ILeaveBalanceRepository _leaveBalanceRepository;
    private readonly ILeaveRequestRepository _leaveRequestRepository;
    private readonly ISaleRepository _saleRepository;

    public DashboardService(
        IEmployeeRepository employeeRepository,
        IAttendanceRepository attendanceRepository,
        IAttendanceAdjustmentRepository attendanceAdjustmentRepository,
        ILeaveBalanceRepository leaveBalanceRepository,
        ILeaveRequestRepository leaveRequestRepository,
        ISaleRepository saleRepository)
    {
        _employeeRepository = employeeRepository;
        _attendanceRepository = attendanceRepository;
        _attendanceAdjustmentRepository = attendanceAdjustmentRepository;
        _leaveBalanceRepository = leaveBalanceRepository;
        _leaveRequestRepository = leaveRequestRepository;
        _saleRepository = saleRepository;
    }

    public async Task<EmployeeDashboardDto> GetEmployeeDashboardAsync(string employeeCode)
    {
        var employee = await _employeeRepository.GetByEmployeeCodeAsync(employeeCode)
            ?? throw new InvalidOperationException($"Employee code '{employeeCode}' not found.");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var todayRecord = await _attendanceRepository.GetByEmployeeAndDateAsync(employee.Id, today);

        var balances = await _leaveBalanceRepository.GetByEmployeeAndYearAsync(employee.Id, today.Year);
        var pendingLeaves = (await _leaveRequestRepository.GetByEmployeeIdAsync(employee.Id))
            .Count(l => l.Status == LeaveRequestStatus.Pending);
        var pendingSales = (await _saleRepository.GetByEmployeeIdAsync(employee.Id))
            .Count(s => s.Status == SaleStatus.Pending);
        var pendingAdjustments = (await _attendanceAdjustmentRepository.GetByEmployeeIdAsync(employee.Id))
            .Count(a => a.Status == ApprovalStatus.Pending);

        return new EmployeeDashboardDto
        {
            EmployeeCode = employee.EmployeeCode,
            EmployeeName = $"{employee.FirstName} {employee.LastName}",
            TodayAttendance = todayRecord is null ? null : new AttendanceRecordDto
            {
                EmployeeCode = employee.EmployeeCode,
                EmployeeName = $"{employee.FirstName} {employee.LastName}",
                AttendanceDate = todayRecord.AttendanceDate,
                CheckInTime = todayRecord.CheckInTime,
                CheckOutTime = todayRecord.CheckOutTime,
                WorkingHours = todayRecord.WorkingHours,
                Status = todayRecord.Status.ToString()
            },
            // Chỉ cộng các loại nghỉ tính theo ngày; loại tính theo giờ (Nghỉ ngắn) không
            // cùng đơn vị nên không gộp chung vào tổng "ngày phép còn lại" này.
            TotalRemainingLeaveDays = balances.Where(b => b.Unit == LeaveUnit.Days).Sum(b => b.RemainingTime),
            PendingLeaveRequestsCount = pendingLeaves,
            PendingSalesCount = pendingSales,
            PendingAttendanceAdjustmentsCount = pendingAdjustments
        };
    }

    public async Task<ManagerDashboardDto> GetManagerDashboardAsync(string managerEmployeeCode)
    {
        var manager = await _employeeRepository.GetByEmployeeCodeAsync(managerEmployeeCode)
            ?? throw new InvalidOperationException($"Employee code '{managerEmployeeCode}' not found.");

        var team = (await _employeeRepository.GetByManagerIdAsync(manager.Id)).ToList();
        var teamIds = team.Select(e => e.Id).ToHashSet();

        var pendingLeaves = (await _leaveRequestRepository.GetPendingAsync())
            .Count(l => teamIds.Contains(l.EmployeeId));
        var pendingSales = (await _saleRepository.GetPendingAsync())
            .Count(s => teamIds.Contains(s.EmployeeId));
        var pendingAdjustments = (await _attendanceAdjustmentRepository.GetPendingAsync())
            .Count(a => teamIds.Contains(a.RequestedBy));

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var monthStart = new DateOnly(today.Year, today.Month, 1);

        decimal monthlyRevenue = 0;
        var presentToday = 0;
        var absentToday = 0;

        foreach (var member in team)
        {
            var sales = await _saleRepository.GetByEmployeeIdAsync(member.Id);
            monthlyRevenue += sales
                .Where(s => s.Status == SaleStatus.Confirmed && DateOnly.FromDateTime(s.OrderDate) >= monthStart)
                .Sum(s => s.Amount);

            var todayRecord = await _attendanceRepository.GetByEmployeeAndDateAsync(member.Id, today);
            if (todayRecord is not null && todayRecord.Status == AttendanceStatus.Present)
            {
                presentToday++;
            }
            else
            {
                absentToday++;
            }
        }

        return new ManagerDashboardDto
        {
            ManagerCode = manager.EmployeeCode,
            TeamSize = team.Count,
            PendingLeaveRequestsCount = pendingLeaves,
            PendingSalesCount = pendingSales,
            PendingAttendanceAdjustmentsCount = pendingAdjustments,
            TeamMonthlyRevenue = monthlyRevenue,
            TeamPresentTodayCount = presentToday,
            TeamAbsentTodayCount = absentToday
        };
    }
}
