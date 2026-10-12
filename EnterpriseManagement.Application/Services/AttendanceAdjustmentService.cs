using EnterpriseManagement.Application.Common;
using EnterpriseManagement.Application.DTOs;
using EnterpriseManagement.Application.Interfaces;
using EnterpriseManagement.Domain.Entities.Attendance;
using EnterpriseManagement.Domain.Enums;

namespace EnterpriseManagement.Application.Services;

public class AttendanceAdjustmentService : IAttendanceAdjustmentService
{
    private readonly IAttendanceAdjustmentRepository _adjustmentRepository;
    private readonly IAttendanceRepository _attendanceRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IApprovalDelegationResolver _approvalDelegationResolver;
    private readonly IAttendanceService _attendanceService;
    private readonly IEffectiveAttendanceService _effective;
    private readonly IAuditLogService _auditLogService;
    private readonly ICurrentUserService _currentUserService;
    private readonly INotificationService _notificationService;

    public AttendanceAdjustmentService(
        IAttendanceAdjustmentRepository adjustmentRepository,
        IAttendanceRepository attendanceRepository,
        IEmployeeRepository employeeRepository,
        IApprovalDelegationResolver approvalDelegationResolver,
        IAttendanceService attendanceService,
        IEffectiveAttendanceService effective,
        IAuditLogService auditLogService,
        ICurrentUserService currentUserService,
        INotificationService notificationService)
    {
        _adjustmentRepository = adjustmentRepository;
        _attendanceRepository = attendanceRepository;
        _employeeRepository = employeeRepository;
        _approvalDelegationResolver = approvalDelegationResolver;
        _attendanceService = attendanceService;
        _effective = effective;
        _auditLogService = auditLogService;
        _currentUserService = currentUserService;
        _notificationService = notificationService;
    }

    private Task LogAsync(string action, long? entityId) =>
        _auditLogService.LogAsync(_currentUserService.UserId, action, "AttendanceAdjustment", entityId, _currentUserService.IpAddress);

    public async Task<AttendanceAdjustmentDto> SubmitAsync(SubmitAdjustmentRequest request, string employeeCode)
    {
        var employee = await _employeeRepository.GetByEmployeeCodeAsync(employeeCode)
            ?? throw new InvalidOperationException($"Không tìm thấy nhân viên có mã '{employeeCode}'.");

        if (request.AttendanceDate > VietnamClock.Today)
        {
            throw new InvalidOperationException("Không thể xin điều chỉnh công cho ngày trong tương lai.");
        }

        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new InvalidOperationException("Vui lòng nhập lý do điều chỉnh công.");
        }

        if (request.NewCheckInTime is null && request.NewCheckOutTime is null)
        {
            throw new InvalidOperationException("Vui lòng nhập ít nhất một giờ vào hoặc giờ ra đề xuất.");
        }

        if ((request.NewCheckInTime is { } proposedIn && DateOnly.FromDateTime(proposedIn) != request.AttendanceDate)
            || (request.NewCheckOutTime is { } proposedOut && DateOnly.FromDateTime(proposedOut) != request.AttendanceDate))
        {
            throw new InvalidOperationException("Giờ đề xuất phải nằm trong chính ngày cần điều chỉnh.");
        }

        if (request.NewCheckInTime is { } inTime && request.NewCheckOutTime is { } outTime && outTime <= inTime)
        {
            throw new InvalidOperationException("Giờ ra phải sau giờ vào.");
        }

        if (await _adjustmentRepository.HasOpenAsync(employee.Id, request.AttendanceDate))
        {
            throw new InvalidOperationException("Ngày này đã có một yêu cầu điều chỉnh công đang chờ xử lý.");
        }

