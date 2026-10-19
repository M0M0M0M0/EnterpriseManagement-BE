using EnterpriseManagement.Application.Common;
using EnterpriseManagement.Application.DTOs;
using EnterpriseManagement.Application.Interfaces;
using EnterpriseManagement.Domain.Entities.HR;
using EnterpriseManagement.Domain.Entities.Leave;
using EnterpriseManagement.Domain.Enums;

namespace EnterpriseManagement.Application.Services;

// Xin hủy đơn nghỉ phép đã duyệt. Chỉ hủy được khi CHƯA có buổi nghỉ nào bắt đầu; kỳ nghỉ đã bắt đầu thì chỉ rút ngắn được
// (LeaveReturnService). Quản lý duyệt như đơn nghỉ; được duyệt thì đơn gốc thành Cancelled và phép đã trừ được hoàn lại.
public class LeaveCancelService : ILeaveCancelService
{
    private readonly ILeaveCancelRepository _repository;
    private readonly ILeaveRequestRepository _leaveRequestRepository;
    private readonly ILeaveBalanceService _leaveBalanceService;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IApprovalDelegationResolver _approvalDelegationResolver;
    private readonly IPeriodLockService _periodLock;
    private readonly INotificationService _notificationService;
    private readonly IAuditLogService _auditLogService;
    private readonly ICurrentUserService _currentUserService;

    public LeaveCancelService(
        ILeaveCancelRepository repository,
        ILeaveRequestRepository leaveRequestRepository,
        ILeaveBalanceService leaveBalanceService,
        IEmployeeRepository employeeRepository,
        IApprovalDelegationResolver approvalDelegationResolver,
        IPeriodLockService periodLock,
        INotificationService notificationService,
        IAuditLogService auditLogService,
        ICurrentUserService currentUserService)
    {
        _repository = repository;
        _leaveRequestRepository = leaveRequestRepository;
        _leaveBalanceService = leaveBalanceService;
        _employeeRepository = employeeRepository;
        _approvalDelegationResolver = approvalDelegationResolver;
        _periodLock = periodLock;
        _notificationService = notificationService;
        _auditLogService = auditLogService;
        _currentUserService = currentUserService;
    }

    private Task LogAsync(string action, long entityId) =>
        _auditLogService.LogAsync(_currentUserService.UserId, action, "LeaveCancelRequest", entityId, _currentUserService.IpAddress);

    private async Task<Employee> GetEmployeeAsync(string code) =>
        await _employeeRepository.GetByEmployeeCodeAsync(code)
            ?? throw new InvalidOperationException($"Không tìm thấy nhân viên có mã '{code}'.");

    // ---------------- Nhân viên ----------------

    public async Task<LeaveCancelRequestDto> SubmitAsync(SubmitLeaveCancelRequest request, string employeeCode)
    {
        var employee = await GetEmployeeAsync(employeeCode);
        if (!employee.TracksAttendance) throw new InvalidOperationException(SystemAccount.NotTrackedMessage);

        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new InvalidOperationException("Vui lòng nhập lý do xin hủy đơn nghỉ phép.");
        }

        var leave = await _leaveRequestRepository.GetByIdAsync(request.LeaveRequestId)
            ?? throw new InvalidOperationException($"Không tìm thấy đơn nghỉ phép {request.LeaveRequestId}.");

        if (leave.EmployeeId != employee.Id)
        {
            throw new InvalidOperationException("Chỉ nhân viên đã gửi đơn nghỉ này mới được xin hủy.");
        }

        if (leave.Status != LeaveRequestStatus.Approved)
        {
            throw new InvalidOperationException(leave.Status == LeaveRequestStatus.Pending
                ? "Đơn nghỉ đang chờ duyệt, bạn có thể huỷ trực tiếp mà không cần xin hủy."
                : "Chỉ xin hủy được đơn nghỉ phép đã được duyệt.");
        }

        EnsureNotStarted(leave);

        if (await _repository.HasPendingAsync(leave.Id))
        {
            throw new InvalidOperationException("Đơn nghỉ này đã có yêu cầu hủy đang chờ duyệt.");
        }

        await _periodLock.EnsureOpenAsync(DateOnly.FromDateTime(leave.StartDate), DateOnly.FromDateTime(leave.EndDate));

