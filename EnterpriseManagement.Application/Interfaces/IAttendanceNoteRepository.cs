using EnterpriseManagement.Domain.Entities.Attendance;
using EnterpriseManagement.Domain.Enums;

namespace EnterpriseManagement.Application.Interfaces;

public interface IAttendanceNoteRepository
{
    Task<AttendanceSessionNote?> GetByIdAsync(long id);
    Task<AttendanceSessionNote?> GetActiveAsync(long employeeId, DateOnly date, DaySession session);
    Task<IEnumerable<AttendanceSessionNote>> GetActiveByRangeAsync(DateOnly from, DateOnly to, long? employeeId);
    Task AddAsync(AttendanceSessionNote note);
    Task<int> SaveChangesAsync();
}
