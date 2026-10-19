using EnterpriseManagement.Application.Common;
using EnterpriseManagement.Application.DTOs;
using EnterpriseManagement.Application.Interfaces;
using EnterpriseManagement.Domain.Entities.HR;
using EnterpriseManagement.Domain.Entities.Leave;
using EnterpriseManagement.Domain.Enums;

namespace EnterpriseManagement.Application.Services;

// Luồng "đi làm lại" khi đang có nghỉ phép đã duyệt. Đơn nghỉ gốc KHÔNG bị sửa: yêu cầu này tạo ra các buổi bị gỡ khỏi đúng
// đơn đó (LeaveReturnSession), hàm tính hiệu lực và số dư phép đọc chúng lúc cần.
//   - Nhân viên chọn buổi bắt đầu đi làm lại (buổi chưa bắt đầu), hệ thống tự gắn vào đơn đang phủ buổi đó.
//   - Manager duyệt: buổi chưa bắt đầu tại thời điểm duyệt có hiệu lực ngay; buổi đã bắt đầu chờ người xử lý (HR) xác nhận.
public class LeaveReturnService : ILeaveReturnService
{
    private static readonly TimeSpan MorningStart = new(8, 0, 0);
    private static readonly TimeSpan AfternoonStart = new(13, 0, 0);

    private readonly ILeaveReturnRepository _repository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IEffectiveAttendanceRepository _effectiveRepository;
    private readonly IApprovalDelegationResolver _approvalDelegationResolver;
    private readonly IAuditLogService _auditLogService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPeriodLockService _periodLock;
    private readonly INotificationService _notificationService;

    public LeaveReturnService(
        ILeaveReturnRepository repository,
        IEmployeeRepository employeeRepository,
        IEffectiveAttendanceRepository effectiveRepository,
        IApprovalDelegationResolver approvalDelegationResolver,
        IAuditLogService auditLogService,
        ICurrentUserService currentUserService,
        IPeriodLockService periodLock,
        INotificationService notificationService)
    {
        _repository = repository;
        _employeeRepository = employeeRepository;
        _effectiveRepository = effectiveRepository;
        _approvalDelegationResolver = approvalDelegationResolver;
        _auditLogService = auditLogService;
        _currentUserService = currentUserService;
        _periodLock = periodLock;
        _notificationService = notificationService;
    }

    private Task LogAsync(string action, long entityId) =>
        _auditLogService.LogAsync(_currentUserService.UserId, action, "LeaveReturnRequest", entityId, _currentUserService.IpAddress);

    // ---------------- Nhân viên ----------------

    public async Task<LeaveReturnPreviewDto> PreviewAsync(string employeeCode, DateOnly fromDate, string fromSession)
    {
        var employee = await GetEmployeeAsync(employeeCode);
        var session = ParseSession(fromSession);
        ValidateNotStarted(fromDate, session, VietnamClock.Now);

        var tail = await ResolveTailAsync(employee.Id, fromDate, session);
        return new LeaveReturnPreviewDto
        {
            Sessions = tail.Select(t => new LeaveReturnSessionDto
            {
                Date = t.Date, Session = t.Session.ToString(), LeaveRequestId = t.LeaveRequestId,
                LeaveTypeName = t.LeaveTypeName, State = ReturnSessionState.Pending.ToString()
            }).ToList(),
            SessionCount = tail.Count,
            RefundDays = tail.Count * 0.5m,
            LeaveRequestIds = tail.Select(t => t.LeaveRequestId).Distinct().ToList()
        };
    }

    public async Task<LeaveReturnRequestDto> SubmitAsync(string employeeCode, SubmitLeaveReturnRequest request)
    {
        var employee = await GetEmployeeAsync(employeeCode);

        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new InvalidOperationException("Vui lòng nhập lý do xin rút ngắn nghỉ phép.");
        }

        var session = ParseSession(request.FromSession);
        ValidateNotStarted(request.FromDate, session, VietnamClock.Now);

        if (await _repository.HasOpenRequestAsync(employee.Id))
        {
            throw new InvalidOperationException("Bạn đang có một yêu cầu rút ngắn nghỉ phép chưa xử lý xong. Vui lòng chờ kết quả trước khi gửi yêu cầu khác.");
        }

        var tail = await ResolveTailAsync(employee.Id, request.FromDate, session);
        await _periodLock.EnsureOpenAsync(request.FromDate, tail.Max(t => t.Date));

