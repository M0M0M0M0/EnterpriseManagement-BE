using EnterpriseManagement.Application.DTOs;
using EnterpriseManagement.Domain.Enums;

namespace EnterpriseManagement.Application.Interfaces;

public interface IAttendanceService
{
    Task<AttendanceRecordDto> PunchAsync(string employeeCode);
    Task<IEnumerable<AttendanceRecordDto>> GetHistoryAsync(string employeeCode);
    Task<IEnumerable<AttendanceRecordDto>> GetByDepartmentAsync(string departmentCode, DateOnly startDate, DateOnly endDate);

    // Gọi khi 1 đơn xin nghỉ buổi/cả ngày (không phải Nghỉ ngắn) được duyệt — tự đánh dấu
    // AttendanceRecord của (các) ngày làm việc trong khoảng nghỉ thành OnLeave (Cả ngày) hoặc
    // HalfDay (nửa buổi), trừ ngày nào đã có chấm công thật thì giữ nguyên.
    Task ApplyApprovedLeaveAsync(long employeeId, DateOnly startDate, DateOnly endDate, LeaveSession session);

    // Cùng logic PunchAsync dùng để xác định Present/Late/HalfDayAbsent theo giờ check-in — expose
    // ra để AttendanceAdjustmentService tính lại đúng trạng thái khi duyệt yêu cầu sửa giờ chấm công
    // (thay vì chỉ có 1 nhánh Absent -> Present như trước).
    Task<AttendanceStatus> DetermineCheckInStatusAsync(long employeeId, DateOnly date, DateTime checkInTime);
}
