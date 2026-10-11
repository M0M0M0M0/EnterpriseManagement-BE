using EnterpriseManagement.Application.Interfaces;
using EnterpriseManagement.Domain.Entities.Attendance;
using EnterpriseManagement.Domain.Enums;
using EnterpriseManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseManagement.Infrastructure.Repositories;

public class AttendanceNoteRepository : IAttendanceNoteRepository
{
    private readonly ApplicationDbContext _context;

    public AttendanceNoteRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AttendanceSessionNote?> GetByIdAsync(long id) =>
        await _context.AttendanceSessionNotes
            .Include(n => n.Employee).Include(n => n.Creator).Include(n => n.Revoker)
            .FirstOrDefaultAsync(n => n.Id == id);

    public async Task<AttendanceSessionNote?> GetActiveAsync(long employeeId, DateOnly date, DaySession session) =>
        await _context.AttendanceSessionNotes
            .FirstOrDefaultAsync(n => n.EmployeeId == employeeId && n.Date == date && n.Session == session && n.RevokedAt == null);

    public async Task<IEnumerable<AttendanceSessionNote>> GetActiveByRangeAsync(DateOnly from, DateOnly to, long? employeeId) =>
        await _context.AttendanceSessionNotes
            .Include(n => n.Employee).Include(n => n.Creator)
            .Where(n => n.RevokedAt == null && n.Date >= from && n.Date <= to && (employeeId == null || n.EmployeeId == employeeId))
            .OrderByDescending(n => n.Date).ThenBy(n => n.EmployeeId).ThenBy(n => n.Session)
            .ToListAsync();

    public async Task AddAsync(AttendanceSessionNote note) =>
        await _context.AttendanceSessionNotes.AddAsync(note);

    public Task<int> SaveChangesAsync() => _context.SaveChangesAsync();
}
