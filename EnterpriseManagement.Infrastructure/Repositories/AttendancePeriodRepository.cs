using EnterpriseManagement.Application.Interfaces;
using EnterpriseManagement.Domain.Entities.Attendance;
using EnterpriseManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseManagement.Infrastructure.Repositories;

public class AttendancePeriodRepository : IAttendancePeriodRepository
{
    private readonly ApplicationDbContext _context;

    public AttendancePeriodRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<AttendancePeriod?> GetAsync(int year, int month) =>
        _context.AttendancePeriods.Include(p => p.Closer).FirstOrDefaultAsync(p => p.Year == year && p.Month == month);

    public Task<List<AttendancePeriod>> GetByYearAsync(int year) =>
        _context.AttendancePeriods.Include(p => p.Closer).Where(p => p.Year == year).ToListAsync();

    public async Task AddAsync(AttendancePeriod period) => await _context.AttendancePeriods.AddAsync(period);

    public void Remove(AttendancePeriod period) => _context.AttendancePeriods.Remove(period);

    public Task<int> SaveChangesAsync() => _context.SaveChangesAsync();
}