        // Cả ngày đang nghỉ phép thì không có giờ làm để điều chỉnh (muốn đi làm phải gửi yêu cầu đi làm lại).
        var day = await _effective.GetDayAsync(employee.Id, request.AttendanceDate);
        if (day.Morning == SessionStatus.OnLeave && day.Afternoon == SessionStatus.OnLeave)
        {
            throw new InvalidOperationException("Ngày này bạn đang nghỉ phép nên không điều chỉnh công được. Nếu đã đi làm, hãy gửi yêu cầu đi làm lại.");
        }

        var record = await _attendanceRepository.GetByEmployeeAndDateAsync(employee.Id, request.AttendanceDate);
        if (record is null)
        {
            // Nhân viên quên chấm công cả ngày đó (không check-in lẫn check-out) nên chưa có record nào —
            // tạo 1 record rỗng để đơn điều chỉnh có chỗ áp dụng vào khi được duyệt.
            record = new AttendanceRecord
            {
                EmployeeId = employee.Id,
                AttendanceDate = request.AttendanceDate,
                Status = AttendanceStatus.Absent,
                CreatedAt = VietnamClock.Now
            };
            await _attendanceRepository.AddAsync(record);
        }

        var adjustment = new AttendanceAdjustment
        {
            Attendance = record, // dùng navigation thay vì AttendanceId vì record có thể chưa có Id (mới tạo, chưa SaveChanges)
            RequestedBy = employee.Id,
            Reason = request.Reason,
            OldCheckInTime = record.CheckInTime,
            NewCheckInTime = request.NewCheckInTime,
            OldCheckOutTime = record.CheckOutTime,
            NewCheckOutTime = request.NewCheckOutTime,
            Status = ApprovalStatus.Pending,
            CreatedAt = VietnamClock.Now
        };

        await _adjustmentRepository.AddAsync(adjustment);
        await _adjustmentRepository.SaveChangesAsync();

        await _notificationService.NotifyApproverAsync(
            employee, "AdjustmentSubmitted", "Có yêu cầu điều chỉnh công mới",
            $"{NotificationText.Name(employee)} xin điều chỉnh công ngày {record.AttendanceDate:dd/MM/yyyy}.",
            "/manager/attendance", adjustment.Id);

