using EnterpriseManagement.Application.Common;
using EnterpriseManagement.Application.DTOs;
using EnterpriseManagement.Application.Interfaces;
using EnterpriseManagement.Domain.Entities.Leave;
using EnterpriseManagement.Domain.Enums;

namespace EnterpriseManagement.Application.Services;

public class LeaveRequestService : ILeaveRequestService
{
    private readonly ILeaveRequestRepository _leaveRequestRepository;
    private readonly ILeaveTypeRepository _leaveTypeRepository;
    private readonly ILeaveBalanceService _leaveBalanceService;
    private readonly IEmployeeRepository _employeeRepository;

    public LeaveRequestService(
        ILeaveRequestRepository leaveRequestRepository,
        ILeaveTypeRepository leaveTypeRepository,
        ILeaveBalanceService leaveBalanceService,
        IEmployeeRepository employeeRepository)
    {
        _leaveRequestRepository = leaveRequestRepository;
        _leaveTypeRepository = leaveTypeRepository;
        _leaveBalanceService = leaveBalanceService;
        _employeeRepository = employeeRepository;
    }

    public async Task<LeaveRequestDto> SubmitAsync(SubmitLeaveRequest request, string employeeCode)
    {
        var employee = await _employeeRepository.GetByEmployeeCodeAsync(employeeCode)
            ?? throw new InvalidOperationException($"Employee code '{employeeCode}' not found.");

        var leaveType = await _leaveTypeRepository.GetByCodeAsync(request.LeaveTypeCode)
            ?? throw new InvalidOperationException($"Leave type code '{request.LeaveTypeCode}' not found.");

        var (startDate, endDate, session, totalTime) = ResolveRequestedTime(leaveType, request, DateTime.Now);

        var balance = await _leaveBalanceService.GetOrCreateAsync(employee, leaveType, startDate);
        if (balance.RemainingTime < totalTime)
        {
            throw new InvalidOperationException(
                $"Insufficient leave balance: remaining {balance.RemainingTime} {balance.Unit}, requested {totalTime} {leaveType.AccrualUnit}.");
        }

        var leaveRequest = new LeaveRequest
        {
            EmployeeId = employee.Id,
            LeaveTypeId = leaveType.Id,
            StartDate = startDate,
            EndDate = endDate,
            Session = session,
            Unit = leaveType.AccrualUnit,
            TotalTime = totalTime,
            Reason = request.Reason,
            Status = LeaveRequestStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        await _leaveRequestRepository.AddAsync(leaveRequest);
        await _leaveRequestRepository.SaveChangesAsync();

        var created = await _leaveRequestRepository.GetByIdAsync(leaveRequest.Id);
        return ToDto(created!);
    }

    // Các khung giờ nghỉ ngắn cố định 30 phút, theo giờ hành chính (sáng 8h-12h, chiều
    // 13h-17h — nghỉ trưa 12h-13h không tính). Mỗi đơn nghỉ ngắn = đúng 1 khung.
    private static readonly TimeSpan[] ShortLeaveSlotStarts =
    {
        new(8, 0, 0), new(8, 30, 0), new(9, 0, 0), new(9, 30, 0),
        new(10, 0, 0), new(10, 30, 0), new(11, 0, 0), new(11, 30, 0),
        new(13, 0, 0), new(13, 30, 0), new(14, 0, 0), new(14, 30, 0),
        new(15, 0, 0), new(15, 30, 0), new(16, 0, 0), new(16, 30, 0),
    };

    // Nghỉ ngắn (AccrualPeriod = MonthlyReset): luôn là ngày hôm nay (ngày submit), client chỉ
    // chọn 1 trong các khung 30 phút cố định ở trên — không cho nhập giờ tự do.
    // Các loại còn lại: client chọn buổi (Session), backend tự gán giờ cố định theo buổi.
    // Nghỉ nhiều ngày bắt buộc chọn "Cả ngày" cho toàn bộ khoảng. Riêng "Nghỉ có phép"
    // (ANNUAL) phải xin trước ít nhất 1 ngày làm việc để có người duyệt kịp sắp xếp công
    // việc: submit trước/đúng 17h thì sớm nhất là xin cho ngày mai, submit sau 17h thì
    // sớm nhất là xin cho ngày kia.
    private static (DateTime StartDate, DateTime EndDate, LeaveSession? Session, decimal TotalTime) ResolveRequestedTime(
        LeaveType leaveType, SubmitLeaveRequest request, DateTime now)
    {
        if (leaveType.AccrualPeriod == LeaveAccrualPeriod.MonthlyReset)
        {
            if (request.StartDate.Date != now.Date)
            {
                throw new InvalidOperationException("Nghỉ ngắn chỉ được xin cho ngày hôm nay.");
            }
            if (!ShortLeaveSlotStarts.Contains(request.StartDate.TimeOfDay))
            {
                throw new InvalidOperationException("Vui lòng chọn một khung giờ 30 phút hợp lệ trong giờ hành chính.");
            }
            if (request.EndDate != request.StartDate.AddMinutes(30))
            {
                throw new InvalidOperationException("Mỗi đơn nghỉ ngắn chỉ áp dụng cho đúng 1 khung 30 phút.");
            }

            return (request.StartDate, request.EndDate, null, 0.5m);
        }

        if (!Enum.TryParse<LeaveSession>(request.Session, true, out var session))
        {
            throw new InvalidOperationException("Vui lòng chọn buổi nghỉ (Morning, Afternoon hoặc FullDay).");
        }

        var startOnly = request.StartDate.Date;
        var endOnly = request.EndDate.Date;
        if (startOnly > endOnly)
        {
            throw new InvalidOperationException("Ngày kết thúc phải sau hoặc bằng ngày bắt đầu.");
        }
        if (startOnly != endOnly && session != LeaveSession.FullDay)
        {
            throw new InvalidOperationException("Nghỉ nhiều ngày chỉ được chọn buổi Cả ngày.");
        }

        if (leaveType.LeaveTypeCode == "ANNUAL")
        {
            var minDate = now.TimeOfDay <= new TimeSpan(17, 0, 0) ? now.Date.AddDays(1) : now.Date.AddDays(2);
            if (startOnly < minDate)
            {
                throw new InvalidOperationException(
                    $"Nghỉ có phép phải xin trước ít nhất 1 ngày để quản lý kịp duyệt. Ngày sớm nhất có thể xin: {minDate:dd/MM/yyyy}.");
            }
        }

        var (start, end) = session switch
        {
            LeaveSession.Morning => (startOnly.AddHours(8), startOnly.AddHours(12)),
            LeaveSession.Afternoon => (startOnly.AddHours(13), startOnly.AddHours(17)),
            LeaveSession.FullDay => (startOnly.AddHours(8), endOnly.AddHours(17)),
            _ => throw new InvalidOperationException("Buổi nghỉ không hợp lệ.")
        };

        var totalTime = session == LeaveSession.FullDay
            ? DateRangeHelper.CountWeekdays(DateOnly.FromDateTime(startOnly), DateOnly.FromDateTime(endOnly))
            : 0.5m;

        return (start, end, session, totalTime);
    }

    public async Task<IEnumerable<LeaveRequestDto>> GetByEmployeeAsync(string employeeCode)
    {
        var employee = await _employeeRepository.GetByEmployeeCodeAsync(employeeCode)
            ?? throw new InvalidOperationException($"Employee code '{employeeCode}' not found.");

        var requests = await _leaveRequestRepository.GetByEmployeeIdAsync(employee.Id);
        return requests.Select(ToDto);
    }

    public async Task<LeaveRequestDto> CancelAsync(long leaveRequestId, string employeeCode)
    {
        var leaveRequest = await _leaveRequestRepository.GetByIdAsync(leaveRequestId)
            ?? throw new InvalidOperationException($"Leave request {leaveRequestId} not found.");

        if (leaveRequest.Employee.EmployeeCode != employeeCode)
        {
            throw new InvalidOperationException("Only the employee who submitted this leave request can cancel it.");
        }

        if (leaveRequest.Status != LeaveRequestStatus.Pending)
        {
            throw new InvalidOperationException("Only pending leave requests can be cancelled.");
        }

        leaveRequest.Status = LeaveRequestStatus.Cancelled;
        leaveRequest.UpdatedAt = DateTime.UtcNow;

        await _leaveRequestRepository.SaveChangesAsync();

        return ToDto(leaveRequest);
    }

    public async Task<IEnumerable<LeaveRequestDto>> GetPendingAsync(string requesterEmployeeCode, bool isAdmin)
    {
        var requests = await _leaveRequestRepository.GetPendingAsync();
        return await FilterByTeamAsync(requests, requesterEmployeeCode, isAdmin);
    }

    public async Task<IEnumerable<LeaveRequestDto>> GetHistoryAsync(string requesterEmployeeCode, bool isAdmin)
    {
        var requests = await _leaveRequestRepository.GetAllAsync();
        return await FilterByTeamAsync(requests, requesterEmployeeCode, isAdmin);
    }

    // ADMIN thấy toàn bộ. Manager chỉ thấy đơn của người mình quản lý trực tiếp
    // (Employee.ManagerId), không thấy đơn của phòng ban/người khác.
    private async Task<IEnumerable<LeaveRequestDto>> FilterByTeamAsync(
        IEnumerable<LeaveRequest> requests, string requesterEmployeeCode, bool isAdmin)
    {
        if (isAdmin)
        {
            return requests.Select(ToDto);
        }

        var requester = await _employeeRepository.GetByEmployeeCodeAsync(requesterEmployeeCode)
            ?? throw new InvalidOperationException($"Employee code '{requesterEmployeeCode}' not found.");

        return requests.Where(r => r.Employee.ManagerId == requester.Id).Select(ToDto);
    }

    public async Task<LeaveRequestDto> ApproveAsync(long leaveRequestId, string approverEmployeeCode, bool isAdmin)
    {
        var approver = await _employeeRepository.GetByEmployeeCodeAsync(approverEmployeeCode)
            ?? throw new InvalidOperationException($"Employee code '{approverEmployeeCode}' not found.");

        var leaveRequest = await _leaveRequestRepository.GetByIdAsync(leaveRequestId)
            ?? throw new InvalidOperationException($"Leave request {leaveRequestId} not found.");

        if (!isAdmin && leaveRequest.Employee.ManagerId != approver.Id)
        {
            throw new InvalidOperationException("Bạn không phải quản lý trực tiếp của nhân viên này.");
        }

        if (leaveRequest.Status != LeaveRequestStatus.Pending)
        {
            throw new InvalidOperationException("Only pending leave requests can be approved.");
        }

        leaveRequest.Status = LeaveRequestStatus.Approved;
        leaveRequest.ApprovedBy = approver.Id;
        leaveRequest.Approver = approver;
        leaveRequest.ApprovedAt = DateTime.UtcNow;
        leaveRequest.UpdatedAt = DateTime.UtcNow;

        var balance = await _leaveBalanceService.GetOrCreateAsync(leaveRequest.Employee, leaveRequest.LeaveType, leaveRequest.StartDate);
        balance.UsedTime += leaveRequest.TotalTime;
        balance.RemainingTime -= leaveRequest.TotalTime;
        balance.UpdatedAt = DateTime.UtcNow;

        await _leaveRequestRepository.SaveChangesAsync();

        return ToDto(leaveRequest);
    }

    public async Task<LeaveRequestDto> RejectAsync(long leaveRequestId, string approverEmployeeCode, string? rejectionReason, bool isAdmin)
    {
        var approver = await _employeeRepository.GetByEmployeeCodeAsync(approverEmployeeCode)
            ?? throw new InvalidOperationException($"Employee code '{approverEmployeeCode}' not found.");

        var leaveRequest = await _leaveRequestRepository.GetByIdAsync(leaveRequestId)
            ?? throw new InvalidOperationException($"Leave request {leaveRequestId} not found.");

        if (!isAdmin && leaveRequest.Employee.ManagerId != approver.Id)
        {
            throw new InvalidOperationException("Bạn không phải quản lý trực tiếp của nhân viên này.");
        }

        if (leaveRequest.Status != LeaveRequestStatus.Pending)
        {
            throw new InvalidOperationException("Only pending leave requests can be rejected.");
        }

        leaveRequest.Status = LeaveRequestStatus.Rejected;
        leaveRequest.ApprovedBy = approver.Id;
        leaveRequest.Approver = approver;
        leaveRequest.ApprovedAt = DateTime.UtcNow;
        leaveRequest.RejectionReason = rejectionReason;
        leaveRequest.UpdatedAt = DateTime.UtcNow;

        await _leaveRequestRepository.SaveChangesAsync();

        return ToDto(leaveRequest);
    }

    private static LeaveRequestDto ToDto(LeaveRequest leaveRequest) => new()
    {
        Id = leaveRequest.Id,
        EmployeeCode = leaveRequest.Employee.EmployeeCode,
        EmployeeName = $"{leaveRequest.Employee.FirstName} {leaveRequest.Employee.LastName}",
        LeaveTypeCode = leaveRequest.LeaveType.LeaveTypeCode,
        LeaveTypeName = leaveRequest.LeaveType.LeaveTypeName,
        StartDate = leaveRequest.StartDate,
        EndDate = leaveRequest.EndDate,
        Session = leaveRequest.Session?.ToString(),
        Unit = leaveRequest.Unit.ToString(),
        TotalTime = leaveRequest.TotalTime,
        Reason = leaveRequest.Reason,
        Status = leaveRequest.Status.ToString(),
        ApproverName = leaveRequest.Approver is null ? null : $"{leaveRequest.Approver.FirstName} {leaveRequest.Approver.LastName}",
        ApprovedAt = leaveRequest.ApprovedAt,
        RejectionReason = leaveRequest.RejectionReason
    };
}