        var cancel = new LeaveCancelRequest
        {
            LeaveRequestId = leave.Id,
            EmployeeId = employee.Id,
            Reason = request.Reason.Trim(),
            Status = LeaveCancelStatus.Pending,
            CreatedAt = VietnamClock.Now
        };
        await _repository.AddAsync(cancel);
        await _repository.SaveChangesAsync();
        await LogAsync("Submit", cancel.Id);

        await _notificationService.NotifyApproverAsync(
            employee, "LeaveCancelSubmitted", "Có yêu cầu hủy đơn nghỉ phép",
            $"{NotificationText.Name(employee)} xin hủy đơn {leave.LeaveType.LeaveTypeName.ToLowerInvariant()} {NotificationText.Range(leave.StartDate, leave.EndDate)}.",
            "/manager/leave?tab=cancels", cancel.Id);

        return ToDto((await _repository.GetByIdAsync(cancel.Id))!);
    }

    // Đơn nghỉ chỉ hủy được khi chưa có buổi nào bắt đầu: kỳ nghỉ đã bắt đầu thì dùng "Rút ngắn nghỉ phép".
    private static void EnsureNotStarted(LeaveRequest leave)
    {
        if (leave.StartDate <= VietnamClock.Now)
        {
            throw new InvalidOperationException(
                "Kỳ nghỉ này đã bắt đầu nên không thể hủy cả đơn. Nếu muốn đi làm sớm, hãy dùng chức năng \"Rút ngắn nghỉ phép\".");
        }
    }

    public async Task<IEnumerable<LeaveCancelRequestDto>> GetMineAsync(string employeeCode)
    {
        var employee = await GetEmployeeAsync(employeeCode);
        return (await _repository.GetByEmployeeAsync(employee.Id)).Select(ToDto);
    }

    public async Task<LeaveCancelRequestDto> WithdrawAsync(long id, string employeeCode)
    {
        var cancel = await _repository.GetByIdAsync(id) ?? throw new InvalidOperationException($"Không tìm thấy yêu cầu hủy {id}.");
        if (cancel.Employee.EmployeeCode != employeeCode)
        {
            throw new InvalidOperationException("Chỉ nhân viên đã gửi yêu cầu này mới được rút lại.");
        }

        if (cancel.Status != LeaveCancelStatus.Pending)
        {
            throw new InvalidOperationException("Chỉ rút lại được yêu cầu hủy đang chờ duyệt.");
        }

        cancel.Status = LeaveCancelStatus.Withdrawn;
        cancel.UpdatedAt = VietnamClock.Now;
        await _repository.SaveChangesAsync();
        await LogAsync("Withdraw", cancel.Id);
        return ToDto(cancel);
    }

    // ---------------- Quản lý ----------------

    public async Task<IEnumerable<LeaveCancelRequestDto>> GetPendingAsync(string requesterEmployeeCode, bool isAdmin) =>
        await FilterByTeamAsync(await _repository.GetPendingAsync(), requesterEmployeeCode, isAdmin);

    public async Task<IEnumerable<LeaveCancelRequestDto>> GetHistoryAsync(string requesterEmployeeCode, bool isAdmin) =>
        await FilterByTeamAsync(await _repository.GetAllAsync(), requesterEmployeeCode, isAdmin);

    private async Task<IEnumerable<LeaveCancelRequestDto>> FilterByTeamAsync(
        IEnumerable<LeaveCancelRequest> requests, string requesterEmployeeCode, bool isAdmin)
    {
        if (isAdmin) return requests.Select(ToDto);

        var requester = await GetEmployeeAsync(requesterEmployeeCode);
        var result = new List<LeaveCancelRequestDto>();
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

    public async Task<LeaveCancelRequestDto> ApproveAsync(long id, string approverEmployeeCode, bool isAdmin)
    {
        var (cancel, approver) = await GetForDecisionAsync(id, approverEmployeeCode, isAdmin);
        var leave = cancel.LeaveRequest;

        if (leave.Status != LeaveRequestStatus.Approved)
        {
            throw new InvalidOperationException("Đơn nghỉ gốc không còn ở trạng thái đã duyệt nên không thể hủy.");
        }

        // Quá giờ bắt đầu kỳ nghỉ rồi mới duyệt thì không hủy cả đơn được nữa.
        EnsureNotStarted(leave);
        await _periodLock.EnsureOpenAsync(DateOnly.FromDateTime(leave.StartDate), DateOnly.FromDateTime(leave.EndDate));

        var now = VietnamClock.Now;
        leave.Status = LeaveRequestStatus.Cancelled;
        leave.UpdatedAt = now;

        // Hoàn phép đã trừ lúc duyệt đơn (nghỉ không lương không có số dư nên không hoàn).
        if (leave.LeaveType.IsPaid)
        {
            var balance = await _leaveBalanceService.GetOrCreateAsync(leave.Employee, leave.LeaveType, leave.StartDate);
            balance.UsedTime = Math.Max(0, balance.UsedTime - leave.TotalTime);
            balance.RemainingTime += leave.TotalTime;
            balance.UpdatedAt = now;
        }

        cancel.Status = LeaveCancelStatus.Approved;
        cancel.ManagerId = approver.Id;
        cancel.DecidedAt = now;
        cancel.UpdatedAt = now;
        await _repository.SaveChangesAsync();
        await LogAsync("Approve", cancel.Id);

        await _notificationService.NotifyAsync(
            cancel.EmployeeId, "LeaveCancelApproved", "Đơn nghỉ phép đã được hủy",
            $"{NotificationText.Name(approver)} đã đồng ý hủy đơn {leave.LeaveType.LeaveTypeName.ToLowerInvariant()} {NotificationText.Range(leave.StartDate, leave.EndDate)}, phép đã được hoàn lại.",
            "/employee/leave", cancel.Id);
        return ToDto(cancel);
    }

    public async Task<LeaveCancelRequestDto> RejectAsync(long id, string approverEmployeeCode, string? reason, bool isAdmin)
    {
        var (cancel, approver) = await GetForDecisionAsync(id, approverEmployeeCode, isAdmin);

        cancel.Status = LeaveCancelStatus.Rejected;
        cancel.ManagerId = approver.Id;
        cancel.DecidedAt = VietnamClock.Now;
        cancel.RejectionReason = reason;
        cancel.UpdatedAt = VietnamClock.Now;
        await _repository.SaveChangesAsync();
        await LogAsync("Reject", cancel.Id);

        await _notificationService.NotifyAsync(
            cancel.EmployeeId, "LeaveCancelRejected", "Yêu cầu hủy đơn nghỉ phép bị từ chối",
            $"{NotificationText.Name(approver)} không đồng ý hủy đơn nghỉ {NotificationText.Range(cancel.LeaveRequest.StartDate, cancel.LeaveRequest.EndDate)}."
            + (string.IsNullOrWhiteSpace(reason) ? string.Empty : $" Lý do: {reason}"),
            "/employee/leave", cancel.Id);
        return ToDto(cancel);
    }

    private async Task<(LeaveCancelRequest Cancel, Employee Approver)> GetForDecisionAsync(long id, string approverEmployeeCode, bool isAdmin)
    {
        var approver = await GetEmployeeAsync(approverEmployeeCode);
        var cancel = await _repository.GetByIdAsync(id) ?? throw new InvalidOperationException($"Không tìm thấy yêu cầu hủy {id}.");

        if (!isAdmin)
        {
            var effectiveApprover = await _approvalDelegationResolver.ResolveApproverAsync(cancel.Employee);
            if (effectiveApprover?.Id != approver.Id)
            {
                throw new InvalidOperationException("Bạn không phải quản lý trực tiếp của nhân viên này.");
            }
        }

        if (cancel.Status != LeaveCancelStatus.Pending)
        {
            throw new InvalidOperationException("Chỉ xử lý được yêu cầu hủy đang chờ duyệt.");
        }

        return (cancel, approver);
    }

    private static LeaveCancelRequestDto ToDto(LeaveCancelRequest c) => new()
    {
        Id = c.Id,
        LeaveRequestId = c.LeaveRequestId,
        EmployeeCode = c.Employee.EmployeeCode,
        EmployeeName = NotificationText.Name(c.Employee),
        LeaveTypeName = c.LeaveRequest.LeaveType.LeaveTypeName,
        StartDate = c.LeaveRequest.StartDate,
        EndDate = c.LeaveRequest.EndDate,
        Session = c.LeaveRequest.Session?.ToString(),
        Unit = c.LeaveRequest.Unit.ToString(),
        TotalTime = c.LeaveRequest.TotalTime,
        Reason = c.Reason,
        Status = c.Status.ToString(),
        ManagerName = c.Manager is null ? null : NotificationText.Name(c.Manager),
        DecidedAt = c.DecidedAt,
        RejectionReason = c.RejectionReason,
        CreatedAt = c.CreatedAt
    };
}
