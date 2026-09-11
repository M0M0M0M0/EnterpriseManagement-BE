using EnterpriseManagement.Application.Common;
using EnterpriseManagement.Application.DTOs;
using EnterpriseManagement.Application.Interfaces;
using EnterpriseManagement.Domain.Enums;

namespace EnterpriseManagement.Application.Services;

public class PayrollCalculationService : IPayrollCalculationService
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ISalaryStructureRepository _salaryStructureRepository;
    private readonly IAttendanceRepository _attendanceRepository;
    private readonly ILeaveRequestRepository _leaveRequestRepository;

    public PayrollCalculationService(
        IEmployeeRepository employeeRepository,
        ISalaryStructureRepository salaryStructureRepository,
        IAttendanceRepository attendanceRepository,
        ILeaveRequestRepository leaveRequestRepository)
    {
        _employeeRepository = employeeRepository;
        _salaryStructureRepository = salaryStructureRepository;
        _attendanceRepository = attendanceRepository;
        _leaveRequestRepository = leaveRequestRepository;
    }

    public async Task<SalaryCalculationResult> CalculateAsync(string employeeCode, int year, int month)
    {
        var employee = await _employeeRepository.GetByEmployeeCodeAsync(employeeCode)
            ?? throw new InvalidOperationException($"Employee code '{employeeCode}' not found.");

        var salaryStructure = await _salaryStructureRepository.GetActiveByEmployeeIdAsync(employee.Id)
            ?? throw new InvalidOperationException($"Employee '{employeeCode}' has no active salary structure.");

        var periodStart = new DateOnly(year, month, 1);
        var periodEnd = new DateOnly(year, month, DateTime.DaysInMonth(year, month));

        var standardWorkingDays = DateRangeHelper.CountWeekdays(periodStart, periodEnd);

        // Ngày bị trừ lương = ngày trong tuần (Thứ 2-6) có Attendance Absent, hợp nhất với ngày nghỉ không lương
        // đã duyệt, để tránh đếm trùng nếu 1 ngày rơi vào cả 2 trường hợp.
        var deductedDates = new HashSet<DateOnly>();

        var attendanceRecords = await _attendanceRepository.GetByEmployeeAndPeriodAsync(employee.Id, periodStart, periodEnd);
        foreach (var record in attendanceRecords)
        {
            if (record.Status == AttendanceStatus.Absent && DateRangeHelper.IsWeekday(record.AttendanceDate))
            {
                deductedDates.Add(record.AttendanceDate);
            }
        }

        var unpaidLeaves = await _leaveRequestRepository.GetApprovedUnpaidByEmployeeAndPeriodAsync(employee.Id, periodStart, periodEnd);
        foreach (var leave in unpaidLeaves)
        {
            var leaveStart = DateOnly.FromDateTime(leave.StartDate);
            var leaveEnd = DateOnly.FromDateTime(leave.EndDate);
            var overlapStart = leaveStart > periodStart ? leaveStart : periodStart;
            var overlapEnd = leaveEnd < periodEnd ? leaveEnd : periodEnd;
            for (var date = overlapStart; date <= overlapEnd; date = date.AddDays(1))
            {
                if (DateRangeHelper.IsWeekday(date))
                {
                    deductedDates.Add(date);
                }
            }
        }

        var dailyRate = standardWorkingDays == 0 ? 0 : salaryStructure.BaseSalary / standardWorkingDays;
        var deductionAmount = dailyRate * deductedDates.Count;
        var netSalary = salaryStructure.BaseSalary - deductionAmount;

        return new SalaryCalculationResult
        {
            EmployeeCode = employee.EmployeeCode,
            Year = year,
            Month = month,
            BaseSalary = salaryStructure.BaseSalary,
            StandardWorkingDays = standardWorkingDays,
            DeductedDays = deductedDates.Count,
            DailyRate = dailyRate,
            DeductionAmount = deductionAmount,
            NetSalary = netSalary
        };
    }
}
