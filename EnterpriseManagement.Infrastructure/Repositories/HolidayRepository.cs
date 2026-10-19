using EnterpriseManagement.Application.Interfaces;
using EnterpriseManagement.Domain.Entities.Attendance;
using EnterpriseManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseManagement.Infrastructure.Repositories;

public class HolidayRepository : IHolidayRepository
{
    private readonly ApplicationDbContext _context;

    public HolidayRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<List<Holiday>> GetAllAsync() => _context.Holidays.AsNoTracking().OrderBy(h => h.Date).ToListAsync();

    public Task<List<Holiday>> GetByYearAsync(int year) =>
        _context.Holidays.AsNoTracking().Where(h => h.Date.Year == year).OrderBy(h => h.Date).ToListAsync();

    public Task<Holiday?> GetByIdAsync(long id) => _context.Holidays.FirstOrDefaultAsync(h => h.Id == id);

    public Task<bool> ExistsOnDateAsync(DateOnly date) => _context.Holidays.AnyAsync(h => h.Date == date);

    public async Task AddAsync(Holiday holiday) => await _context.Holidays.AddAsync(holiday);

    public void Remove(Holiday holiday) => _context.Holidays.Remove(holiday);

    public Task<int> SaveChangesAsync() => _context.SaveChangesAsync();
}
