using EnterpriseManagement.Application.Common;
using EnterpriseManagement.Application.Interfaces;
using EnterpriseManagement.Domain.Enums;
using EnterpriseManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseManagement.Infrastructure.Repositories;

public class EffectiveAttendanceRepository : IEffectiveAttendanceRepository
{
    private readonly ApplicationDbContext _context;

    public EffectiveAttendanceRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<EffectiveInputs> LoadAsync(IReadOnlyCollection<long> employeeIds, DateOnly from, DateOnly to)
    {
        var ids = employeeIds.ToList();
        var fromDt = from.ToDateTime(TimeOnly.MinValue);
        var toDt = to.AddDays(1).ToDateTime(TimeOnly.MinValue);

        // Đơn nghỉ đã duyệt tính theo buổi (nghỉ theo giờ không có Session nên không nằm trong mô hình buổi).
        var leaveRows = await _context.LeaveRequests
            .AsNoTracking()
            .Include(l => l.LeaveType)
            .Where(l => ids.Contains(l.EmployeeId)
                && l.Status == LeaveRequestStatus.Approved
                && l.Session != null
                && l.EndDate >= fromDt
                && l.StartDate < toDt)
            .ToListAsync();

        var leaves = leaveRows
            .Select(l => new LeaveCoverage(
                l.Id, l.EmployeeId, DateOnly.FromDateTime(l.StartDate), DateOnly.FromDateTime(l.EndDate),
                l.Session!.Value, l.LeaveType.LeaveTypeName))
            .ToList();

        var leaveIds = leaveRows.Select(l => l.Id).ToList();
        var employeeByLeave = leaveRows.ToDictionary(l => l.Id, l => l.EmployeeId);

        var returnRows = leaveIds.Count == 0
            ? new List<Domain.Entities.Leave.LeaveReturnSession>()
            : await _context.LeaveReturnSessions
                .AsNoTracking()
                .Where(s => leaveIds.Contains(s.LeaveRequestId) && s.State == ReturnSessionState.Active)
                .ToListAsync();

        var returns = returnRows
            .Select(s => new ActiveReturn(s.LeaveRequestId, employeeByLeave[s.LeaveRequestId], s.Date, s.Session, s.WorkedStatus))
            .ToList();

        var notes = await _context.AttendanceSessionNotes
            .AsNoTracking()
            .Where(n => ids.Contains(n.EmployeeId) && n.RevokedAt == null && n.Date >= from && n.Date <= to)
            .Select(n => new SessionNote(n.EmployeeId, n.Date, n.Session, n.Status, n.Note))
            .ToListAsync();

        // Chỉ lấy bản ghi đã có giờ vào thật. Bản ghi giữ chỗ (tạo khi duyệt nghỉ phép cũ hoặc khi gửi điều chỉnh công)
        // không có giờ vào nên bị bỏ qua, tránh trở thành nguồn sự thật thứ hai.
        var punches = await _context.AttendanceRecords
            .AsNoTracking()
            .Where(a => ids.Contains(a.EmployeeId) && a.CheckInTime != null && a.AttendanceDate >= from && a.AttendanceDate <= to)
            .Select(a => new Punch(a.EmployeeId, a.AttendanceDate, a.CheckInTime!.Value, a.CheckOutTime, a.WorkingHours))
            .ToListAsync();

        // OT đã được duyệt: cộng vào công của chính ngày đó.
        var overtimes = (await _context.OvertimeRequests
                .AsNoTracking()
                .Where(o => ids.Contains(o.EmployeeId) && o.Status == OvertimeStatus.Approved && o.Date >= from && o.Date <= to)
                .Select(o => new { o.EmployeeId, o.Date, o.Minutes })
                .ToListAsync())
            .GroupBy(o => (o.EmployeeId, o.Date))
            .Select(g => new OvertimeEntry(g.Key.EmployeeId, g.Key.Date, g.Sum(x => x.Minutes)))
            .ToList();

        var employeeRows = await _context.Employees
            .AsNoTracking()
            .Where(e => ids.Contains(e.Id))
            .Select(e => new { e.Id, e.HireDate, e.TracksAttendance })
            .ToListAsync();
        var hireDates = employeeRows.ToDictionary(e => e.Id, e => (DateOnly?)e.HireDate);
        var nonTracking = employeeRows.Where(e => !e.TracksAttendance).Select(e => e.Id).ToHashSet();

        return new EffectiveInputs
        {
            Leaves = leaves,
            Returns = returns,
            Notes = notes,
            Punches = punches,
            Overtimes = overtimes,
            HireDates = hireDates,
            NonTrackingEmployeeIds = nonTracking
        };
    }
}
