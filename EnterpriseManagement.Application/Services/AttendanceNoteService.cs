using EnterpriseManagement.Application.Common;
using EnterpriseManagement.Application.DTOs;
using EnterpriseManagement.Application.Interfaces;
using EnterpriseManagement.Domain.Entities.Attendance;
using EnterpriseManagement.Domain.Enums;

namespace EnterpriseManagement.Application.Services;

// Ghi chú ngoại lệ chấm công theo từng buổi (quên chấm công, vào chiều muộn, vắng...). Không sửa dữ liệu chấm công gốc.
// Không áp dụng cho buổi đang nghỉ phép: muốn đi làm trong ngày nghỉ phải qua yêu cầu đi làm lại (LeaveReturnService).
public class AttendanceNoteService : IAttendanceNoteService
{
    private const int MaxRangeDays = 31;

    private readonly IAttendanceNoteRepository _repository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IEffectiveAttendanceService _effective;
    private readonly IAuditLogService _auditLogService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPeriodLockService _periodLock;
    private readonly INotificationService _notificationService;

    public AttendanceNoteService(
        IAttendanceNoteRepository repository,
        IEmployeeRepository employeeRepository,
        IEffectiveAttendanceService effective,
        IAuditLogService auditLogService,
        ICurrentUserService currentUserService,
        IPeriodLockService periodLock,
        INotificationService notificationService)
    {
        _repository = repository;
        _employeeRepository = employeeRepository;
        _effective = effective;
        _auditLogService = auditLogService;
        _currentUserService = currentUserService;
        _periodLock = periodLock;
        _notificationService = notificationService;
    }

    private Task LogAsync(string action, long entityId) =>
        _auditLogService.LogAsync(_currentUserService.UserId, action, "AttendanceSessionNote", entityId, _currentUserService.IpAddress);

    public async Task<IEnumerable<AttendanceNoteDto>> CreateAsync(string creatorEmployeeCode, CreateAttendanceNoteRequest request)
    {
        var creator = await _employeeRepository.GetByEmployeeCodeAsync(creatorEmployeeCode)
            ?? throw new InvalidOperationException($"Không tìm thấy nhân viên có mã '{creatorEmployeeCode}'.");
        var employee = await _employeeRepository.GetByEmployeeCodeAsync(request.EmployeeCode)
            ?? throw new InvalidOperationException($"Không tìm thấy nhân viên có mã '{request.EmployeeCode}'.");

        // Không tự ghi chú cho chính mình, để người xác nhận luôn là người khác.
        if (creator.Id == employee.Id)
        {
            throw new InvalidOperationException("Bạn không thể tự ghi chú chấm công cho chính mình.");
        }

        if (string.IsNullOrWhiteSpace(request.Note))
        {
            throw new InvalidOperationException("Vui lòng nhập nội dung ghi chú.");
        }

        if (!Enum.TryParse<SessionStatus>(request.Status, true, out var status)
            || status is not (SessionStatus.OnTime or SessionStatus.Late or SessionStatus.Absent))
        {
            throw new InvalidOperationException("Trạng thái chỉ có thể là OnTime (đúng giờ), Late (đi muộn) hoặc Absent (vắng).");
        }

        if (!Enum.TryParse<LeaveSession>(request.Session, true, out var leaveSession))
        {
            throw new InvalidOperationException("Vui lòng chọn buổi (Morning, Afternoon hoặc FullDay).");
        }

        if (request.EndDate < request.StartDate)
        {
            throw new InvalidOperationException("Ngày kết thúc phải sau hoặc bằng ngày bắt đầu.");
        }

        if (request.EndDate.DayNumber - request.StartDate.DayNumber >= MaxRangeDays)
        {
            throw new InvalidOperationException($"Mỗi lần ghi chú tối đa {MaxRangeDays} ngày.");
        }

        if (request.StartDate < employee.HireDate)
        {
            throw new InvalidOperationException("Không ghi chú cho ngày trước ngày vào làm của nhân viên.");
        }

        // Danh sách (ngày, buổi) cần ghi chú: chỉ ngày làm việc, và chỉ buổi đã bắt đầu (không xác nhận cho tương lai).
        var now = VietnamClock.Now;
        var targets = new List<(DateOnly Date, DaySession Session)>();
        for (var date = request.StartDate; date <= request.EndDate; date = date.AddDays(1))
        {
            if (EffectiveDayCalculator.IsNonWorkingDay(date)) continue;

            if (leaveSession is LeaveSession.FullDay or LeaveSession.Morning) targets.Add((date, DaySession.Morning));
            if (leaveSession is LeaveSession.FullDay or LeaveSession.Afternoon) targets.Add((date, DaySession.Afternoon));
        }

        if (targets.Count == 0)
        {
            throw new InvalidOperationException("Khoảng đã chọn không có ngày làm việc nào.");
        }

        await _periodLock.EnsureOpenAsync(request.StartDate, request.EndDate);

        foreach (var (date, session) in targets)
        {
            var start = date.ToDateTime(TimeOnly.MinValue) + (session == DaySession.Morning ? new TimeSpan(8, 0, 0) : new TimeSpan(13, 0, 0));
            if (start > now)
            {
                throw new InvalidOperationException($"Buổi {Label(session)} {date:dd/MM/yyyy} chưa bắt đầu nên chưa thể ghi chú.");
            }
        }

        // Buổi đang nghỉ phép hiệu lực thì không ghi chú được (kể cả "đi làm"): phải qua yêu cầu đi làm lại.
        var days = (await _effective.GetRangeAsync(employee.Id, request.StartDate, request.EndDate)).ToDictionary(d => d.Date);
        foreach (var (date, session) in targets)
        {
            var current = session == DaySession.Morning ? days[date].Morning : days[date].Afternoon;
            if (current == SessionStatus.OnLeave)
            {
                throw new InvalidOperationException(
                    $"Buổi {Label(session)} {date:dd/MM/yyyy} đang nghỉ phép. Nhân viên cần gửi yêu cầu rút ngắn nghỉ phép, không ghi chú trực tiếp được.");
            }
        }

        // Ghi chú cũ còn hiệu lực của cùng buổi bị thu hồi trước (lưu riêng) rồi mới thêm ghi chú mới, để không vi phạm
        // ràng buộc "mỗi buổi chỉ một ghi chú hiệu lực".
        var revokedAny = false;
        foreach (var (date, session) in targets)
        {
            var existing = await _repository.GetActiveAsync(employee.Id, date, session);
            if (existing is null) continue;

            existing.RevokedBy = creator.Id;
            existing.RevokedAt = now;
            revokedAny = true;
        }

        if (revokedAny) await _repository.SaveChangesAsync();

        var created = new List<AttendanceSessionNote>();
        foreach (var (date, session) in targets)
        {
            var note = new AttendanceSessionNote
            {
                EmployeeId = employee.Id,
                Date = date,
                Session = session,
                Status = status,
                Note = request.Note.Trim(),
                CreatedBy = creator.Id,
                CreatedAt = now
            };
            await _repository.AddAsync(note);
            created.Add(note);
        }

        await _repository.SaveChangesAsync();
        foreach (var note in created) await LogAsync("Create", note.Id);

        await _notificationService.NotifyAsync(
            employee.Id, "AttendanceNoteCreated", "Nhân sự đã ghi nhận chấm công cho bạn",
            $"{NotificationText.Range(request.StartDate, request.EndDate)} ({NotificationText.Session(leaveSession)}): {NotificationText.Status(status)}. Ghi chú: {request.Note.Trim()}",
            "/employee/attendance", created.First().Id);

        return created.Select(n => ToDto(n, employee, creator));
    }

