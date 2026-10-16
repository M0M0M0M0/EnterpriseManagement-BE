using EnterpriseManagement.Application.Common;
using EnterpriseManagement.Application.DTOs;
using EnterpriseManagement.Application.Interfaces;
using EnterpriseManagement.Domain.Entities.HR;
using EnterpriseManagement.Domain.Entities.Leave;
using EnterpriseManagement.Domain.Enums;

namespace EnterpriseManagement.Application.Services;

public class LeaveBalanceService : ILeaveBalanceService
{
    private readonly ILeaveBalanceRepository _leaveBalanceRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ILeaveTypeRepository _leaveTypeRepository;
    private readonly ILeaveReturnRepository _leaveReturnRepository;

    public LeaveBalanceService(
        ILeaveBalanceRepository leaveBalanceRepository,
        IEmployeeRepository employeeRepository,
        ILeaveTypeRepository leaveTypeRepository,
        ILeaveReturnRepository leaveReturnRepository)
    {
        _leaveBalanceRepository = leaveBalanceRepository;
        _employeeRepository = employeeRepository;
        _leaveTypeRepository = leaveTypeRepository;
        _leaveReturnRepository = leaveReturnRepository;
    }

    public async Task<IEnumerable<LeaveBalanceDto>> GetByEmployeeAsync(string employeeCode, int year)
    {
        var employee = await _employeeRepository.GetByEmployeeCodeAsync(employeeCode)
            ?? throw new InvalidOperationException($"Không tìm thấy nhân viên có mã '{employeeCode}'.");

        // Tài khoản hệ thống không dùng nghỉ phép: không tạo số dư phép.
        if (!employee.TracksAttendance) return Array.Empty<LeaveBalanceDto>();

        // Loại nghỉ không lương (IsPaid = false) không có số dư: không cấp, không trừ, không hiện trong danh sách số dư.
        var leaveTypes = (await _leaveTypeRepository.GetAllAsync()).Where(lt => lt.IsActive && lt.IsPaid);
        var now = VietnamClock.Now;
        var targetDate = year == now.Year ? now : new DateTime(year, 1, 1);

        var balances = new List<LeaveBalance>();
        foreach (var leaveType in leaveTypes)
        {
            balances.Add(await GetOrCreateAsync(employee, leaveType, targetDate));
        }

        var result = new List<LeaveBalanceDto>();
        foreach (var balance in balances)
        {
            result.Add(await ToDtoAsync(balance));
        }

        return result;
    }

    public async Task<LeaveBalanceDto> SetAllocatedTimeAsync(SetLeaveBalanceRequest request)
    {
        var employee = await _employeeRepository.GetByEmployeeCodeAsync(request.EmployeeCode)
            ?? throw new InvalidOperationException($"Không tìm thấy nhân viên có mã '{request.EmployeeCode}'.");

        if (!employee.TracksAttendance)
        {
            throw new InvalidOperationException("Tài khoản hệ thống không dùng chức năng nghỉ phép.");
        }

        var leaveType = await _leaveTypeRepository.GetByCodeAsync(request.LeaveTypeCode)
            ?? throw new InvalidOperationException($"Không tìm thấy loại nghỉ có mã '{request.LeaveTypeCode}'.");

        var month = leaveType.AccrualPeriod == LeaveAccrualPeriod.MonthlyReset ? request.Month : null;

        var balance = await _leaveBalanceRepository.GetAsync(employee.Id, leaveType.Id, request.Year, month);
        if (balance is null)
        {
            balance = new LeaveBalance
            {
                EmployeeId = employee.Id,
                LeaveTypeId = leaveType.Id,
                Year = request.Year,
                Month = month,
                Unit = leaveType.AccrualUnit,
                AllocatedTime = request.AllocatedTime,
                UsedTime = 0,
                RemainingTime = request.AllocatedTime,
                CreatedAt = VietnamClock.Now
            };
            await _leaveBalanceRepository.AddAsync(balance);
        }
        else
        {
            var delta = request.AllocatedTime - balance.AllocatedTime;
            balance.AllocatedTime = request.AllocatedTime;
            balance.RemainingTime += delta;
            balance.UpdatedAt = VietnamClock.Now;
        }

        await _leaveBalanceRepository.SaveChangesAsync();

        balance.LeaveType = leaveType;
        return await ToDtoAsync(balance);
    }

