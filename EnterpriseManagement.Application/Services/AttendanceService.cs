using EnterpriseManagement.Application.Common;
using EnterpriseManagement.Application.DTOs;
using EnterpriseManagement.Application.Interfaces;
using EnterpriseManagement.Domain.Entities.Attendance;
using EnterpriseManagement.Domain.Entities.HR;
using EnterpriseManagement.Domain.Enums;

namespace EnterpriseManagement.Application.Services;

public class AttendanceService : IAttendanceService
{
    private readonly IAttendanceRepository _attendanceRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IDepartmentRepository _departmentRepository;

    public AttendanceService(
        IAttendanceRepository attendanceRepository,
        IEmployeeRepository employeeRepository,
        IDepartmentRepository departmentRepository)
    {
        _attendanceRepository = attendanceRepository;
        _employeeRepository = employeeRepository;
        _departmentRepository = departmentRepository;
    }

    public async Task<AttendanceRecordDto> PunchAsync(string employeeCode)
    {
        var employee = await _employeeRepository.GetByEmployeeCodeAsync(employeeCode)
            ?? throw new InvalidOperationException($"Employee code '{employeeCode}' not found.");

        var today = VietnamClock.Today;
        var record = await _attendanceRepository.GetByEmployeeAndDateAsync(employee.Id, today);
        var now = VietnamClock.Now;

        if (record is null)
        {
            // Lần punch đầu tiên trong ngày = check-in.
            record = new AttendanceRecord
            {
                EmployeeId = employee.Id,
                AttendanceDate = today,
                CheckInTime = now,
                Status = AttendanceStatus.Present,
                CreatedAt = now
            };
            await _attendanceRepository.AddAsync(record);
        }
        else
        {
            // Các lần punch sau trong cùng ngày = check-out, đè lên checkout trước đó nếu có.
            record.CheckOutTime = now;
            record.WorkingHours = (decimal)(now - record.CheckInTime!.Value).TotalHours;
            record.UpdatedAt = now;
        }

        await _attendanceRepository.SaveChangesAsync();

        return ToDto(record, employee);
    }

    public async Task<IEnumerable<AttendanceRecordDto>> GetHistoryAsync(string employeeCode)
    {
        var employee = await _employeeRepository.GetByEmployeeCodeAsync(employeeCode)
            ?? throw new InvalidOperationException($"Employee code '{employeeCode}' not found.");

        var records = await _attendanceRepository.GetByEmployeeAsync(employee.Id);
        return records.Select(r => ToDto(r, employee));
    }

    public async Task<IEnumerable<AttendanceRecordDto>> GetByDepartmentAsync(string departmentCode, DateOnly startDate, DateOnly endDate)
    {
        var department = await _departmentRepository.GetByCodeAsync(departmentCode)
            ?? throw new InvalidOperationException($"Department code '{departmentCode}' not found.");

        var records = await _attendanceRepository.GetByDepartmentAndPeriodAsync(department.Id, startDate, endDate);
        return records.Select(r => ToDto(r, r.Employee));
    }

    private static AttendanceRecordDto ToDto(AttendanceRecord record, Employee employee) => new()
    {
        EmployeeCode = employee.EmployeeCode,
        EmployeeName = $"{employee.FirstName} {employee.LastName}",
        AttendanceDate = record.AttendanceDate,
        CheckInTime = record.CheckInTime,
        CheckOutTime = record.CheckOutTime,
        WorkingHours = record.WorkingHours,
        Status = record.Status.ToString()
    };
}
