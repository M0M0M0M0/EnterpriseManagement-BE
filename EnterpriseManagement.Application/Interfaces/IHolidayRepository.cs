using EnterpriseManagement.Domain.Entities.Attendance;

namespace EnterpriseManagement.Application.Interfaces;

public interface IHolidayRepository
{
    Task<List<Holiday>> GetAllAsync();
    Task<List<Holiday>> GetByYearAsync(int year);
    Task<Holiday?> GetByIdAsync(long id);
    Task<bool> ExistsOnDateAsync(DateOnly date);
    Task AddAsync(Holiday holiday);
    void Remove(Holiday holiday);
    Task<int> SaveChangesAsync();
}