    public async Task<LeaveBalance> GetOrCreateAsync(Employee employee, LeaveType leaveType, DateTime targetDate)
    {
        var year = targetDate.Year;
        var month = leaveType.AccrualPeriod == LeaveAccrualPeriod.MonthlyReset ? targetDate.Month : (int?)null;

        var existing = await _leaveBalanceRepository.GetAsync(employee.Id, leaveType.Id, year, month);
        if (existing is not null)
        {
            existing.LeaveType = leaveType;
            return existing;
        }

        var allocated = CalculateDefaultAllocation(leaveType, employee.CreatedAt, year);
        var balance = new LeaveBalance
        {
            EmployeeId = employee.Id,
            LeaveTypeId = leaveType.Id,
            LeaveType = leaveType,
            Year = year,
            Month = month,
            Unit = leaveType.AccrualUnit,
            AllocatedTime = allocated,
            UsedTime = 0,
            RemainingTime = allocated,
            CreatedAt = VietnamClock.Now
        };

        await _leaveBalanceRepository.AddAsync(balance);
        await _leaveBalanceRepository.SaveChangesAsync();

        return balance;
    }

    // Xem EnterpriseManagement.Domain.Enums.LeaveAccrualPeriod để biết ý nghĩa từng nhánh.
    private static decimal CalculateDefaultAllocation(LeaveType leaveType, DateTime employeeCreatedAt, int year)
    {
        switch (leaveType.AccrualPeriod)
        {
            case LeaveAccrualPeriod.MonthlyReset:
                return leaveType.AccrualAmount;

            case LeaveAccrualPeriod.FlatYearly:
                return leaveType.AccrualAmount;

            case LeaveAccrualPeriod.ProratedYearly:
                if (year < employeeCreatedAt.Year) return 0;
                var monthsRemaining = year == employeeCreatedAt.Year ? 13 - employeeCreatedAt.Month : 12;
                return leaveType.AccrualAmount * monthsRemaining;

            default:
                return 0;
        }
    }

    // Mỗi buổi đi làm lại đang có hiệu lực của một đơn nghỉ đã duyệt hoàn lại 0,5 ngày, vào đúng số dư mà đơn đó đã bị trừ
    // (số dư của kỳ chứa ngày bắt đầu đơn). Cùng cách tính thời gian như lúc trừ: cả ngày = 2 buổi = 1 ngày, nửa ngày = 0,5.
    // Chỉ áp dụng cho loại nghỉ tính theo ngày.
    public async Task<decimal> GetRefundedTimeAsync(LeaveBalance balance)
    {
        if (balance.Unit != LeaveUnit.Days) return 0;

        var sessions = await _leaveReturnRepository.GetActiveSessionsAsync(balance.EmployeeId, balance.LeaveTypeId);
        var count = sessions.Count(s =>
            s.LeaveRequest.StartDate.Year == balance.Year
            && (balance.Month is null || s.LeaveRequest.StartDate.Month == balance.Month));

        return count * 0.5m;
    }

    private async Task<LeaveBalanceDto> ToDtoAsync(LeaveBalance balance)
    {
        var refunded = await GetRefundedTimeAsync(balance);
        return new LeaveBalanceDto
        {
            LeaveTypeCode = balance.LeaveType.LeaveTypeCode,
            LeaveTypeName = balance.LeaveType.LeaveTypeName,
            Year = balance.Year,
            Month = balance.Month,
            Unit = balance.Unit.ToString(),
            AllocatedTime = balance.AllocatedTime,
            UsedTime = balance.UsedTime - refunded,
            RemainingTime = balance.RemainingTime + refunded,
            RefundedTime = refunded
        };
    }
}
