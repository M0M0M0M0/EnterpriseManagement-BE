using EnterpriseManagement.Application.Common;
using EnterpriseManagement.Application.DTOs;
using EnterpriseManagement.Application.Interfaces;
using EnterpriseManagement.Domain.Entities.Attendance;
using EnterpriseManagement.Domain.Entities.HR;
using EnterpriseManagement.Domain.Entities.Leave;
using EnterpriseManagement.Domain.Enums;

namespace EnterpriseManagement.Application.Services;

public class AttendanceService : IAttendanceService
{
    // Giờ chuẩn check-in buổi sáng: đúng/trước 8h15 tính Present, sau đó tính Late — mỗi phút
    // nghỉ ngắn đã được duyệt trong buổi sáng cùng ngày cộng dồn thêm vào mốc này (xem
    // GetApprovedShortLeaveMinutesAsync). Quá 2 tiếng so với giờ chuẩn (8h) mà vẫn chưa
    // check-in và không có phép thì coi như bỏ nguyên buổi sáng (HalfDayAbsent), không tính
    // Late nữa. Giờ chuẩn buổi chiều dùng khi nhân viên đã được duyệt nghỉ buổi sáng — nghĩa
    // vụ thật của họ chỉ bắt đầu từ chiều nên so theo mốc này thay vì mốc buổi sáng.
    private static readonly TimeOnly StandardCheckInTime = new(8, 0);
    private static readonly TimeOnly StandardCheckInDeadline = new(8, 15);
    private static readonly TimeOnly HalfDayAbsentDeadline = StandardCheckInTime.AddHours(2);
    private static readonly TimeOnly StandardAfternoonDeadline = new(13, 15);

    private readonly IAttendanceRepository _attendanceRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly ILeaveRequestRepository _leaveRequestRepository;

    public AttendanceService(
        IAttendanceRepository attendanceRepository,
        IEmployeeRepository employeeRepository,
        IDepartmentRepository departmentRepository,
        ILeaveRequestRepository leaveRequestRepository)
    {
        _attendanceRepository = attendanceRepository;
        _employeeRepository = employeeRepository;
        _departmentRepository = departmentRepository;
        _leaveRequestRepository = leaveRequestRepository;
    }

    public async Task<AttendanceRecordDto> PunchAsync(string employeeCode)
    {
        var employee = await _employeeRepository.GetByEmployeeCodeAsync(employeeCode)
            ?? throw new InvalidOperationException($"Employee code '{employeeCode}' not found.");

        var today = VietnamClock.Today;
        var record = await _attendanceRepository.GetByEmployeeAndDateAsync(employee.Id, today);
        var now = VietnamClock.Now;

        if (record is null)
        {
            // Lần punch đầu tiên trong ngày = check-in.
            record = new AttendanceRecord
            {
                EmployeeId = employee.Id,
                AttendanceDate = today,
                CheckInTime = now,
                Status = await DetermineCheckInStatusAsync(employee.Id, today, now),
                CreatedAt = now
            };
            await _attendanceRepository.AddAsync(record);
        }
        else if (record.CheckInTime is null)
        {
            // Bản ghi hôm nay đã tồn tại nhưng chưa có check-in thật (vd ngày đang bị đánh dấu
            // Vắng/Nghỉ phép) — coi lần bấm này là check-in thật, không phải check-out.
            record.CheckInTime = now;
            record.CheckOutTime = null;
            record.WorkingHours = null;
            record.Status = await DetermineCheckInStatusAsync(employee.Id, today, now);
            record.Note = null;
            record.UpdatedAt = now;
        }
        else
        {
            // Các lần punch sau trong cùng ngày = check-out, đè lên checkout trước đó nếu có.
            record.CheckOutTime = now;
            record.WorkingHours = (decimal)(now - record.CheckInTime.Value).TotalHours;
            record.UpdatedAt = now;
        }

        await _attendanceRepository.SaveChangesAsync();

        return ToDto(record, employee);
    }