    public async Task RevokeAsync(long noteId, string revokerEmployeeCode)
    {
        var revoker = await _employeeRepository.GetByEmployeeCodeAsync(revokerEmployeeCode)
            ?? throw new InvalidOperationException($"Không tìm thấy nhân viên có mã '{revokerEmployeeCode}'.");

        var note = await _repository.GetByIdAsync(noteId)
            ?? throw new InvalidOperationException($"Ghi chú {noteId} không tồn tại.");

        if (note.RevokedAt is not null)
        {
            throw new InvalidOperationException("Ghi chú này đã được thu hồi trước đó.");
        }

        if (note.EmployeeId == revoker.Id)
        {
            throw new InvalidOperationException("Bạn không thể thu hồi ghi chú về chính mình.");
        }

        await _periodLock.EnsureOpenAsync(note.Date);

        note.RevokedBy = revoker.Id;
        note.RevokedAt = VietnamClock.Now;

        await _repository.SaveChangesAsync();
        await LogAsync("Revoke", note.Id);
    }

    public async Task<IEnumerable<AttendanceNoteDto>> GetActiveAsync(DateOnly from, DateOnly to, string? employeeCode)
    {
        long? employeeId = null;
        if (!string.IsNullOrWhiteSpace(employeeCode))
        {
            var employee = await _employeeRepository.GetByEmployeeCodeAsync(employeeCode)
                ?? throw new InvalidOperationException($"Không tìm thấy nhân viên có mã '{employeeCode}'.");
            employeeId = employee.Id;
        }

        var notes = await _repository.GetActiveByRangeAsync(from, to, employeeId);
        return notes.Select(n => ToDto(n, n.Employee, n.Creator));
    }

    private static string Label(DaySession session) => session == DaySession.Morning ? "sáng" : "chiều";

    private static AttendanceNoteDto ToDto(AttendanceSessionNote n, Domain.Entities.HR.Employee employee, Domain.Entities.HR.Employee creator) => new()
    {
        Id = n.Id,
        EmployeeCode = employee.EmployeeCode,
        EmployeeName = $"{employee.FirstName} {employee.LastName}",
        Date = n.Date,
        Session = n.Session.ToString(),
        Status = n.Status.ToString(),
        Note = n.Note,
        CreatedByName = $"{creator.FirstName} {creator.LastName}",
        CreatedAt = n.CreatedAt
    };
}
