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
}