        var entity = new LeaveReturnRequest
        {
            EmployeeId = employee.Id,
            FromDate = request.FromDate,
            FromSession = session,
            Reason = request.Reason.Trim(),
            Status = ReturnRequestStatus.PendingManager,
            CreatedAt = VietnamClock.Now,
            Sessions = tail.Select(t => new LeaveReturnSession
            {
                LeaveRequestId = t.LeaveRequestId, Date = t.Date, Session = t.Session, State = ReturnSessionState.Pending
            }).ToList()
        };

        await _repository.AddAsync(entity);
        await _repository.SaveChangesAsync();
        await LogAsync("Submit", entity.Id);

        await _notificationService.NotifyApproverAsync(
            employee, "ReturnSubmitted", "Có yêu cầu rút ngắn nghỉ phép mới",
            $"{NotificationText.Name(employee)} xin rút ngắn nghỉ phép, đi làm lại từ {NotificationText.Session(session)} {request.FromDate:dd/MM/yyyy}.",
            "/manager/leave?tab=returns", entity.Id);

        return ToDto((await _repository.GetByIdAsync(entity.Id))!);
    }

    public async Task<IEnumerable<LeaveReturnRequestDto>> GetMineAsync(string employeeCode)
    {
        var employee = await GetEmployeeAsync(employeeCode);
        return (await _repository.GetByEmployeeAsync(employee.Id)).Select(ToDto);
    }

    // ---------------- Manager ----------------

    public async Task<IEnumerable<LeaveReturnRequestDto>> GetPendingForManagerAsync(string managerEmployeeCode, bool isAdmin)
    {
        var manager = await GetEmployeeAsync(managerEmployeeCode);
        var pending = await _repository.GetByStatusAsync(ReturnRequestStatus.PendingManager);
        if (isAdmin) return pending.Select(ToDto);

        var result = new List<LeaveReturnRequestDto>();
        foreach (var request in pending)
        {
            var approver = await _approvalDelegationResolver.ResolveApproverAsync(request.Employee);
            if (approver?.Id == manager.Id) result.Add(ToDto(request));
        }

        return result;
    }

    public async Task<LeaveReturnRequestDto> ApproveAsync(long id, string managerEmployeeCode, bool isAdmin)
    {
        var manager = await GetEmployeeAsync(managerEmployeeCode);
        var request = await GetForManagerAsync(id, manager, isAdmin);

        var now = VietnamClock.Now;
        foreach (var session in request.Sessions.Where(s => s.State == ReturnSessionState.Pending))
        {
            // Buổi chưa bắt đầu lúc duyệt thì có hiệu lực ngay; buổi đã bắt đầu hoặc đã qua cần người xử lý xác nhận
            // là nhân viên thật sự đã đi làm.
            session.State = SessionStart(session.Date, session.Session) > now
                ? ReturnSessionState.Active
                : ReturnSessionState.AwaitingHr;
        }

        request.ManagerId = manager.Id;
        request.ManagerApprovedAt = now;
        request.Status = request.Sessions.Any(s => s.State == ReturnSessionState.AwaitingHr)
            ? ReturnRequestStatus.PendingHr
            : ReturnRequestStatus.Completed;
        request.UpdatedAt = now;

        await _repository.SaveChangesAsync();
        await LogAsync("Approve", request.Id);

        var awaitingHr = request.Status == ReturnRequestStatus.PendingHr;
        await _notificationService.NotifyAsync(
            request.EmployeeId, "ReturnApproved", "Yêu cầu rút ngắn nghỉ phép đã được duyệt",
            awaitingHr
                ? "Quản lý đã duyệt. Các buổi đã bắt đầu đang chờ nhân sự xác nhận bạn đã đi làm."
                : "Quản lý đã duyệt. Các buổi đi làm lại đã được gỡ khỏi nghỉ phép và phép được hoàn lại.",
            "/employee/leave", request.Id);

        if (awaitingHr)
        {
            await _notificationService.NotifyPermissionHoldersAsync(
                "attendance.note.manage", request.EmployeeId, "ReturnAwaitingProcessing", "Có yêu cầu rút ngắn nghỉ phép chờ xác nhận",
                $"{NotificationText.Name(request.Employee)} đã được quản lý duyệt đi làm lại, cần xác nhận các buổi đã bắt đầu.",
                "/hr/leave-returns", request.Id);
        }
        return ToDto(request);
    }

    public async Task<LeaveReturnRequestDto> RejectAsync(long id, string managerEmployeeCode, bool isAdmin, string? reason)
    {
        var manager = await GetEmployeeAsync(managerEmployeeCode);
        var request = await GetForManagerAsync(id, manager, isAdmin);

        foreach (var session in request.Sessions) session.State = ReturnSessionState.Rejected;

        request.ManagerId = manager.Id;
        request.ManagerApprovedAt = VietnamClock.Now;
        request.Status = ReturnRequestStatus.Rejected;
        request.RejectionReason = reason;
        request.UpdatedAt = VietnamClock.Now;

        await _repository.SaveChangesAsync();
        await LogAsync("Reject", request.Id);

        await _notificationService.NotifyAsync(
            request.EmployeeId, "ReturnRejected", "Yêu cầu rút ngắn nghỉ phép bị từ chối",
            "Quản lý đã từ chối yêu cầu rút ngắn nghỉ phép của bạn." + (string.IsNullOrWhiteSpace(reason) ? string.Empty : $" Lý do: {reason}"),
            "/employee/leave", request.Id);
        return ToDto(request);
    }

    // ---------------- Người xử lý (HR) ----------------

    public async Task<IEnumerable<LeaveReturnRequestDto>> GetAwaitingProcessingAsync() =>
        (await _repository.GetByStatusAsync(ReturnRequestStatus.PendingHr)).Select(ToDto);

    public async Task<LeaveReturnRequestDto> ProcessAsync(long id, string processorEmployeeCode, ProcessLeaveReturnRequest request)
    {
        var processor = await GetEmployeeAsync(processorEmployeeCode);
        var entity = await GetForProcessingAsync(id, processor);

        var chosen = new Dictionary<(DateOnly, DaySession), SessionStatus>();
        foreach (var item in request.Items)
        {
            if (!Enum.TryParse<DaySession>(item.Session, true, out var daySession))
            {
                throw new InvalidOperationException($"Buổi '{item.Session}' không hợp lệ.");
            }

            if (!Enum.TryParse<SessionStatus>(item.Status, true, out var status) || status is not (SessionStatus.OnTime or SessionStatus.Late))
            {
                throw new InvalidOperationException("Trạng thái đi làm chỉ có thể là OnTime (đúng giờ) hoặc Late (đi muộn).");
            }

            chosen[(item.Date, daySession)] = status;
        }

        foreach (var session in entity.Sessions.Where(s => s.State == ReturnSessionState.AwaitingHr))
        {
            session.State = ReturnSessionState.Active;
            session.WorkedStatus = chosen.TryGetValue((session.Date, session.Session), out var status) ? status : SessionStatus.OnTime;
        }

        entity.ProcessedBy = processor.Id;
        entity.ProcessedAt = VietnamClock.Now;
        entity.Status = ReturnRequestStatus.Completed;
        entity.UpdatedAt = VietnamClock.Now;

        await _repository.SaveChangesAsync();
        await LogAsync("Process", entity.Id);

        await _notificationService.NotifyAsync(
            entity.EmployeeId, "ReturnProcessed", "Nhân sự đã xác nhận bạn đi làm",
            "Các buổi đi làm lại của bạn đã được xác nhận, phép đã được hoàn lại.", "/employee/leave", entity.Id);
        return ToDto(entity);
    }

    public async Task<LeaveReturnRequestDto> RejectProcessingAsync(long id, string processorEmployeeCode, string? reason)
    {
        var processor = await GetEmployeeAsync(processorEmployeeCode);
        var entity = await GetForProcessingAsync(id, processor);

        // Từ chối phần cần xác nhận: các buổi đó vẫn là nghỉ phép. Phần đã có hiệu lực lúc Manager duyệt (buổi chưa tới) giữ nguyên.
        foreach (var session in entity.Sessions.Where(s => s.State == ReturnSessionState.AwaitingHr))
        {
            session.State = ReturnSessionState.Rejected;
        }

        entity.ProcessedBy = processor.Id;
        entity.ProcessedAt = VietnamClock.Now;
        entity.RejectionReason = reason;
        entity.Status = entity.Sessions.Any(s => s.State == ReturnSessionState.Active)
            ? ReturnRequestStatus.Completed
            : ReturnRequestStatus.Rejected;
        entity.UpdatedAt = VietnamClock.Now;

        await _repository.SaveChangesAsync();
        await LogAsync("RejectProcessing", entity.Id);

        await _notificationService.NotifyAsync(
            entity.EmployeeId, "ReturnRejected", "Yêu cầu rút ngắn nghỉ phép bị từ chối xác nhận",
            "Nhân sự không xác nhận các buổi đã bắt đầu, những buổi đó vẫn tính là nghỉ phép."
            + (string.IsNullOrWhiteSpace(reason) ? string.Empty : $" Lý do: {reason}"),
            "/employee/leave", entity.Id);
        return ToDto(entity);
    }

    // ---------------- Hỗ trợ ----------------

    private async Task<Employee> GetEmployeeAsync(string employeeCode) =>
        await _employeeRepository.GetByEmployeeCodeAsync(employeeCode)
            ?? throw new InvalidOperationException($"Không tìm thấy nhân viên có mã '{employeeCode}'.");

    private async Task<LeaveReturnRequest> GetForManagerAsync(long id, Employee manager, bool isAdmin)
    {
        var request = await _repository.GetByIdAsync(id)
            ?? throw new InvalidOperationException($"Yêu cầu rút ngắn nghỉ phép {id} không tồn tại.");

        if (!isAdmin)
        {
            var effectiveApprover = await _approvalDelegationResolver.ResolveApproverAsync(request.Employee);
            if (effectiveApprover?.Id != manager.Id)
            {
                throw new InvalidOperationException("Bạn không phải quản lý trực tiếp của nhân viên này.");
            }
        }

        if (request.Status != ReturnRequestStatus.PendingManager)
        {
            throw new InvalidOperationException("Chỉ duyệt hoặc từ chối được yêu cầu đang chờ Manager.");
        }

        return request;
    }

    private async Task<LeaveReturnRequest> GetForProcessingAsync(long id, Employee processor)
    {
        var request = await _repository.GetByIdAsync(id)
            ?? throw new InvalidOperationException($"Yêu cầu rút ngắn nghỉ phép {id} không tồn tại.");

        if (request.Status != ReturnRequestStatus.PendingHr)
        {
            throw new InvalidOperationException("Yêu cầu này không ở trạng thái chờ xử lý.");
        }

        // Không tự xử lý yêu cầu của chính mình, để người xác nhận luôn là người khác.
        if (request.EmployeeId == processor.Id)
        {
            throw new InvalidOperationException("Bạn không thể xử lý yêu cầu rút ngắn nghỉ phép của chính mình.");
        }

        // Người đã duyệt (bước 1) không được tự xác nhận (bước 2).
        if (request.ManagerId == processor.Id)
        {
            throw new InvalidOperationException("Bạn đã là người duyệt yêu cầu này nên không thể tự xác nhận. Cần một người nhân sự khác xử lý.");
        }

        await _periodLock.EnsureOpenAsync(request.FromDate, request.Sessions.Max(s => s.Date));

        return request;
    }

    private static DaySession ParseSession(string session) =>
        Enum.TryParse<DaySession>(session, true, out var parsed)
            ? parsed
            : throw new InvalidOperationException("Vui lòng chọn buổi bắt đầu đi làm lại (Morning hoặc Afternoon).");

    private static DateTime SessionStart(DateOnly date, DaySession session) =>
        date.ToDateTime(TimeOnly.MinValue) + (session == DaySession.Morning ? MorningStart : AfternoonStart);

    // Chỉ được xin đi làm lại từ buổi chưa bắt đầu. Buổi đã bắt đầu hoặc đã qua không xin được nữa.
    private static void ValidateNotStarted(DateOnly date, DaySession session, DateTime now)
    {
        if (EffectiveDayCalculator.IsNonWorkingDay(date))
        {
            throw new InvalidOperationException("Ngày đi làm lại phải là ngày làm việc (thứ Hai đến thứ Sáu, không phải ngày lễ).");
        }

        if (SessionStart(date, session) <= now)
        {
            var (earliestDate, earliestSession) = EarliestSlot(now);
            var label = earliestSession == DaySession.Morning ? "buổi sáng" : "buổi chiều";
            throw new InvalidOperationException(
                $"Buổi này đã bắt đầu hoặc đã qua nên không rút ngắn nghỉ phép được. Buổi sớm nhất có thể xin: {label} {earliestDate:dd/MM/yyyy}.");
        }
    }

    // Buổi làm việc đầu tiên chưa bắt đầu tính từ thời điểm hiện tại.
    private static (DateOnly Date, DaySession Session) EarliestSlot(DateTime now)
    {
        var date = DateOnly.FromDateTime(now);
        var time = now.TimeOfDay;

        DaySession session;
        if (EffectiveDayCalculator.IsNonWorkingDay(date) || time >= AfternoonStart)
        {
            date = date.AddDays(1);
            session = DaySession.Morning;
        }
        else
        {
            session = time < MorningStart ? DaySession.Morning : DaySession.Afternoon;
        }

        while (EffectiveDayCalculator.IsNonWorkingDay(date))
        {
            date = date.AddDays(1);
            session = DaySession.Morning;
        }

        return (date, session);
    }

    // Từ (ngày, buổi) bắt đầu, lấy các buổi nghỉ phép hiệu lực nối liền nhau (bỏ qua cuối tuần) cho tới khi gặp buổi đi làm
    // HOẶC buổi thuộc một đơn nghỉ khác. Chỉ huỷ các buổi trong CÙNG MỘT đơn nghỉ với buổi bắt đầu: hai đơn nghỉ liền kề
    // là hai đơn riêng, xin đi làm lại ở đơn trước không kéo theo huỷ đơn sau.
    private async Task<List<(DateOnly Date, DaySession Session, long LeaveRequestId, string LeaveTypeName)>> ResolveTailAsync(
        long employeeId, DateOnly fromDate, DaySession fromSession)
    {
        var horizon = fromDate.AddDays(400);
        var inputs = await _effectiveRepository.LoadAsync(new[] { employeeId }, fromDate, horizon);

        var covered = new Dictionary<(DateOnly, DaySession), (long RequestId, string TypeName)>();
        foreach (var leave in inputs.Leaves)
        {
            foreach (var (date, session) in EffectiveDayCalculator.CoveredSessions(leave, inputs.Returns))
            {
                covered[(date, session)] = (leave.RequestId, leave.LeaveTypeName);
            }
        }

        if (!covered.ContainsKey((fromDate, fromSession)))
        {
            throw new InvalidOperationException("Buổi này bạn không có nghỉ phép đã duyệt đang còn hiệu lực nên không cần rút ngắn nghỉ phép.");
        }

        var result = new List<(DateOnly, DaySession, long, string)>();
        var cursor = (Date: fromDate, Session: fromSession);
        while (cursor.Date <= horizon)
        {
            if (EffectiveDayCalculator.IsNonWorkingDay(cursor.Date))
            {
                cursor = (cursor.Date.AddDays(1), DaySession.Morning);
                continue;
            }

            if (!covered.TryGetValue(cursor, out var info)) break;

            // Gặp buổi thuộc đơn nghỉ khác thì dừng, không kéo theo đơn đó.
            if (result.Count > 0 && info.RequestId != result[0].Item3) break;

            result.Add((cursor.Date, cursor.Session, info.RequestId, info.TypeName));
            cursor = cursor.Session == DaySession.Morning
                ? (cursor.Date, DaySession.Afternoon)
                : (cursor.Date.AddDays(1), DaySession.Morning);
        }

        // Rút ngắn chỉ cắt phần CÒN LẠI của một kỳ nghỉ đã bắt đầu. Nếu buổi bắt đầu rút ngắn chính là buổi nghỉ đầu tiên của đơn
        // thì thực chất là bỏ cả kỳ nghỉ: phải dùng "Xin hủy đơn nghỉ phép" (cần quản lý duyệt, không qua nhân sự).
        if (result.Count > 0)
        {
            var requestId = result[0].Item3;
            var firstOfRequest = covered
                .Where(kv => kv.Value.RequestId == requestId)
                .Select(kv => kv.Key)
                .OrderBy(k => k.Item1).ThenBy(k => k.Item2)
                .First();
            if (firstOfRequest == (result[0].Item1, result[0].Item2))
            {
                throw new InvalidOperationException(
                    "Kỳ nghỉ này chưa bắt đầu nên không thể rút ngắn. Nếu không đi nghỉ nữa, hãy dùng chức năng \"Xin hủy đơn nghỉ phép\".");
            }
        }

        return result;
    }

    private static LeaveReturnRequestDto ToDto(LeaveReturnRequest r) => new()
    {
        Id = r.Id,
        EmployeeCode = r.Employee.EmployeeCode,
        EmployeeName = $"{r.Employee.FirstName} {r.Employee.LastName}",
        FromDate = r.FromDate,
        FromSession = r.FromSession.ToString(),
        Reason = r.Reason,
        Status = r.Status.ToString(),
        Sessions = r.Sessions
            .OrderBy(s => s.Date).ThenBy(s => s.Session)
            .Select(s => new LeaveReturnSessionDto
            {
                Date = s.Date, Session = s.Session.ToString(), LeaveRequestId = s.LeaveRequestId,
                LeaveTypeName = s.LeaveRequest?.LeaveType?.LeaveTypeName ?? string.Empty,
                State = s.State.ToString(), WorkedStatus = s.WorkedStatus?.ToString()
            }).ToList(),
        ManagerName = r.Manager is null ? null : $"{r.Manager.FirstName} {r.Manager.LastName}",
        ManagerApprovedAt = r.ManagerApprovedAt,
        ProcessedByName = r.Processor is null ? null : $"{r.Processor.FirstName} {r.Processor.LastName}",
        ProcessedAt = r.ProcessedAt,
        RejectionReason = r.RejectionReason,
        CreatedAt = r.CreatedAt
    };
}
