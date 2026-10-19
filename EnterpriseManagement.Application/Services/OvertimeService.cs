using EnterpriseManagement.Application.Common;
using EnterpriseManagement.Application.DTOs;
using EnterpriseManagement.Application.Interfaces;
using EnterpriseManagement.Domain.Entities.Attendance;
using EnterpriseManagement.Domain.Enums;

namespace EnterpriseManagement.Application.Services;

// Xin làm thêm giờ (OT). Luồng giống đơn nghỉ: nhân viên gửi, quản lý trực tiếp duyệt (hoặc cấp trên khi quản lý đang nghỉ).
// Đơn đã duyệt được cộng vào công của chính ngày đó (xem EffectiveDay.OvertimeMinutes).
// Giới hạn theo Bộ luật Lao động 2019: OT tối đa 40 giờ/tháng và 200 giờ/năm; tổng giờ làm một ngày không quá 12 giờ.
public class OvertimeService : IOvertimeService
{
    public const int MinMinutes = 30;
    public const int StepMinutes = 30;
    public const int WeekdayMaxMinutes = 4 * 60;
    public const int OffDayMaxMinutes = 12 * 60;
    public const int MonthLimitMinutes = 40 * 60;
    public const int YearLimitMinutes = 200 * 60;

    private static readonly TimeOnly WeekdayEarliest = new(17, 0);
    private static readonly TimeOnly OffDayEarliest = new(6, 0);
    private static readonly TimeOnly Latest = new(22, 0);
    private const int MaxDaysAhead = 30;

    private readonly IOvertimeRepository _repository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IApprovalDelegationResolver _approvalDelegationResolver;
    private readonly IEffectiveAttendanceService _effective;
    private readonly IPeriodLockService _periodLock;
    private readonly INotificationService _notificationService;
    private readonly IAuditLogService _auditLogService;
    private readonly ICurrentUserService _currentUserService;

    public OvertimeService(
        IOvertimeRepository repository,
        IEmployeeRepository employeeRepository,
        IApprovalDelegationResolver approvalDelegationResolver,
        IEffectiveAttendanceService effective,
        IPeriodLockService periodLock,
        INotificationService notificationService,
        IAuditLogService auditLogService,
        ICurrentUserService currentUserService)
    {
        _repository = repository;
        _employeeRepository = employeeRepository;
        _approvalDelegationResolver = approvalDelegationResolver;
        _effective = effective;
        _periodLock = periodLock;
        _notificationService = notificationService;
        _auditLogService = auditLogService;
        _currentUserService = currentUserService;
    }

    private Task LogAsync(string action, long entityId) =>
        _auditLogService.LogAsync(_currentUserService.UserId, action, "OvertimeRequest", entityId, _currentUserService.IpAddress);

    private async Task<Domain.Entities.HR.Employee> GetEmployeeAsync(string employeeCode) =>
        await _employeeRepository.GetByEmployeeCodeAsync(employeeCode)
            ?? throw new InvalidOperationException($"Không tìm thấy nhân viên có mã '{employeeCode}'.");

    // ---------------- Nhân viên ----------------

    public async Task<OvertimeRequestDto> SubmitAsync(SubmitOvertimeRequest request, string employeeCode)
    {
        var employee = await GetEmployeeAsync(employeeCode);
        if (!employee.TracksAttendance)
        {
            throw new InvalidOperationException(SystemAccount.NotTrackedMessage);
        }

        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new InvalidOperationException("Vui lòng nhập lý do làm thêm giờ.");
        }

        var (start, end) = ParseTimes(request);
        var minutes = (int)(end.ToTimeSpan() - start.ToTimeSpan()).TotalMinutes;
        var now = VietnamClock.Now;
        var today = DateOnly.FromDateTime(now);

        if (request.Date < today) throw new InvalidOperationException("Chỉ xin làm thêm giờ cho hôm nay hoặc các ngày sắp tới.");
        if (request.Date > today.AddDays(MaxDaysAhead)) throw new InvalidOperationException($"Chỉ xin trước tối đa {MaxDaysAhead} ngày.");
        if (request.Date == today && TimeOnly.FromDateTime(now) >= end) throw new InvalidOperationException("Khung giờ làm thêm này đã qua.");