    private async Task<AttendanceStatus> DetermineCheckInStatusAsync(long employeeId, DateOnly date, DateTime checkInTime)
    {
        var checkInTimeOnly = TimeOnly.FromDateTime(checkInTime);
        var approvedLeavesToday = await GetApprovedLeavesForDateAsync(employeeId, date);

        // Đã được duyệt nghỉ buổi sáng -> nghĩa vụ thật của ngày hôm nay chỉ bắt đầu từ chiều,
        // so giờ check-in với giờ chuẩn buổi chiều thay vì buổi sáng (vd checkin 10h55 vẫn tính
        // Present vì còn sớm hơn nhiều so với 13h15, không phải "đến muộn buổi sáng").
        var hasApprovedMorningLeave = approvedLeavesToday.Any(l =>
            l.LeaveType.AccrualPeriod != LeaveAccrualPeriod.MonthlyReset && l.Session == LeaveSession.Morning);
        if (hasApprovedMorningLeave)
        {
            return checkInTimeOnly <= StandardAfternoonDeadline ? AttendanceStatus.Present : AttendanceStatus.Late;
        }

        // Không có phép mà quá 2 tiếng so với giờ chuẩn (8h) vẫn chưa check-in -> coi như bỏ
        // nguyên buổi sáng không xin phép, cần trạng thái riêng để quản lý thấy rõ thay vì lẫn
        // với "đến trễ vài phút".
        if (checkInTimeOnly >= HalfDayAbsentDeadline)
        {
            return AttendanceStatus.HalfDayAbsent;
        }

        // Nghỉ ngắn đã được duyệt vào buổi sáng (trước 12h) cùng ngày cộng dồn vào giờ chuẩn
        // check-in — xin nghỉ ngắn 30 phút thì được đi trễ thêm 30 phút mà vẫn tính Present.
        var shortLeaveMinutes = approvedLeavesToday
            .Where(l => l.LeaveType.AccrualPeriod == LeaveAccrualPeriod.MonthlyReset && l.StartDate.TimeOfDay < new TimeSpan(12, 0, 0))
            .Sum(l => l.TotalTime) * 60;
        var deadline = shortLeaveMinutes > 0 ? StandardCheckInDeadline.AddMinutes((double)shortLeaveMinutes) : StandardCheckInDeadline;

        return checkInTimeOnly <= deadline ? AttendanceStatus.Present : AttendanceStatus.Late;
    }

    private async Task<List<LeaveRequest>> GetApprovedLeavesForDateAsync(long employeeId, DateOnly date)
    {
        var dayStart = date.ToDateTime(TimeOnly.MinValue);
        var dayEnd = date.ToDateTime(TimeOnly.MaxValue);
        var requests = await _leaveRequestRepository.GetActiveByEmployeeAndRangeAsync(employeeId, dayStart, dayEnd);

        return requests.Where(l => l.Status == LeaveRequestStatus.Approved).ToList();
    }

    public async Task ApplyApprovedLeaveAsync(long employeeId, DateOnly startDate, DateOnly endDate, LeaveSession session)
    {
        var status = session == LeaveSession.FullDay ? AttendanceStatus.OnLeave : AttendanceStatus.HalfDay;
        var now = VietnamClock.Now;

        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            if (!DateRangeHelper.IsWeekday(date)) continue;

            var record = await _attendanceRepository.GetByEmployeeAndDateAsync(employeeId, date);
            if (record is not null && record.CheckInTime is not null)
            {
                // Ngày này đã có chấm công thật (vd nghỉ nửa buổi, nửa buổi còn lại đã đi làm) —
                // không ghi đè dữ liệu chấm công thật bằng trạng thái nghỉ phép.
                continue;
            }

            if (record is null)
            {
                record = new AttendanceRecord
                {
                    EmployeeId = employeeId,
                    AttendanceDate = date,
                    Status = status,
                    CreatedAt = now
                };
                await _attendanceRepository.AddAsync(record);
            }
            else
            {
                record.Status = status;
                record.UpdatedAt = now;
            }
        }

        await _attendanceRepository.SaveChangesAsync();
    }

    public async Task<IEnumerable<AttendanceRecordDto>> GetHistoryAsync(string employeeCode)
    {
        var employee = await _employeeRepository.GetByEmployeeCodeAsync(employeeCode)
            ?? throw new InvalidOperationException($"Employee code '{employeeCode}' not found.");

        var records = await _attendanceRepository.GetByEmployeeAsync(employee.Id);
        return records.Select(r => ToDto(r, employee));
    }

    public async Task<IEnumerable<AttendanceRecordDto>> GetByDepartmentAsync(string departmentCode, DateOnly startDate, DateOnly endDate)
    {
        var department = await _departmentRepository.GetByCodeAsync(departmentCode)
            ?? throw new InvalidOperationException($"Department code '{departmentCode}' not found.");

        var records = await _attendanceRepository.GetByDepartmentAndPeriodAsync(department.Id, startDate, endDate);
        return records.Select(r => ToDto(r, r.Employee));
    }

    private static AttendanceRecordDto ToDto(AttendanceRecord record, Employee employee) => new()
    {
        EmployeeCode = employee.EmployeeCode,
        EmployeeName = $"{employee.FirstName} {employee.LastName}",
        AttendanceDate = record.AttendanceDate,
        CheckInTime = record.CheckInTime,
        CheckOutTime = record.CheckOutTime,
        WorkingHours = record.WorkingHours,
        Status = record.Status.ToString()
    };
}
