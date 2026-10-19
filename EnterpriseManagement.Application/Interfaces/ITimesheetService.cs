using EnterpriseManagement.Application.DTOs;

namespace EnterpriseManagement.Application.Interfaces;

public interface ITimesheetService
{
    Task<TimesheetDto> GetAsync(int year, int month, string? departmentCode);
    Task<TimesheetDto> GetTeamAsync(int year, int month, string managerEmployeeCode);
    Task<byte[]> ExportCsvAsync(int year, int month, string? departmentCode);
    Task<IEnumerable<AttendancePeriodDto>> GetClosedPeriodsAsync(int year);
    Task<AttendancePeriodDto> CloseAsync(int year, int month, string closerEmployeeCode);
    Task ReopenAsync(int year, int month);
}