        var nonWorking = EffectiveDayCalculator.IsNonWorkingDay(request.Date);
        var earliest = nonWorking ? OffDayEarliest : WeekdayEarliest;
        if (start < earliest || end > Latest)
        {
            throw new InvalidOperationException(nonWorking
                ? $"Làm thêm vào {WorkCalendar.NonWorkingReason(request.Date)} chỉ trong khoảng {OffDayEarliest:HH:mm} - {Latest:HH:mm}."
                : $"Ngày làm việc chỉ xin làm thêm từ {WeekdayEarliest:HH:mm} (sau giờ tan làm) đến {Latest:HH:mm}.");
        }

        var maxMinutes = nonWorking ? OffDayMaxMinutes : WeekdayMaxMinutes;
        if (minutes < MinMinutes || minutes > maxMinutes || minutes % StepMinutes != 0)
        {
            throw new InvalidOperationException(
                $"Thời lượng làm thêm phải từ {MinMinutes} phút đến {maxMinutes / 60} giờ, theo bước {StepMinutes} phút.");
        }

        await _periodLock.EnsureOpenAsync(request.Date);

        if (!nonWorking)
        {
            var day = await _effective.GetDayAsync(employee.Id, request.Date);
            if (day.Morning == SessionStatus.OnLeave && day.Afternoon == SessionStatus.OnLeave)
            {
                throw new InvalidOperationException("Bạn đang nghỉ phép cả ngày này nên không thể xin làm thêm giờ.");
            }
        }

        foreach (var other in await _repository.GetActiveByEmployeeAndDateAsync(employee.Id, request.Date))
        {
            if (start < other.EndTime && end > other.StartTime)
            {
                throw new InvalidOperationException("Khung giờ này trùng với một đơn làm thêm giờ khác của bạn (đang chờ duyệt hoặc đã duyệt).");
            }
        }

        await EnsureWithinQuotaAsync(employee.Id, request.Date, minutes, null);

        var overtime = new OvertimeRequest
        {
            EmployeeId = employee.Id,
            Date = request.Date,
            StartTime = start,
            EndTime = end,
            Minutes = minutes,
            Reason = request.Reason.Trim(),
            Status = OvertimeStatus.Pending,
            CreatedAt = now
        };
        await _repository.AddAsync(overtime);
        await _repository.SaveChangesAsync();
        await LogAsync("Submit", overtime.Id);

        await _notificationService.NotifyApproverAsync(
            employee, "OvertimeSubmitted", "Có đơn xin làm thêm giờ mới",
            $"{NotificationText.Name(employee)} xin làm thêm {FormatDuration(minutes)} ngày {request.Date:dd/MM/yyyy} ({start:HH:mm} - {end:HH:mm}).",
            "/manager/leave?tab=overtime", overtime.Id);

