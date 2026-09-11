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

    public LeaveBalanceService(
        ILeaveBalanceRepository leaveBalanceRepository,
        IEmployeeRepository employeeRepository,
        ILeaveTypeRepository leaveTypeRepository)
    {
        _leaveBalanceRepository = leaveBalanceRepository;
        _employeeRepository = employeeRepository;
        _leaveTypeRepository = leaveTypeRepository;
    }

    public async Task<IEnumerable<LeaveBalanceDto>> GetByEmployeeAsync(string employeeCode, int year)
    {
        var employee = await _employeeRepository.GetByEmployeeCodeAsync(employeeCode)
            ?? throw new InvalidOperationException($"Employee code '{employeeCode}' not found.");

        var leaveTypes = (await _leaveTypeRepository.GetAllAsync()).Where(lt => lt.IsActive);
        var now = DateTime.UtcNow;
        var targetDate = year == now.Year ? now : new DateTime(year, 1, 1);

        var balances = new List<LeaveBalance>();
        foreach (var leaveType in leaveTypes)
        {
            balances.Add(await GetOrCreateAsync(employee, leaveType, targetDate));
        }

        return balances.Select(ToDto);
    }

    public async Task<LeaveBalanceDto> SetAllocatedTimeAsync(SetLeaveBalanceRequest request)
    {
        var employee = await _employeeRepository.GetByEmployeeCodeAsync(request.EmployeeCode)
            ?? throw new InvalidOperationException($"Employee code '{request.EmployeeCode}' not found.");

        var leaveType = await _leaveTypeRepository.GetByCodeAsync(request.LeaveTypeCode)
            ?? throw new InvalidOperationException($"Leave type code '{request.LeaveTypeCode}' not found.");

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
                CreatedAt = DateTime.UtcNow
            };
            await _leaveBalanceRepository.AddAsync(balance);
        }
        else
        {
            var delta = request.AllocatedTime - balance.AllocatedTime;
            balance.AllocatedTime = request.AllocatedTime;
            balance.RemainingTime += delta;
            balance.UpdatedAt = DateTime.UtcNow;
        }

        await _leaveBalanceRepository.SaveChangesAsync();

        balance.LeaveType = leaveType;
        return ToDto(balance);
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
            CreatedAt = DateTime.UtcNow
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

    private static LeaveBalanceDto ToDto(LeaveBalance balance) => new()
    {
        LeaveTypeCode = balance.LeaveType.LeaveTypeCode,
        LeaveTypeName = balance.LeaveType.LeaveTypeName,
        Year = balance.Year,
        Month = balance.Month,
        Unit = balance.Unit.ToString(),
        AllocatedTime = balance.AllocatedTime,
        UsedTime = balance.UsedTime,
        RemainingTime = balance.RemainingTime
    };
}
