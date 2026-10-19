using EnterpriseManagement.Domain.Entities.Attendance;

namespace EnterpriseManagement.Application.Interfaces;

public interface IAttendancePeriodRepository
{
    Task<AttendancePeriod?> GetAsync(int year, int month);
    Task<List<AttendancePeriod>> GetByYearAsync(int year);
    Task AddAsync(AttendancePeriod period);
    void Remove(AttendancePeriod period);
    Task<int> SaveChangesAsync();
}