        return ToDto((await _repository.GetByIdAsync(overtime.Id))!);
    }

    // Hạn mức tính cả đơn đang chờ duyệt để nhân viên không "xin vượt" bằng cách gửi nhiều đơn cùng lúc.
    private async Task EnsureWithinQuotaAsync(long employeeId, DateOnly date, int addMinutes, long? excludeId)
    {
        var monthStart = new DateOnly(date.Year, date.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);
        var monthUsed = await _repository.SumActiveMinutesAsync(employeeId, monthStart, monthEnd, excludeId);
        if (monthUsed + addMinutes > MonthLimitMinutes)
        {
            throw new InvalidOperationException(
                $"Vượt giới hạn làm thêm {MonthLimitMinutes / 60} giờ/tháng theo luật (đã dùng hoặc đang chờ {FormatDuration(monthUsed)}, đơn này {FormatDuration(addMinutes)}).");
        }

        var yearUsed = await _repository.SumActiveMinutesAsync(employeeId, new DateOnly(date.Year, 1, 1), new DateOnly(date.Year, 12, 31), excludeId);
        if (yearUsed + addMinutes > YearLimitMinutes)
        {
            throw new InvalidOperationException(
                $"Vượt giới hạn làm thêm {YearLimitMinutes / 60} giờ/năm theo luật (đã dùng hoặc đang chờ {FormatDuration(yearUsed)}, đơn này {FormatDuration(addMinutes)}).");
        }
    }

    public async Task<IEnumerable<OvertimeRequestDto>> GetMineAsync(string employeeCode)
    {
        var employee = await GetEmployeeAsync(employeeCode);
        return (await _repository.GetByEmployeeAsync(employee.Id)).Select(ToDto);
    }

    public async Task<OvertimeQuotaDto> GetQuotaAsync(string employeeCode)
    {
        var employee = await GetEmployeeAsync(employeeCode);
        var today = VietnamClock.Today;
        var monthStart = new DateOnly(today.Year, today.Month, 1);
        return new OvertimeQuotaDto
        {
            Year = today.Year,
            Month = today.Month,
            MonthUsedMinutes = await _repository.SumActiveMinutesAsync(employee.Id, monthStart, monthStart.AddMonths(1).AddDays(-1)),
            MonthLimitMinutes = MonthLimitMinutes,
            YearUsedMinutes = await _repository.SumActiveMinutesAsync(employee.Id, new DateOnly(today.Year, 1, 1), new DateOnly(today.Year, 12, 31)),
            YearLimitMinutes = YearLimitMinutes
        };
    }

    public async Task<OvertimeRequestDto> CancelAsync(long id, string employeeCode)
    {
        var request = await _repository.GetByIdAsync(id) ?? throw new InvalidOperationException($"Không tìm thấy đơn làm thêm giờ {id}.");
        if (request.Employee.EmployeeCode != employeeCode)
        {
            throw new InvalidOperationException("Chỉ nhân viên đã gửi đơn này mới được huỷ.");
        }

        if (request.Status != OvertimeStatus.Pending)
        {
            throw new InvalidOperationException("Chỉ có thể huỷ đơn làm thêm giờ đang chờ duyệt.");
        }

        request.Status = OvertimeStatus.Cancelled;
        request.UpdatedAt = VietnamClock.Now;
        await _repository.SaveChangesAsync();
        await LogAsync("Cancel", request.Id);
        return ToDto(request);
    }

    // ---------------- Quản lý ----------------

    public async Task<IEnumerable<OvertimeRequestDto>> GetPendingAsync(string requesterEmployeeCode, bool isAdmin) =>
        await FilterByTeamAsync(await _repository.GetPendingAsync(), requesterEmployeeCode, isAdmin);

    public async Task<IEnumerable<OvertimeRequestDto>> GetHistoryAsync(string requesterEmployeeCode, bool isAdmin) =>
        await FilterByTeamAsync(await _repository.GetAllAsync(), requesterEmployeeCode, isAdmin);

    // Admin thấy tất cả; quản lý thấy đơn của người mình đang là người duyệt hiệu lực (kể cả khi quản lý trực tiếp của họ đang nghỉ).
    private async Task<IEnumerable<OvertimeRequestDto>> FilterByTeamAsync(
        IEnumerable<OvertimeRequest> requests, string requesterEmployeeCode, bool isAdmin)
    {
        if (isAdmin) return requests.Select(ToDto);

        var requester = await GetEmployeeAsync(requesterEmployeeCode);
        var result = new List<OvertimeRequestDto>();
        var approverByEmployee = new Dictionary<long, long?>();
        foreach (var r in requests)
        {
            if (!approverByEmployee.TryGetValue(r.EmployeeId, out var approverId))
            {
                approverId = (await _approvalDelegationResolver.ResolveApproverAsync(r.Employee))?.Id;
                approverByEmployee[r.EmployeeId] = approverId;
            }

            if (approverId == requester.Id) result.Add(ToDto(r));
        }

        return result;
    }

    public async Task<OvertimeRequestDto> ApproveAsync(long id, string approverEmployeeCode, bool isAdmin)
    {
        var (request, approver) = await GetForApprovalAsync(id, approverEmployeeCode, isAdmin);

        await _periodLock.EnsureOpenAsync(request.Date);
        await EnsureWithinQuotaAsync(request.EmployeeId, request.Date, request.Minutes, request.Id);

        request.Status = OvertimeStatus.Approved;
        request.ApprovedBy = approver.Id;
        request.Approver = approver;
        request.ApprovedAt = VietnamClock.Now;
        request.UpdatedAt = VietnamClock.Now;
        await _repository.SaveChangesAsync();
        await LogAsync("Approve", request.Id);

        await _notificationService.NotifyAsync(
            request.EmployeeId, "OvertimeApproved", "Đơn làm thêm giờ đã được duyệt",
            $"Đơn làm thêm giờ ngày {request.Date:dd/MM/yyyy} ({request.StartTime:HH:mm} - {request.EndTime:HH:mm}) đã được {NotificationText.Name(approver)} duyệt và sẽ được cộng vào công của ngày đó.",
            "/employee/overtime", request.Id);
        return ToDto(request);
    }

    public async Task<OvertimeRequestDto> RejectAsync(long id, string approverEmployeeCode, string? reason, bool isAdmin)
    {
        var (request, approver) = await GetForApprovalAsync(id, approverEmployeeCode, isAdmin);

        request.Status = OvertimeStatus.Rejected;
        request.ApprovedBy = approver.Id;
        request.Approver = approver;
        request.ApprovedAt = VietnamClock.Now;
        request.RejectionReason = reason;
        request.UpdatedAt = VietnamClock.Now;
        await _repository.SaveChangesAsync();
        await LogAsync("Reject", request.Id);

        await _notificationService.NotifyAsync(
            request.EmployeeId, "OvertimeRejected", "Đơn làm thêm giờ bị từ chối",
            $"Đơn làm thêm giờ ngày {request.Date:dd/MM/yyyy} của bạn đã bị {NotificationText.Name(approver)} từ chối."
            + (string.IsNullOrWhiteSpace(reason) ? string.Empty : $" Lý do: {reason}"),
            "/employee/overtime", request.Id);
        return ToDto(request);
    }

    private async Task<(OvertimeRequest Request, Domain.Entities.HR.Employee Approver)> GetForApprovalAsync(
        long id, string approverEmployeeCode, bool isAdmin)
    {
        var approver = await GetEmployeeAsync(approverEmployeeCode);
        var request = await _repository.GetByIdAsync(id) ?? throw new InvalidOperationException($"Không tìm thấy đơn làm thêm giờ {id}.");

        if (!isAdmin)
        {
            var effectiveApprover = await _approvalDelegationResolver.ResolveApproverAsync(request.Employee);
            if (effectiveApprover?.Id != approver.Id)
            {
                throw new InvalidOperationException("Bạn không phải quản lý trực tiếp của nhân viên này.");
            }
        }

        if (request.Status != OvertimeStatus.Pending)
        {
            throw new InvalidOperationException("Chỉ có thể xử lý đơn làm thêm giờ đang chờ duyệt.");
        }

        return (request, approver);
    }

    // ---------------- Hỗ trợ ----------------

    private static (TimeOnly Start, TimeOnly End) ParseTimes(SubmitOvertimeRequest request)
    {
        if (!TimeOnly.TryParseExact(request.StartTime, "HH:mm", out var start)
            || !TimeOnly.TryParseExact(request.EndTime, "HH:mm", out var end))
        {
            throw new InvalidOperationException("Giờ bắt đầu và giờ kết thúc phải có dạng HH:mm.");
        }

        if (end <= start) throw new InvalidOperationException("Giờ kết thúc phải sau giờ bắt đầu (làm thêm trong cùng một ngày).");
        return (start, end);
    }

    public static string FormatDuration(int minutes) =>
        minutes % 60 == 0 ? $"{minutes / 60} giờ" : $"{minutes / 60} giờ {minutes % 60} phút";

    private static OvertimeRequestDto ToDto(OvertimeRequest r) => new()
    {
        Id = r.Id,
        EmployeeCode = r.Employee.EmployeeCode,
        EmployeeName = NotificationText.Name(r.Employee),
        Date = r.Date,
        StartTime = r.StartTime.ToString("HH:mm"),
        EndTime = r.EndTime.ToString("HH:mm"),
        Minutes = r.Minutes,
        Reason = r.Reason,
        Status = r.Status.ToString(),
        ApproverName = r.Approver is null ? null : NotificationText.Name(r.Approver),
        ApprovedAt = r.ApprovedAt,
        RejectionReason = r.RejectionReason,
        CreatedAt = r.CreatedAt
    };
}
