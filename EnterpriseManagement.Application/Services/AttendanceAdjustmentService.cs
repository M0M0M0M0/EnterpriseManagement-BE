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

    public AttendanceAdjustmentService(
        IAttendanceAdjustmentRepository adjustmentRepository,
        IAttendanceRepository attendanceRepository,
        IEmployeeRepository employeeRepository,
        IApprovalDelegationResolver approvalDelegationResolver)
    {
        _adjustmentRepository = adjustmentRepository;
        _attendanceRepository = attendanceRepository;
        _employeeRepository = employeeRepository;
        _approvalDelegationResolver = approvalDelegationResolver;
    }

    public async Task<AttendanceAdjustmentDto> SubmitAsync(SubmitAdjustmentRequest request, string employeeCode)
    {
        var employee = await _employeeRepository.GetByEmployeeCodeAsync(employeeCode)
            ?? throw new InvalidOperationException($"Employee code '{employeeCode}' not found.");

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
            ?? throw new InvalidOperationException($"Employee code '{requesterEmployeeCode}' not found.");

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
            ?? throw new InvalidOperationException($"Employee code '{employeeCode}' not found.");

        var adjustments = await _adjustmentRepository.GetByEmployeeIdAsync(employee.Id);
        return adjustments.Select(a => ToDto(a, a.Attendance.AttendanceDate, employee.EmployeeCode,
            $"{employee.FirstName} {employee.LastName}", a.Approver));
    }

    public async Task<AttendanceAdjustmentDto> ApproveAsync(long adjustmentId, string approverEmployeeCode, bool isAdmin)
    {
        var approver = await _employeeRepository.GetByEmployeeCodeAsync(approverEmployeeCode)
            ?? throw new InvalidOperationException($"Employee code '{approverEmployeeCode}' not found.");

        var adjustment = await _adjustmentRepository.GetByIdAsync(adjustmentId)
            ?? throw new InvalidOperationException($"Adjustment {adjustmentId} not found.");

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
            throw new InvalidOperationException("Only pending adjustments can be approved.");
        }

        adjustment.Status = ApprovalStatus.Approved;
        adjustment.ApprovedBy = approver.Id;
        adjustment.ApprovedAt = VietnamClock.Now;

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

        if (adjustment.Attendance.CheckInTime.HasValue && adjustment.Attendance.CheckOutTime.HasValue)
        {
            adjustment.Attendance.WorkingHours =
                (decimal)(adjustment.Attendance.CheckOutTime.Value - adjustment.Attendance.CheckInTime.Value).TotalHours;

            // Record được tạo rỗng lúc submit (nhân viên quên chấm công cả ngày) mặc định Status = Absent;
            // giờ đã có đủ giờ vào/ra thật thì đổi lại thành Present cho đúng.
            if (adjustment.Attendance.Status == AttendanceStatus.Absent)
            {
                adjustment.Attendance.Status = AttendanceStatus.Present;
            }
        }

        adjustment.Attendance.UpdatedAt = VietnamClock.Now;

        await _adjustmentRepository.SaveChangesAsync();

        return ToDto(adjustment, adjustment.Attendance.AttendanceDate, adjustment.Requester.EmployeeCode,
            $"{adjustment.Requester.FirstName} {adjustment.Requester.LastName}", approver);
    }

    public async Task<AttendanceAdjustmentDto> RejectAsync(long adjustmentId, string approverEmployeeCode, bool isAdmin)
    {
        var approver = await _employeeRepository.GetByEmployeeCodeAsync(approverEmployeeCode)
            ?? throw new InvalidOperationException($"Employee code '{approverEmployeeCode}' not found.");

        var adjustment = await _adjustmentRepository.GetByIdAsync(adjustmentId)
            ?? throw new InvalidOperationException($"Adjustment {adjustmentId} not found.");

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
            throw new InvalidOperationException("Only pending adjustments can be rejected.");
        }

        adjustment.Status = ApprovalStatus.Rejected;
        adjustment.ApprovedBy = approver.Id;
        adjustment.ApprovedAt = VietnamClock.Now;

        await _adjustmentRepository.SaveChangesAsync();

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
        ApprovedAt = adjustment.ApprovedAt
    };
}