        return ToDto(adjustment, record.AttendanceDate, employee.EmployeeCode, $"{employee.FirstName} {employee.LastName}");
    }

    public async Task<IEnumerable<AttendanceAdjustmentDto>> GetPendingAsync(string requesterEmployeeCode, bool isAdmin)
    {
        var adjustments = await _adjustmentRepository.GetPendingAsync();

        if (isAdmin)
        {
            return adjustments.Select(a => ToDto(a, a.Attendance.AttendanceDate, a.Requester.EmployeeCode, $"{a.Requester.FirstName} {a.Requester.LastName}"));
        }

        var requester = await _employeeRepository.GetByEmployeeCodeAsync(requesterEmployeeCode)
            ?? throw new InvalidOperationException($"Không tìm thấy nhân viên có mã '{requesterEmployeeCode}'.");

        var result = new List<AttendanceAdjustmentDto>();
        foreach (var a in adjustments)
        {
            // ADMIN thấy toàn bộ (đã trả ở trên). Manager thấy đơn của người mình quản lý trực
            // tiếp — hoặc của người mà quản lý trực tiếp của họ đang nghỉ phép (đẩy việc duyệt
            // lên mình), xem IApprovalDelegationResolver để biết chi tiết.
            var approver = await _approvalDelegationResolver.ResolveApproverAsync(a.Requester);
            if (approver?.Id == requester.Id)
            {
                result.Add(ToDto(a, a.Attendance.AttendanceDate, a.Requester.EmployeeCode, $"{a.Requester.FirstName} {a.Requester.LastName}"));
            }
        }

        return result;
    }

    public async Task<IEnumerable<AttendanceAdjustmentDto>> GetByEmployeeAsync(string employeeCode)
    {
        var employee = await _employeeRepository.GetByEmployeeCodeAsync(employeeCode)
            ?? throw new InvalidOperationException($"Không tìm thấy nhân viên có mã '{employeeCode}'.");

        var adjustments = await _adjustmentRepository.GetByEmployeeIdAsync(employee.Id);
        return adjustments.Select(a => ToDto(a, a.Attendance.AttendanceDate, employee.EmployeeCode,
            $"{employee.FirstName} {employee.LastName}", a.Approver));
    }

    public async Task<AttendanceAdjustmentDto> ApproveAsync(long adjustmentId, string approverEmployeeCode, bool isAdmin)
    {
        var approver = await _employeeRepository.GetByEmployeeCodeAsync(approverEmployeeCode)
            ?? throw new InvalidOperationException($"Không tìm thấy nhân viên có mã '{approverEmployeeCode}'.");

        var adjustment = await _adjustmentRepository.GetByIdAsync(adjustmentId)
            ?? throw new InvalidOperationException($"Không tìm thấy yêu cầu điều chỉnh công {adjustmentId}.");

        if (!isAdmin)
        {
            var effectiveApprover = await _approvalDelegationResolver.ResolveApproverAsync(adjustment.Requester);
            if (effectiveApprover?.Id != approver.Id)
            {
                throw new InvalidOperationException("Bạn không phải quản lý trực tiếp của nhân viên này.");
            }
        }

        if (adjustment.Status != ApprovalStatus.Pending)
        {
            throw new InvalidOperationException("Chỉ có thể duyệt yêu cầu điều chỉnh công đang chờ xử lý.");
        }

        // Manager xác nhận nhân viên có mặt và đi làm đúng như đề xuất. Bước này KHÔNG sửa dữ liệu chấm công: yêu cầu chuyển
        // sang người có quyền xử lý (HR), người đó mới áp dụng vào hệ thống (xem ApplyAsync).
        adjustment.Status = ApprovalStatus.PendingHr;
        adjustment.ApprovedBy = approver.Id;
        adjustment.ApprovedAt = VietnamClock.Now;

        await _adjustmentRepository.SaveChangesAsync();
        await LogAsync("Approve", adjustment.Id);

        await _notificationService.NotifyAsync(
            adjustment.RequestedBy, "AdjustmentManagerApproved", "Quản lý đã xác nhận điều chỉnh công",
            $"Yêu cầu điều chỉnh công ngày {adjustment.Attendance.AttendanceDate:dd/MM/yyyy} đã được quản lý xác nhận, đang chờ nhân sự áp dụng.",
            "/employee/attendance", adjustment.Id);
        await _notificationService.NotifyPermissionHoldersAsync(
            "attendance.adjustment.apply", adjustment.RequestedBy, "AdjustmentAwaitingProcessing", "Có yêu cầu điều chỉnh công chờ xử lý",
            $"Yêu cầu điều chỉnh công ngày {adjustment.Attendance.AttendanceDate:dd/MM/yyyy} của {NotificationText.Name(adjustment.Requester)} đã được quản lý xác nhận.",
            "/hr/adjustments", adjustment.Id);

        return ToDto(adjustment, adjustment.Attendance.AttendanceDate, adjustment.Requester.EmployeeCode,
            $"{adjustment.Requester.FirstName} {adjustment.Requester.LastName}", approver);
    }

    public async Task<IEnumerable<AttendanceAdjustmentDto>> GetPendingProcessingAsync()
    {
        var adjustments = await _adjustmentRepository.GetPendingHrAsync();
        return adjustments.Select(a => ToDto(a, a.Attendance.AttendanceDate, a.Requester.EmployeeCode,
            $"{a.Requester.FirstName} {a.Requester.LastName}", a.Approver));
    }

    public async Task<AttendanceAdjustmentDto> ApplyAsync(long adjustmentId, string processorEmployeeCode)
    {
        var (adjustment, processor) = await GetForProcessingAsync(adjustmentId, processorEmployeeCode);

        adjustment.Status = ApprovalStatus.Approved;
        adjustment.ProcessedBy = processor.Id;
        adjustment.ProcessedAt = VietnamClock.Now;

        // Cùng 1 DbContext (Scoped) đang theo dõi cả adjustment lẫn adjustment.Attendance,
        // nên SaveChangesAsync() dưới đây commit cả 2 thay đổi trong 1 transaction duy nhất.
        if (adjustment.NewCheckInTime.HasValue)
        {
            adjustment.Attendance.CheckInTime = adjustment.NewCheckInTime;
        }

        if (adjustment.NewCheckOutTime.HasValue)
        {
            adjustment.Attendance.CheckOutTime = adjustment.NewCheckOutTime;
        }

        // Tính lại đúng trạng thái theo giờ check-in mới bằng chính logic PunchAsync đang dùng.
        if (adjustment.Attendance.CheckInTime.HasValue)
        {
            adjustment.Attendance.Status = await _attendanceService.DetermineCheckInStatusAsync(
                adjustment.Attendance.EmployeeId, adjustment.Attendance.AttendanceDate, adjustment.Attendance.CheckInTime.Value);
        }

        if (adjustment.Attendance.CheckInTime.HasValue && adjustment.Attendance.CheckOutTime.HasValue)
        {
            adjustment.Attendance.WorkingHours =
                (decimal)(adjustment.Attendance.CheckOutTime.Value - adjustment.Attendance.CheckInTime.Value).TotalHours;
        }

        adjustment.Attendance.UpdatedAt = VietnamClock.Now;

        await _adjustmentRepository.SaveChangesAsync();
        await LogAsync("Apply", adjustment.Id);

        await _notificationService.NotifyAsync(
            adjustment.RequestedBy, "AdjustmentApplied", "Điều chỉnh công đã được áp dụng",
            $"Giờ chấm công ngày {adjustment.Attendance.AttendanceDate:dd/MM/yyyy} của bạn đã được điều chỉnh.",
            "/employee/attendance", adjustment.Id);

        return ToDto(adjustment, adjustment.Attendance.AttendanceDate, adjustment.Requester.EmployeeCode,
            $"{adjustment.Requester.FirstName} {adjustment.Requester.LastName}", adjustment.Approver);
    }

    public async Task<AttendanceAdjustmentDto> RejectProcessingAsync(long adjustmentId, string processorEmployeeCode, string? reason)
    {
        var (adjustment, processor) = await GetForProcessingAsync(adjustmentId, processorEmployeeCode);

        adjustment.Status = ApprovalStatus.Rejected;
        adjustment.ProcessedBy = processor.Id;
        adjustment.ProcessedAt = VietnamClock.Now;
        adjustment.RejectionReason = reason;

        await _adjustmentRepository.SaveChangesAsync();
        await LogAsync("RejectProcessing", adjustment.Id);

        await _notificationService.NotifyAsync(
            adjustment.RequestedBy, "AdjustmentRejected", "Yêu cầu điều chỉnh công bị từ chối",
            $"Nhân sự đã từ chối yêu cầu điều chỉnh công ngày {adjustment.Attendance.AttendanceDate:dd/MM/yyyy} của bạn."
            + (string.IsNullOrWhiteSpace(reason) ? string.Empty : $" Lý do: {reason}"),
            "/employee/attendance", adjustment.Id);

        return ToDto(adjustment, adjustment.Attendance.AttendanceDate, adjustment.Requester.EmployeeCode,
            $"{adjustment.Requester.FirstName} {adjustment.Requester.LastName}", adjustment.Approver);
    }

    private async Task<(AttendanceAdjustment Adjustment, Domain.Entities.HR.Employee Processor)> GetForProcessingAsync(
        long adjustmentId, string processorEmployeeCode)
    {
        var processor = await _employeeRepository.GetByEmployeeCodeAsync(processorEmployeeCode)
            ?? throw new InvalidOperationException($"Không tìm thấy nhân viên có mã '{processorEmployeeCode}'.");

        var adjustment = await _adjustmentRepository.GetByIdAsync(adjustmentId)
            ?? throw new InvalidOperationException($"Không tìm thấy yêu cầu điều chỉnh công {adjustmentId}.");

        if (adjustment.Status != ApprovalStatus.PendingHr)
        {
            throw new InvalidOperationException("Yêu cầu này không ở trạng thái chờ xử lý (Manager chưa duyệt hoặc đã xử lý rồi).");
        }

        // Không tự xử lý yêu cầu của chính mình, để người áp dụng luôn là người khác.
        if (adjustment.RequestedBy == processor.Id)
        {
            throw new InvalidOperationException("Bạn không thể xử lý yêu cầu điều chỉnh công của chính mình.");
        }

        return (adjustment, processor);
    }

    public async Task<AttendanceAdjustmentDto> RejectAsync(long adjustmentId, string approverEmployeeCode, bool isAdmin)
    {
        var approver = await _employeeRepository.GetByEmployeeCodeAsync(approverEmployeeCode)
            ?? throw new InvalidOperationException($"Không tìm thấy nhân viên có mã '{approverEmployeeCode}'.");

        var adjustment = await _adjustmentRepository.GetByIdAsync(adjustmentId)
            ?? throw new InvalidOperationException($"Không tìm thấy yêu cầu điều chỉnh công {adjustmentId}.");

        if (!isAdmin)
        {
            var effectiveApprover = await _approvalDelegationResolver.ResolveApproverAsync(adjustment.Requester);
            if (effectiveApprover?.Id != approver.Id)
            {
                throw new InvalidOperationException("Bạn không phải quản lý trực tiếp của nhân viên này.");
            }
        }

        if (adjustment.Status != ApprovalStatus.Pending)
        {
            throw new InvalidOperationException("Chỉ có thể từ chối yêu cầu điều chỉnh công đang chờ xử lý.");
        }

        adjustment.Status = ApprovalStatus.Rejected;
        adjustment.ApprovedBy = approver.Id;
        adjustment.ApprovedAt = VietnamClock.Now;

        await _adjustmentRepository.SaveChangesAsync();
        await LogAsync("Reject", adjustment.Id);

        await _notificationService.NotifyAsync(
            adjustment.RequestedBy, "AdjustmentRejected", "Yêu cầu điều chỉnh công bị từ chối",
            $"Yêu cầu điều chỉnh công ngày {adjustment.Attendance.AttendanceDate:dd/MM/yyyy} của bạn bị quản lý từ chối.",
            "/employee/attendance", adjustment.Id);

        return ToDto(adjustment, adjustment.Attendance.AttendanceDate, adjustment.Requester.EmployeeCode,
            $"{adjustment.Requester.FirstName} {adjustment.Requester.LastName}", approver);
    }

    private static AttendanceAdjustmentDto ToDto(
        AttendanceAdjustment adjustment, DateOnly attendanceDate, string employeeCode, string employeeName,
        Domain.Entities.HR.Employee? approver = null) => new()
    {
        Id = adjustment.Id,
        EmployeeCode = employeeCode,
        EmployeeName = employeeName,
        AttendanceDate = attendanceDate,
        Reason = adjustment.Reason,
        OldCheckInTime = adjustment.OldCheckInTime,
        NewCheckInTime = adjustment.NewCheckInTime,
        OldCheckOutTime = adjustment.OldCheckOutTime,
        NewCheckOutTime = adjustment.NewCheckOutTime,
        Status = adjustment.Status.ToString(),
        ApproverName = approver is null ? null : $"{approver.FirstName} {approver.LastName}",
        ApprovedAt = adjustment.ApprovedAt,
        ProcessedByName = adjustment.Processor is null ? null : $"{adjustment.Processor.FirstName} {adjustment.Processor.LastName}",
        ProcessedAt = adjustment.ProcessedAt,
        RejectionReason = adjustment.RejectionReason
    };
}
