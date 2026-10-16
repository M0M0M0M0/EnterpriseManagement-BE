using EnterpriseManagement.Application.Interfaces;
using EnterpriseManagement.Domain.Entities.Attendance;
using EnterpriseManagement.Domain.Enums;
using EnterpriseManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseManagement.Infrastructure.Repositories;

public class AttendanceAdjustmentRepository : IAttendanceAdjustmentRepository
{
    private readonly ApplicationDbContext _context;

    public AttendanceAdjustmentRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AttendanceAdjustment?> GetByIdAsync(long id) =>
        await _context.AttendanceAdjustments
            .Include(a => a.Attendance)
            .Include(a => a.Requester)
            .Include(a => a.Approver)
            .Include(a => a.Processor)
            .FirstOrDefaultAsync(a => a.Id == id);

    public async Task<IEnumerable<AttendanceAdjustment>> GetPendingHrAsync() =>
        await _context.AttendanceAdjustments
            .Include(a => a.Attendance)
            .Include(a => a.Requester)
            .Include(a => a.Approver)
            .Where(a => a.Status == ApprovalStatus.PendingHr)
            // Yêu cầu gửi mới nhất lên đầu (theo thời điểm nhân viên gửi, không theo ngày cần điều chỉnh).
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

    public async Task<bool> HasOpenAsync(long employeeId, DateOnly date) =>
        await _context.AttendanceAdjustments
            .AnyAsync(a => a.RequestedBy == employeeId
                && a.Attendance.AttendanceDate == date
                && (a.Status == ApprovalStatus.Pending || a.Status == ApprovalStatus.PendingHr));

    public async Task<IEnumerable<AttendanceAdjustment>> GetPendingAsync() =>
        await _context.AttendanceAdjustments
            .Include(a => a.Attendance)
            .Include(a => a.Requester)
            .Where(a => a.Status == ApprovalStatus.Pending)
            // Yêu cầu gửi mới nhất lên đầu (theo thời điểm nhân viên gửi, không theo ngày cần điều chỉnh).
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

    public async Task<IEnumerable<AttendanceAdjustment>> GetByEmployeeIdAsync(long employeeId) =>
        await _context.AttendanceAdjustments
            .Include(a => a.Attendance)
            .Include(a => a.Requester)
            .Include(a => a.Approver)
            .Include(a => a.Processor)
            .Where(a => a.RequestedBy == employeeId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

    public async Task AddAsync(AttendanceAdjustment adjustment) =>
        await _context.AttendanceAdjustments.AddAsync(adjustment);

    public Task<int> SaveChangesAsync() =>
        _context.SaveChangesAsync();
}
